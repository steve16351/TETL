using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TETL.Attributes;
using TETL.Converters;
using TETL.Exceptions;

namespace TETL
{
    public class TextFileSerializer<T> : IDisposable, IEnumerable<T> where T : new()
    {
        private StreamReader _readStream = null;
        private StreamWriter _writeStream = null;
        private Stream _inputStream;
        private string _fileName = null;
        private bool _writeInitialised = false;

        public TextFileSerializer(Stream stream) : this()
        {
            _inputStream = stream;
        }

        public TextFileSerializer(string fileName) : this()
        {
            _fileName = fileName;
        }

        public TextFileSerializer()
        {

        }
        
        /// <summary>
        /// Number of rows at the top of the file to skip
        /// </summary>
        public int SkipHeaderRows { get; set; }
        /// <summary>
        /// Number of rows to skip at the end of the file
        /// </summary>
        public int SkipFooterRows { get; set; }
        /// <summary>
        /// First row contains column headings
        /// </summary>
        public bool FirstRowHeader { get; set; }
        /// <summary>
        /// Delimiter to spit rows into columns
        /// </summary>
        public string Delimiter { get; set; }
        /// <summary>
        /// All meta data
        /// </summary>
        public ColumnMetaData[] MetaData { get; set; }
        /// <summary>
        /// Relevant meta data for mapping
        /// </summary>
        public ColumnMetaData[] RelevantMetaData { get; set; }
        
        /// <summary>
        /// Initialise for write operations
        /// </summary>
        private void InitialiseWrite()
        {
            Initialise();

            if (_inputStream == null && _fileName == null) throw new InvalidOperationException("Expected input stream or file name");

            if (_inputStream == null && _fileName != null)
                _inputStream = new FileStream(_fileName, AppendMode ? FileMode.Append : FileMode.Create);
            
            _writeStream = new StreamWriter(_inputStream);
            
            MetaData = MappingAttributes
                .Select((mappingAttribute, ordinal) => new ColumnMetaData()
                {
                    ColumnHeader = mappingAttribute.Mapping.ColumnName,
                    Ordinal = mappingAttribute.Mapping.InternalColumnOrdinal.HasValue ? mappingAttribute.Mapping.InternalColumnOrdinal.Value : ordinal,
                    MappingAttribute = mappingAttribute.Mapping,
                    TargetProperty = mappingAttribute.Property
                })
                .ToArray();

            foreach (var meta in MetaData)
                meta.SetTarget(meta.TargetProperty, meta.MappingAttribute);

            RelevantMetaData = MetaData
                .Where(a => a.Ignore == false)
                .ToArray();
        }

        /// <summary>
        /// Initialise the serializer for reading
        /// </summary>
        private void InitialiseRead()
        {
            Initialise();
            _readBuffer = new Queue<string>(SkipFooterRows);
            if (_inputStream == null && _fileName == null) throw new InvalidOperationException("Expected input stream or file name");

            if (_inputStream == null && _fileName != null)
                _inputStream = new FileStream(_fileName, FileMode.Open);

            _readStream = new StreamReader(_inputStream);
            LineNo = 0;

            if (_readStream.BaseStream.CanSeek)
                _readStream.BaseStream.Position = 0;

            for (int i = 0; i < SkipHeaderRows; i++)
            {
                if (_readStream.EndOfStream) throw new ArgumentException($"SkipRows ({SkipHeaderRows}) contains more rows than there are lines in the file");
                _readStream.ReadLine();
                LineNo++;
            }

            if (FirstRowHeader)
            {
                LineNo++;
                var headerRow = _readStream.ReadLine();
                if (String.IsNullOrWhiteSpace(headerRow))
                    throw new InvalidOperationException($"No header row found but one was expected");

                MetaData = headerRow
                    .Split(new[] { Delimiter }, StringSplitOptions.None)
                    .Select((a, i) => new ColumnMetaData() { ColumnHeader = a.Trim(), Ordinal = i })
                    .ToArray();
            }

            if (_readStream.EndOfStream)
                return; // file is empty, nothing to do

            ReadNext();

            if (!FirstRowHeader)
            {
                MetaData = CurrentLine
                    .Select((a, i) => new ColumnMetaData() { ColumnHeader = null, Ordinal = i })
                    .ToArray();
            }

            foreach (var mapping in MappingAttributes)
            {
                var matchedMeta = MetaData.Where(a => a.IsMatch(mapping.Mapping));
                if (!matchedMeta.Any())
                    throw new ArgumentException($"For mapping attribute on column {(mapping.Mapping.InternalColumnOrdinal.HasValue ? mapping.Mapping.ColumnOrdinal.ToString() : $"\"{mapping.Mapping.ColumnName}\"")}, no match was found in the file");
                if (matchedMeta.Count() > 1)
                    throw new ArgumentException($"For mapping attribute on column ({(mapping.Mapping.InternalColumnOrdinal.HasValue ? mapping.Mapping.ColumnOrdinal.ToString() : mapping.Mapping.ColumnName)}), multiple possible matches were found in the file");

                var match = matchedMeta.Single();
                match.SetTarget(mapping.Property, mapping.Mapping);
            }

