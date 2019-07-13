// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
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
    /// <summary>
    /// Imports data from a text file to a MS SQL Server instance.
    /// </summary>
    /// <typeparam name="T">Target object type containing data for transfer</typeparam>
    public class MsSqlServerDatabaseTextFileImport<T> : IDisposable where T : new()
    {
        /// <summary>
        /// The default number of rows to buffer in memory before the batch 
        /// is flushed to the database instance
        /// </summary>
        private const int BATCH_SIZE_DEFAULT = 1000;

        /// <summary>
        /// Holds a mapping of MS SQL types to .NET type equivalents
        /// </summary>
        private static readonly Dictionary<string, Type> SqlTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
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
        /// Timer for write operations
        /// </summary>
        private Stopwatch _writeTimer = null;

        /// <summary>
        /// Task handling the database flush
        /// </summary>
        private Task _flushTask = null;

        /// <summary>
        /// Flag indicating whether the instance has been initialized in terms
        /// of mappings created for the target table/source type
        /// </summary>
        private bool _isInitialised = false;

        /// <summary>
        /// Template of the target database table
        /// </summary>
        private DataTable _template;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlServerDatabaseTextFileImport{T}" /> class
        /// </summary>
        public MsSqlServerDatabaseTextFileImport()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlServerDatabaseTextFileImport{T}" /> class
        /// </summary>
        /// <param name="textFileSerializer">Text file serializer instance to consume data from</param>
        public MsSqlServerDatabaseTextFileImport(TextFileSerializer<T> textFileSerializer) : this()
        {
            TextFileReader = textFileSerializer;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlServerDatabaseTextFileImport{T}" /> class
        /// </summary>
        /// <param name="fileName">Source file name</param>
        /// <param name="delimiter">Delimiter in source file</param>
        /// <param name="firstRowHeader">Does the first row contain column headings</param>
        /// <param name="skipRows">Number of rows to skip at the head of the file</param>
        public MsSqlServerDatabaseTextFileImport(string fileName, string delimiter, bool firstRowHeader = true, int skipRows = 0) : 
            this(new FileStream(fileName, FileMode.Open), delimiter, firstRowHeader, skipRows)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlServerDatabaseTextFileImport{T}" /> class
        /// </summary>
        /// <param name="file">Stream of the source file</param>
        /// <param name="delimiter">Delimiter in source file</param>
        /// <param name="firstRowHeader">Does the first row contain column headings</param>
        /// <param name="skipRows">Number of rows to skip at the head of the file</param>
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
        /// Event handler delegate for a before/after row populate event
        /// </summary>
        /// <param name="sender">Sender class</param>
        /// <param name="e">Transform event arguments</param>
        public delegate void TransformEventHandler(object sender, TransformEventArgs e);

        /// <summary>
        /// Event handler delegate for a flush operation
        /// </summary>
        /// <param name="e">Flush event args</param>
        public delegate void FlushEventHandler(FlushEventArgs e);
        
        /// <summary>
        /// Event that occurs before a data row is populated
        /// </summary>
        public event TransformEventHandler BeforeDataRowPopulate;

        /// <summary>
        /// Event that occurs after a data row is populated
        /// </summary>
        public event TransformEventHandler AfterDataRowPopulate;
        
        /// <summary>
        /// Flush event handler
        /// </summary>
        public event FlushEventHandler OnFlush;

        /// <summary>
        /// A property fetching instance able to retrieve
        /// the value contains on the target type instance
        /// for a given property
        /// </summary>
        private interface IPropertyFetcher
        {
            /// <summary>
            /// Gets the value from the property
            /// </summary>
            /// <param name="instance">Target type instance</param>
            /// <returns>Property value</returns>
            object GetValue(T instance);
        }

        /// <summary>
        /// Gets or sets the current buffer of data to save to the database
        /// </summary>
        public DataTable Buffer { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of rows in the data table before 
        /// the data is flushed to the database
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Gets or sets the target database table name
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the meta data found on the target type for database mapping
        /// </summary>
        public IEnumerable<DatabaseMappingMetaData> MetaData { get; set; }
        
        /// <summary>
        /// Gets or sets the underlying SQL connection to use
        /// </summary>
        public SqlConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets the underlying transaction to use
        /// </summary>
        public SqlTransaction Transaction { get; set; }

        /// <summary>
        /// Gets the number of rows saved to the database
        /// </summary>
        public int RowsSaved { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether flushing to the database happens
        /// in sync or async. If true, then the flush to the 
        /// database is always done in sync, we don't carry on reading the file 
        /// and populating the buffer while waiting for the flush to complete
        /// </summary>
        public bool AlwaysSyncronousFlush { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the SQL bulk copy
        /// </summary>
        public int? BulkCopyTimeout { get; set; }

        /// <summary>
        /// Gets the mapping of the target type's properties
        /// to database mapping attributes defined on that type
        /// </summary>
        public IEnumerable<PropertyToAttributeMap> MappingAttributes { get; private set; }

        /// <summary>
        /// Gets or sets the text file serializer instance from which source data is read
        /// </summary>
        private TextFileSerializer<T> TextFileReader { get; set; }
                
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
        /// Execute a flush operation to clear data buffered in memory
        /// to the underlying database table
        /// </summary>
        /// <param name="final">Flag indicating if this is the last flush, all source data is read</param>
        /// <param name="tableName">The underlying database table name to flush to</param>
        /// <returns>True, if a flush was performed</returns>
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
                    if (BulkCopyTimeout.HasValue) sqlBulkCopy.BulkCopyTimeout = BulkCopyTimeout.Value;
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

        /// <summary>
        /// Dispose of the managed resources created by this class
        /// </summary>
        public void Dispose()
        {
            TextFileReader?.Dispose();
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

        /// <summary>
        /// Fetch a template data table of the target
        /// database table by querying the database
        /// for its schema
        /// </summary>
        /// <param name="tableName">Name of the database name, including any schema prefix</param>
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
                if (SqlTypeMap.TryGetValue(dataType, out targetType) == false)
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

        /// <summary>
        /// Meta data for mapping properties to the underlying
        /// database table instance
        /// </summary>
        public class DatabaseMappingMetaData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DatabaseMappingMetaData" /> class
            /// </summary>
            /// <param name="mapping">Property info/database mapping attribute pairing</param>
            public DatabaseMappingMetaData(PropertyToAttributeMap mapping)
            {
                DatabaseColumn = mapping?.Mapping?.DatabaseColumn;
                Mapping = mapping;
                var baseType = typeof(PropertyGetter<>);
                var targetType = baseType.MakeGenericType(typeof(T), mapping.Property.PropertyType);
                Fetcher = (IPropertyFetcher)Activator.CreateInstance(targetType, mapping.Property);
            }

            /// <summary>
            /// Gets or sets the database column name to map to
            /// </summary>
            public string DatabaseColumn { get; set; }

            /// <summary>
            /// Gets or sets the source ordinal in the text file
            /// </summary>
            public int Ordinal { get; set; }

            /// <summary>
            /// Gets or sets the property/attribute pairing map
            /// </summary>
            public PropertyToAttributeMap Mapping { get; set; }

            /// <summary>
            /// Gets or sets the instance to retrieve property information
            /// </summary>
            private IPropertyFetcher Fetcher { get; set; }

            /// <summary>
            /// Get the value of this property from the given
            /// target type instance
            /// </summary>
            /// <param name="instance">Target type instance</param>
            /// <returns>Corresponding property value in the target type instance</returns>
            public object GetValue(T instance)
            {
                return Fetcher.GetValue(instance);
            }
        }

        /// <summary>
        /// Holds a pairing of a property and its corresponding
        /// database mapping attribute
        /// </summary>
        public class PropertyToAttributeMap
        {
            /// <summary>
            /// Gets or sets the property info instance indicating the property
            /// on the target type
            /// </summary>
            public PropertyInfo Property { get; set; }

            /// <summary>
            /// Gets or sets the database mapping attribute associated with the property
            /// </summary>
            public DatabaseMappingAttribute Mapping { get; set; }
        }

        /// <summary>
        /// Event arguments for a Flush event
        /// </summary>
        public class FlushEventArgs
        {
            /// <summary>
            /// Gets or sets the number of rows saved in the flush event
            /// </summary>
            public int RowsSaved { get; set; }

            /// <summary>
            /// Gets or sets the total elapsed time of the flush event
            /// </summary>
            public TimeSpan TotalElapsedTime { get; set; }

            /// <summary>
            /// Gets or sets the time spent flushing to the database
            /// </summary>
            public TimeSpan DatabaseWriteTime { get; set; }
        }

        /// <summary>
        /// Transformation event arguments
        /// </summary>
        public class TransformEventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TransformEventArgs" /> class
            /// </summary>
            /// <param name="row">Data row being transformed</param>
            /// <param name="sourceLineNumber">Source line number from the text file</param>
            /// <param name="rowNumber">Number of rows processed</param>
            /// <param name="sourceObject">Source object instance</param>
            public TransformEventArgs(DataRow row, int sourceLineNumber, int rowNumber, T sourceObject)
            {
                DataRow = row;
                SourceLineNumber = sourceLineNumber;
                RowNumber = rowNumber;
                SourceObject = sourceObject;
            }

            /// <summary>
            /// Gets the data row being transformed
            /// </summary>
            public DataRow DataRow { get; private set; }

            /// <summary>
            /// Gets the source line number in the text file
            /// </summary>
            public int SourceLineNumber { get; private set; }

            /// <summary>
            /// Gets the number of rows processed so far
            /// </summary>
            public int RowNumber { get; private set; }

            /// <summary>
            /// Gets the source object for the row
            /// </summary>
            public T SourceObject { get; private set; }
        }

        /// <summary>
        /// Property setter, able to provide access
        /// to the target property on the target type
        /// </summary>
        /// <typeparam name="TTarget">Target type</typeparam>
        private class PropertyGetter<TTarget> : IPropertyFetcher
        {
            /// <summary>
            /// Function to retrieve data from the target type
            /// from the target property
            /// </summary>
            private Func<T, TTarget> _getter;

            /// <summary>
            /// Initializes a new instance of the <see cref="PropertyGetter{TTarget}" /> class
            /// </summary>
            /// <param name="propInfo">Property info this instance retrieves data from on the target type</param>
            public PropertyGetter(PropertyInfo propInfo)
            {
                _getter = (Func<T, TTarget>)propInfo.GetGetMethod().CreateDelegate(typeof(Func<T, TTarget>));
            }

            /// <summary>
            /// Gets the value contained in the target property
            /// on the target instance
            /// </summary>
            /// <param name="instance">Target instance</param>
            /// <returns>Value contained in the target property</returns>
            public object GetValue(T instance)
            {
                return _getter(instance);
            }
        }
    }
}
