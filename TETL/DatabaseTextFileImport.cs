using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using TETL.Attributes;

namespace TETL
{
    public class DatabaseTextFileImport<T> : IDisposable where T : new()
    {
        public int? BatchSize { get; set; }

        public string TableName { get; set; }

        private TextFileSerializer<T> TextFileReader { get; set; }

        private bool _isInitialised = false;

        private const int BATCH_SIZE_DEFAULT = 1000;

        public DatabaseTextFileImport()
        {

        }


        public DatabaseTextFileImport(TextFileSerializer<T> textFileSerializer) : this()
        {
            TextFileReader = textFileSerializer;
        }

        public DatabaseTextFileImport(string fileName, string delimiter, bool firstRowHeader = true, int skipRows = 0) : this
            (new FileStream(fileName, FileMode.Open), delimiter, firstRowHeader, skipRows)
        {

        }

        public DatabaseTextFileImport(Stream file, string delimiter, bool firstRowHeader = true, int skipRows = 0) : this()
        {
            TextFileReader = new TextFileSerializer<T>(file)
            {
                Delimiter = delimiter,
                FirstRowHeader = firstRowHeader,
                SkipRows = skipRows
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
                throw new ArgumentException($"No DatabaseMappingAttribute present on any properties on type {typeof(T).FullName}");

            foreach (var databaseMapping in databaseMappingAttributes)
                databaseMapping.ThrowIfInvalid();

            MappingAttributes = mappings;
            MetaData = MappingAttributes
                .Select((a, i) => new DatabaseMappingMetaData(a) { Ordinal = i })
                .ToList();            

            _isInitialised = true;
        }

        /// <summary>
        /// Create database table instance
        /// </summary>
        /// <param name="tableName">Target database table name</param>
        /// <returns>Data table with appropriate columns and types</returns>
        public DataTable CreateTable(string tableName = null)
        {
            DataTable table = new DataTable() { TableName = TableName };
            foreach (var meta in MetaData)
                table.Columns.Add(new DataColumn(meta.DatabaseColumn, meta.Mapping.Property.PropertyType));
            return table;
        }

        public void WriteToTable(string tableName)
        {
            if (!_isInitialised)
                Initialise();

            // create the data table
            Buffer = CreateTable(tableName);
            Buffer.BeginLoadData();

            foreach (T row in TextFileReader)
            {
                if (TransformAction != null)
                    TransformAction(row);

                var dataRow = Buffer.NewRow();

                foreach (var metaData in MetaData)
                {
                    dataRow[metaData.Ordinal] = metaData.GetValue(row);
                }

                Buffer.Rows.Add(dataRow);
            }

            Buffer.EndLoadData();
        }

        public void Dispose()
        {
            TextFileReader?.Dispose();
        }

        public DataTable Buffer { get; set; }

        public Action<T> TransformAction { get; set; }

        public IEnumerable<DatabaseMappingMetaData> MetaData { get; set; }

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
