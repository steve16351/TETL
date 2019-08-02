// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TETL.Attributes;

namespace TETL
{
    /// <summary>
    /// Text file serializer to handle reading of text files into 
    /// a target object type
    /// </summary>
    /// <typeparam name="T">Target object type</typeparam>
    public class TextFileSerializer<T> : IDisposable, IEnumerable<T> where T : new()
    {
        /// <summary>
        /// Read stream from the text file
        /// </summary>
        private StreamReader _readStream = null;

        /// <summary>
        /// Write stream to the text file
        /// </summary>
        private StreamWriter _writeStream = null;

        /// <summary>
        /// Input stream of the text file
        /// </summary>
        private Stream _inputStream;

        /// <summary>
        /// Text file name
        /// </summary>
        private string _fileName = null;

        /// <summary>
        /// Flag to indicate if write mode initialized
        /// </summary>
        private bool _writeInitialised = false;

        /// <summary>
        /// Flag to indicate whether the serializer has been initialized
        /// </summary>
        private bool _isInitialised = false;

        /// <summary>
        /// Read buffer from the source file
        /// </summary>
        private Queue<string> _readBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileSerializer{T}" /> class
        /// </summary>
        /// <param name="stream">Source input stream</param>
        public TextFileSerializer(Stream stream) : this()
        {
            _inputStream = stream;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileSerializer{T}" /> class
        /// </summary>
        /// <param name="fileName">Source text file</param>
        public TextFileSerializer(string fileName) : this()
        {
            _fileName = fileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileSerializer{T}" /> class
        /// </summary>
        public TextFileSerializer()
        {
        }

        /// <summary>
        /// Predicate delegate, used for skipping rows
        /// </summary>
        /// <param name="row">Row to skip</param>
        /// <returns>True if row should be skipped in enumeration</returns>
        public delegate bool SkipRow(T row);

        /// <summary>
        /// Gets or sets the number of rows at the top of the file to skip
        /// </summary>
        public int SkipHeaderRows { get; set; }

        /// <summary>
        /// Gets or sets the number of rows to skip at the end of the file
        /// </summary>
        public int SkipFooterRows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first row contains column headings
        /// </summary>
        public bool FirstRowHeader { get; set; }

        /// <summary>
        /// Gets or sets the delimiter to spit rows into columns
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to check for fields enclosed with quotes to escape delimiters
        /// </summary>
        public bool FieldsInQuotes { get; set; }

        /// <summary>
        /// Gets or sets column meta data
        /// </summary>
        public ColumnMetaData<T>[] MetaData { get; set; }

        /// <summary>
        /// Gets or sets relevant meta data for columns involved in the target object
        /// </summary>
        public ColumnMetaData<T>[] RelevantMetaData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to append write records to an existing file rather than
        /// creating a new file, adding headings, etc.
        /// </summary>
        public bool AppendMode { get; set; }

        /// <summary>
        /// Gets a value indicating whether we are currently at the end of the file
        /// </summary>
        public bool IsEOF
        {
            get
            {
                return _readStream.EndOfStream;
            }
        }

        /// <summary>
        /// Gets the current line being processed, by column
        /// </summary>
        public string[] CurrentLine { get; private set; }

        /// <summary>
        /// Gets the current line number we are on
        /// </summary>
        public int LineNo { get; private set; }

        /// <summary>
        /// Gets or sets preamble lines, when writing the text file from objects, any lines
        /// you wish to appear at the top of the file before headings or the data
        /// </summary>
        public string[] PreambleLines { get; set; }        

        /// <summary>
        /// Gets or sets a predicate which will cause a row to be skipped if matched, optional
        /// </summary>
        public SkipRow SkipPredicate { get; set; }

        /// <summary>
        /// Gets or sets the mapping of properties on the target type to attributes associated with them
        /// </summary>
        private IEnumerable<PropertyToAttributeMap> MappingAttributes { get; set; }

        /// <summary>
        /// If needed, and not in append mode - add skipped lines, 
        /// and headings
        /// </summary>
        /// <param name="preambleLines">Pre-amble lines to add</param>
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
                var headerRow = RelevantMetaData.Select((a, i) => a.ColumnHeader ?? string.Concat("Column " + i))
                    .Aggregate((a, b) => string.Concat(a, Delimiter, b));

                _writeStream.WriteLine(headerRow);
            }

            _writeInitialised = true;
        }

        /// <summary>
        /// Write lines for the specified records to the output stream
        /// </summary>
        /// <param name="records">Records of the target type to serialize to text</param>
        public void WriteLines(IEnumerable<T> records)
        {
            if (Delimiter == null) throw new ArgumentNullException("Delimiter");

            if (_writeInitialised == false)
            {
                WritePreamble(PreambleLines);
            }

            foreach (T record in records)
            {
                var writeData = string.Join(Delimiter, RelevantMetaData.Select(a => a.GetValue(record)));
                _writeStream.WriteLine(writeData);
            }
        }

