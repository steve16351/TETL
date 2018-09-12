using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TETL.Attributes;

namespace TETL
{
    public class MsSqlServerDatabaseTextFileImport<T> : IDisposable where T : new()
    {
        public class TransformEventArgs
        {
            public TransformEventArgs(DataRow row, int sourceLineNumber, int rowNumber, T sourceObject)
            {
                DataRow = row;
                SourceLineNumber = sourceLineNumber;
                RowNumber = rowNumber;
                SourceObject = sourceObject;
            }
            
            public DataRow DataRow { get; private set; }
            public int SourceLineNumber { get; private set; }
            public int RowNumber { get; private set; }
            public T SourceObject { get; private set; }
        }

        public delegate void TransformEventHandler(object sender, TransformEventArgs e);

        public event TransformEventHandler BeforeDataRowPopulate;
        public event TransformEventHandler AfterDataRowPopulate;

        /// <summary>
        /// Contains the current buffer of data to save to the database
        /// </summary>
        public DataTable Buffer { get; set; }
        /// <summary>
        /// Maximum number of rows in the datatable before the data is flushed
        /// to the database
        /// </summary>
        public int? BatchSize { get; set; }
        /// <summary>
        /// Target table name
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Meta data found on the target type for database mapping
        /// </summary>
        public IEnumerable<DatabaseMappingMetaData> MetaData { get; set; }
        
        private TextFileSerializer<T> TextFileReader { get; set; }
        private bool _isInitialised = false;
        private const int BATCH_SIZE_DEFAULT = 1000;        
        

        public MsSqlServerDatabaseTextFileImport()
        {

        }

        public MsSqlServerDatabaseTextFileImport(TextFileSerializer<T> textFileSerializer) : this()
        {
            TextFileReader = textFileSerializer;
        }

        public MsSqlServerDatabaseTextFileImport(string fileName, string delimiter, bool firstRowHeader = true, int skipRows = 0) : this
            (new FileStream(fileName, FileMode.Open), delimiter, firstRowHeader, skipRows)
        {

        }

        public MsSqlServerDatabaseTextFileImport(Stream file, string delimiter, bool firstRowHeader = true, int skipRows = 0) : this()
        {
            TextFileReader = new TextFileSerializer<T>(file)
            {
                Delimiter = delimiter,
                FirstRowHeader = firstRowHeader,
                SkipHeaderRows = skipRows
            };
        }
        
        /// <summary>
        /// Build meta data on the target type
        /// </summary>
        private void Initialise()
        {
            var databaseMappingAttributes = typeof(T).GetCustomAttributes(true)
                .Where(a => a.GetType() == typeof(DatabaseMappingAttribute))
                .Cast<DatabaseMappingAttribute>();

            var mappings = typeof(T)
                .GetProperties()
                .Where(prop => prop.GetCustomAttributes(typeof(DatabaseMappingAttribute), true).Any())
                .Select(prop => new PropertyToAttributeMap()
                {
                    Property = prop,
                    Mapping = prop
                        .GetCustomAttributes(typeof(DatabaseMappingAttribute), true)
                        .Cast<DatabaseMappingAttribute>().Single()
                });

            if (!mappings.Any())
                throw new ArgumentException($"No DatabaseMappingAttribute present on any properties on type \"{typeof(T).FullName}\"");

            foreach (var databaseMapping in databaseMappingAttributes)
                databaseMapping.ThrowIfInvalid();

            MappingAttributes = mappings;
            MetaData = MappingAttributes
                .Select((a, i) => new DatabaseMappingMetaData(a) { Ordinal = i })
                .ToList();            

            _isInitialised = true;
        }

        private DataTable _template;

        private void FetchTemplate(string tableName)
        {
            if (_template != null) return;

            var schemaAndTable = tableName.Split(new char[] { '.' });
            if (schemaAndTable.Length != 2)
                throw new ArgumentException("Expected table name in format schema.tablename");

            SqlCommand sqlCommand = new SqlCommand(@"SELECT COLUMN_NAME, DATA_TYPE, ORDINAL_POSITION
                                                    FROM INFORMATION_SCHEMA.COLUMNS
                                                    WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @SchemaName
                                                    ORDER BY ORDINAL_POSITION");

            sqlCommand.Connection = Connection;
            sqlCommand.Parameters.AddWithValue("@TableName", schemaAndTable[1]);
            sqlCommand.Parameters.AddWithValue("@SchemaName", schemaAndTable[0]);

            DataTable schema = new DataTable();
            SqlDataAdapter dataAdapter = new SqlDataAdapter(sqlCommand);
            var tableSchema = new DataTable() { TableName = tableName };
            dataAdapter.Fill(tableSchema);

            if (tableSchema.Rows.Count == 0)
                throw new ArgumentException($"Couldn't find table \"{tableName}\" in the database");

            _template = new DataTable() { TableName = tableName };

            foreach (DataRow row in tableSchema.Rows)
            {
                var fieldName = row.Field<string>("COLUMN_NAME");
                var dataType = row.Field<string>("DATA_TYPE");

                Type targetType = null;
                if (_sqlTypeMap.TryGetValue(dataType, out targetType) == false)
                    throw new NotSupportedException($"Sql data type \"{dataType}\" not implemented");

                _template.Columns.Add(fieldName, targetType);
            }

            // check meta is ok, set the ordinal correctly
            foreach (var meta in MetaData)
            {
                if (_template.Columns.Contains(meta.DatabaseColumn) == false)
                    throw new ArgumentException($"The column {meta.DatabaseColumn} does not exist in the table {tableName}");

                meta.Ordinal = _template.Columns[meta.DatabaseColumn].Ordinal;
            }
        }

        private static readonly Dictionary<string, Type> _sqlTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "char", typeof(string) },
            { "uniqueidentifier", typeof(Guid) },
            { "varchar", typeof(string) },
            { "float", typeof(double) },
            { "int", typeof(int) },
            { "bit", typeof(bool) },
            { "decimal", typeof(decimal) },
            { "datetime", typeof(DateTime) }
        };