            RelevantMetaData = MetaData
                .Where(a => !a.Ignore)
                .ToArray();

        }

        private bool _isInitialised = false;

        /// <summary>
        /// Build meta data on the target type
        /// </summary>
        private void Initialise()
        {
            if (_isInitialised) return;
            if (string.IsNullOrWhiteSpace(Delimiter))
                throw new ArgumentNullException("Delimiter must be specified");
                        
            var textFileMappingAttributes = typeof(T).GetCustomAttributes(true)
                .Where(a => a.GetType() == typeof(TextFileMappingAttribute))
                .Cast<TextFileMappingAttribute>();

            var mappings = typeof(T)
                .GetProperties()
                .Where(prop => prop.GetCustomAttributes(typeof(TextFileMappingAttribute), true).Any())
                .Select(prop => new PropertyToAttributeMap()
                {
                    Property = prop,
                    Mapping = prop
                        .GetCustomAttributes(typeof(TextFileMappingAttribute), true)
                        .Cast<TextFileMappingAttribute>().Single()
                });

            if (!mappings.Any())
                throw new ArgumentException($"No TextFileMappingAttribute present on any properties on type {typeof(T).FullName}");            

            foreach (var textFileMapping in textFileMappingAttributes)
                textFileMapping.ThrowIfInvalid();

            MappingAttributes = mappings;
            _isInitialised = true;
        }
        
        /// <summary>
        /// Append write records to an existing file rather than
        /// creating a new file, adding headings, etc.
        /// </summary>
        public bool AppendMode { get; set; }

        /// <summary>
        /// Are we currently at the end of the file?
        /// </summary>
        public bool IsEOF
        {
            get
            {
                return _readStream.EndOfStream;
            }
        }

        public string[] CurrentLine { get; private set; }

        /// <summary>
        /// Current line number we are on
        /// </summary>
        public int LineNo { get; private set; }
        
        private Queue<string> _readBuffer;

        private IEnumerable<PropertyToAttributeMap> MappingAttributes { get; set; }

        /// <summary>
        /// Preamble lines, when writing the text file from objects, any lines
        /// you wish to appear at the top of the file before headings or the data
        /// </summary>
        public string[] PreambleLines { get; set; }
        
        /// <summary>
        /// If needed, and not in append mode - add skipped lines, 
        /// and headings
        /// </summary>
        public void WritePreamble(string[] preambleLines = null)
        {
            InitialiseWrite();
            if (AppendMode)
            {
                _writeInitialised = true;
                return;
            }

            if (Delimiter == null)
                throw new ArgumentNullException("Delimiter");
            
            if (preambleLines != null)
            {
                foreach (string line in preambleLines)
                    _writeStream.WriteLine(line);
            }
            
            if (FirstRowHeader)
            {
                var headerRow = RelevantMetaData.Select((a, i) => a.ColumnHeader ?? String.Concat("Column " + i))
                    .Aggregate((a, b) => String.Concat(a, Delimiter, b));

                _writeStream.WriteLine(headerRow);
            }

            _writeInitialised = true;
        }

        public void WriteLines(IEnumerable<T> records)
        {
            if (Delimiter == null) throw new ArgumentNullException("Delimiter");

            if (_writeInitialised == false)
            {
                WritePreamble(PreambleLines);
            }            

            foreach (T record in records)
            {
                var writeData = RelevantMetaData
                    .Select(a => a.GetValue(record))
                    .Aggregate((a, b) => String.Concat(a, Delimiter, b));

                _writeStream.WriteLine(writeData);
            }
        }

        public void WriteLine(T record)
        {
            WriteLines(new[] { record });
        }

        /// <summary>
        /// Read the next line from the stream 
        /// into the buffer
        /// </summary>
        /// <returns>True if there are more lines to read</returns>
        private bool ReadNextWithBuffer()
        {
            // if we have to skip some footer rows, then check ahead because we need
            // to know where the end of the file is before we get there
            while (_readBuffer.Count < (this.SkipFooterRows + 1))
            {
                if (_readStream.EndOfStream)
                    break;

                _readBuffer.Enqueue(_readStream.ReadLine());
            }

            if (_readStream.EndOfStream && _readBuffer.Count == SkipFooterRows)
            {
                CurrentLine = null;
                return false; // we reached the end of the file
            }

            LineNo++;

            string nextLine = _readBuffer.Dequeue();
            CurrentLine = nextLine
                .Split(new[] { Delimiter }, StringSplitOptions.None);

            return true;
        }