        /// <summary>
        /// Write the specified record to the output stream
        /// </summary>
        /// <param name="record">Record of target type</param>
        public void WriteLine(T record)
        {
            WriteLines(new[] { record });
        }

        /// <summary>
        /// Clean up resources created by the serializer
        /// </summary>
        public void Dispose()
        {
            _readStream?.Dispose();
            _writeStream?.Dispose();
        }

        /// <summary>
        /// Retrieve an enumerator for the text file
        /// </summary>
        /// <returns>Enumerating instance</returns>
        public IEnumerator<T> GetEnumerator()
        {
            InitialiseRead();
            return new TextFileEnumerator<T>(this);
        }

        /// <summary>
        /// Get a enumerator for the current instance
        /// </summary>
        /// <returns>Enumerator for the data deserialized</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Read the next line from the stream
        /// </summary>
        /// <returns>Value indicating whether the line was read, or false if the end of the file has been reached</returns>
        internal bool ReadNext()
        {
            if (SkipFooterRows > 0) return ReadNextWithBuffer();

            if (_readStream.EndOfStream)
            {
                CurrentLine = null;
                return false; // we reached the end of the file
            }

            LineNo++;

            string nextLine = _readStream.ReadLine();
            CurrentLine = SplitLine(nextLine);
            return true;
        }
                
        /// <summary>
        /// Initialize for write operations
        /// </summary>
        private void InitialiseWrite()
        {
            Initialise();

            if (_inputStream == null && _fileName == null) throw new InvalidOperationException("Expected input stream or file name");

            if (_inputStream == null && _fileName != null)
                _inputStream = new FileStream(_fileName, AppendMode ? FileMode.Append : FileMode.Create);
            
            _writeStream = new StreamWriter(_inputStream);
            
            MetaData = MappingAttributes
                .Select((mappingAttribute, ordinal) => new ColumnMetaData<T>()
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
        /// Initialize the serializer for reading
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
                if (string.IsNullOrWhiteSpace(headerRow))
                    throw new InvalidOperationException($"No header row found but one was expected");

                MetaData = SplitLine(headerRow)
                    .Select((a, i) => new ColumnMetaData<T>() { ColumnHeader = a.Trim(), Ordinal = i })
                    .ToArray();
            }

            if (_readStream.EndOfStream)
                return; // file is empty, nothing to do

            ReadNext();

            if (!FirstRowHeader)
            {
                MetaData = CurrentLine
                    .Select((a, i) => new ColumnMetaData<T>() { ColumnHeader = null, Ordinal = i })
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
            CurrentLine = SplitLine(nextLine);
            return true;
        }
        
        /// <summary>
        /// Split the specified line based upon the
        /// delimited defined for the text file,
        /// and check for escaping via quotes
        /// </summary>
        /// <param name="line">Source line to split</param>
        /// <returns>Line split to fields</returns>
        private string[] SplitLine(string line)
        {
            if (!FieldsInQuotes)
                return line.Split(new[] { Delimiter }, StringSplitOptions.None);

            return SplitQuotedLine(line, Delimiter)
                .ToArray();
        }
        
        /// <summary>
        /// Split a quoted line - i.e. check whether the delimiter is escaped
        /// </summary>
        /// <param name="line">Source text file line</param>
        /// <param name="delimiter">Delimiter to split by</param>
        /// <returns>Line split to fields, taking note of any quotes that escape delimiters</returns>
        private List<string> SplitQuotedLine(string line, string delimiter)
        {
            StringBuilder builder = new StringBuilder();
            int delimiterLength = delimiter.Length;
            List<string> values = new List<string>();
            int quoteSeen = 0;

            for (int i = 0; i < line.Length; i++)
            {
                string current = line[i].ToString();
                string maybeDelim = line.Substring(i, Math.Min(delimiter.Length, line.Length - i));

                if (current.Equals("\""))
                    quoteSeen++;

                bool isDelimiter = maybeDelim.Equals(delimiter) && (quoteSeen % 2 == 0);

                if (!isDelimiter)
                    builder.Append(current);
                else
                {
                    quoteSeen = 0;
                    if (delimiter.Length > 1) i = i + delimiter.Length - 1;
                    values.Add(builder.ToString().TrimEnd('"').TrimStart('"').Replace("\"\"", "\""));
                    builder = new StringBuilder();
                }
            }

            values.Add(builder.ToString().TrimEnd('"').TrimStart('"').Replace("\"\"", "\""));
            return values;
        }

        /// <summary>
        /// Internal helper class to hold together a property info type
        /// and corresponding mapping attribute that is associated with it
        /// </summary>
        private class PropertyToAttributeMap
        {
            /// <summary>
            /// Gets or sets the property info
            /// </summary>
            public PropertyInfo Property { get; set; }

            /// <summary>
            /// Gets or sets the mapping attribute
            /// </summary>
            public TextFileMappingAttribute Mapping { get; set; }
        }
    }
}