        /// <summary>
        /// Create database table instance
        /// </summary>
        /// <param name="tableName">Target database table name</param>
        /// <returns>Data table with appropriate columns and types</returns>
        public DataTable CreateTable(string tableName)
        {
            FetchTemplate(tableName);
            DataTable table = _template.Clone();                        
            table.BeginLoadData();
            return table;
        }

        /// <summary>
        /// Extract data from the text file and send to the database
        /// </summary>
        /// <param name="tableName">Table name to target</param>
        public void Extract(string tableName)
        {
            if (!_isInitialised)
                Initialise();

            if (Connection == null)
                throw new ArgumentNullException("Connection");

            RowsSaved = 0;
            _writeTimer = Stopwatch.StartNew();            
            Buffer = CreateTable(tableName);

            int rowNo = 0;

            foreach (T row in TextFileReader)
            {                
                var dataRow = Buffer.NewRow();
                TransformEventArgs args = new TransformEventArgs(dataRow, TextFileReader.LineNo, rowNo, row);
                BeforeDataRowPopulate?.Invoke(this, args);

                foreach (var metaData in MetaData)                
                    dataRow[metaData.Ordinal] = metaData.GetValue(row) ?? DBNull.Value;

                AfterDataRowPopulate?.Invoke(this, args);
                Buffer.Rows.Add(dataRow);
                ExecuteFlush(false, tableName);
            }

            // execute final flush
            ExecuteFlush(true, tableName);
            _writeTimer.Stop();
        }

        /// <summary>
        /// Underlying SQL connection to use
        /// </summary>
        public SqlConnection Connection { get; set; }
        /// <summary>
        /// Underlying transaction to use
        /// </summary>
        public SqlTransaction Transaction { get; set; }
        /// <summary>
        /// Flush event handler
        /// </summary>
        public event FlushEventHandler OnFlush;
        public delegate void FlushEventHandler(FlushEventArgs e);

        public class FlushEventArgs
        {
            public int RowsSaved { get; set; }
            public TimeSpan TotalElapsedTime { get; set; }
            public TimeSpan DatabaseWriteTime { get; set; }
        }

        private Stopwatch _writeTimer = null;
        private Task _flushTask = null;

        /// <summary>
        /// Number of rows saved to the database
        /// </summary>
        public int RowsSaved { get; private set; }        
        /// <summary>
        /// If true, then the flush to the database is always done in sync, 
        /// we don't carry on reading the file and populating the buffer
        /// while waiting for the flush to complete
        /// </summary>
        public bool AlwaysSyncronousFlush { get; set; }

        public bool ExecuteFlush(bool final, string tableName)
        {
            if (Buffer.Rows.Count < (BatchSize ?? BATCH_SIZE_DEFAULT) && !final)
                return false;
            if (Buffer.Rows.Count == 0)
                return false;

            Buffer.EndLoadData();

            if (_flushTask != null)
                _flushTask.Wait(); // wait for any previous flush to finish before starting the next one

            DataTable toFlush = Buffer;
            Buffer = final ? null : CreateTable(tableName);
            Buffer?.BeginLoadData();

            _flushTask = Task.Run(() =>
            {
                Stopwatch writeTime = Stopwatch.StartNew();

                using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(Connection, SqlBulkCopyOptions.Default, Transaction))
                {
                    sqlBulkCopy.DestinationTableName = tableName;
                    sqlBulkCopy.WriteToServer(toFlush);
                    RowsSaved += toFlush.Rows.Count;
                }

                writeTime.Stop();

                OnFlush?.Invoke(new FlushEventArgs() { RowsSaved = RowsSaved, TotalElapsedTime = _writeTimer.Elapsed, DatabaseWriteTime = writeTime.Elapsed });
            });

            if (final || AlwaysSyncronousFlush)
                _flushTask.Wait();

            return true;
        }

        public void Dispose()
        {
            TextFileReader?.Dispose();
        }

        public class DatabaseMappingMetaData
        {
            public string DatabaseColumn { get; set; }
            public int Ordinal { get; set; }
            private IPropertyFetcher Fetcher { get; set; }
            public PropertyToAttributeMap Mapping { get; set; }

            public DatabaseMappingMetaData(PropertyToAttributeMap mapping)
            {
                DatabaseColumn = mapping?.Mapping?.DatabaseColumn;
                Mapping = mapping;
                var baseType = typeof(PropertySetter<>);
                var targetType = baseType.MakeGenericType(typeof(T), mapping.Property.PropertyType);
                Fetcher = (IPropertyFetcher)Activator.CreateInstance(targetType, mapping.Property);
            }

            public object GetValue(T instance)
            {
                return Fetcher.GetValue(instance);
            }
        }

        private interface IPropertyFetcher
        {
            object GetValue(T instance);
        }

        private class PropertySetter<TTarget> : IPropertyFetcher
        {
            private Func<T, TTarget> _getter;

            public PropertySetter(PropertyInfo propInfo)
            {
                _getter = (Func<T, TTarget>)propInfo.GetGetMethod().CreateDelegate(typeof(Func<T, TTarget>));
            }

            public object GetValue(T instance)
            {
                return _getter(instance);
            }                
        }

        public IEnumerable<PropertyToAttributeMap> MappingAttributes { get; private set; }

        public class PropertyToAttributeMap
        {
            public PropertyInfo Property { get; set; }
            public DatabaseMappingAttribute Mapping { get; set; }
        }
    }
}