        /// <summary>
        /// Read the next line from the stream
        /// </summary>
        /// <returns></returns>
        private bool ReadNext()
        {
            if (SkipFooterRows > 0) return ReadNextWithBuffer();

            if (_readStream.EndOfStream)
            {
                CurrentLine = null;
                return false; // we reached the end of the file
            }
            
            LineNo++;

            string nextLine = _readStream.ReadLine();
            CurrentLine = nextLine
                .Split(new[] { Delimiter }, StringSplitOptions.None);

            return true;
        }

        public void Dispose()
        {
            _readStream?.Dispose();
            _writeStream?.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            InitialiseRead();
            return new TextFileEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        private class PropertyToAttributeMap
        {
            public PropertyInfo Property { get; set; }
            public TextFileMappingAttribute Mapping { get; set; }
        }

        /// <summary>
        /// Column meta data, the position of the column, the column heading,
        /// the mapping information to the target object
        /// </summary>
        public class ColumnMetaData
        {
            /// <summary>
            /// Ordinal of this column in the text file
            /// </summary>
            public int Ordinal { get; set; }
            /// <summary>
            /// Column heading in the text file
            /// </summary>
            public string ColumnHeader { get; set; }
            /// <summary>
            /// Property on the target type
            /// </summary>
            public PropertyInfo TargetProperty { get; set; }

            /// <summary>
            /// Is this relevant for serialisation/deserialisation (i.e. does it have a mapping to/from the target type)
            /// </summary>
            public bool Ignore
            {
                get
                {
                    return TargetProperty == null;
                }
            }

            /// <summary>
            /// Does this mapping attribute match this column?
            /// </summary>
            /// <param name="mappingAttribute">Mapping attribute to check</param>
            /// <returns>True if a match, via either the column name or the ordinal</returns>
            public bool IsMatch(TextFileMappingAttribute mappingAttribute)
            {
                if (mappingAttribute.InternalColumnOrdinal.HasValue && Ordinal == mappingAttribute.InternalColumnOrdinal.Value)
                    return true;
                if (mappingAttribute.ColumnName != null && ColumnHeader != null && ColumnHeader.Equals(mappingAttribute.ColumnName, StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            }

            /// <summary>
            /// Set the target property and corresponding mapping data
            /// </summary>
            /// <param name="target">Target property info on the target type</param>
            /// <param name="mappingAttribute">Mapping attribute</param>
            public void SetTarget(PropertyInfo target, TextFileMappingAttribute mappingAttribute)
            {
                TargetProperty = target;
                MappingAttribute = mappingAttribute;
                _converter = StringConverterFactory<T>.Get(TargetProperty, MappingAttribute);
            }

            private IConvertAndSet _converter;

            public TextFileMappingAttribute MappingAttribute { get; set; }

            public string GetValue(T target)
            {
                return _converter.GetValue(target);
            }

            public void SetValue(T target, string value)
            {
                _converter.SetValue(target, value);
            }

        }


        public class TextFileEnumerator : IEnumerator<T>
        {
            private TextFileSerializer<T> _parent;

            public TextFileEnumerator(TextFileSerializer<T> parent)
            {
                _parent = parent;
            }

            object IEnumerator.Current { get { return _current; } }

            T IEnumerator<T>.Current { get { return _current; } }

            private T _current;

            public void Dispose()
            {
                return;
            }

            private bool Read()
            {
                if (_parent.CurrentLine == null) return false;
                _current = new T();

                foreach (ColumnMetaData metaData in _parent.RelevantMetaData)
                {
                    if (_parent.CurrentLine.Length <= metaData.Ordinal)
                    {
                        throw new TETLBadDataException($"Insuffient field length on line {_parent.LineNo}, trying to read field #{(metaData.Ordinal + 1)}, but source line only has {_parent.CurrentLine.Length} field(s)")
                        {
                            LineNo = _parent.LineNo,
                            Line = _parent.CurrentLine
                        };
                    }                   

                    var value = _parent.CurrentLine[metaData.Ordinal];

                    try
                    {
                        metaData.SetValue(_current, value);
                    }
                    catch (Exception conversionEx)
                    {
                        var sourceColumn = metaData.ColumnHeader + $" ({metaData.Ordinal})" ?? "Column " + metaData.Ordinal;
                        string errorMessage = $"Conversion problem on line {_parent.LineNo}, column \"{sourceColumn}\" could not convert value \"{value}\" to the target type of \"{metaData.TargetProperty.PropertyType.Name}\" for property \"{metaData.TargetProperty.Name}\"";

                        throw new TETLConversionException(errorMessage, conversionEx)
                        {
                            LineNumber = _parent.LineNo,
                            TargetProperty = metaData.TargetProperty,
                            TargetType = metaData.TargetProperty.PropertyType,
                            SourceColumn = metaData.ColumnHeader ?? "Column " + metaData.Ordinal,
                            Line = _parent.CurrentLine
                        };
                    }
                }

                return true;
            }

            public bool MoveNext()
            {
                if (_current == null)
                {
                    return Read();
                }

                var ok = _parent.ReadNext();
                return Read();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

    }
}

