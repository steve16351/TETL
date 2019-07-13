// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using TETL.Exceptions;

namespace TETL
{
    /// <summary>
    /// Enumerates a text file instance, transformed into a 
    /// target object type
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    public class TextFileEnumerator<T> : IEnumerator<T> where T : new()
    {
        /// <summary>
        /// Parent serializer instance
        /// </summary>
        private TextFileSerializer<T> _parent;

        /// <summary>
        /// Current instance of the target type for the current row
        /// </summary>
        private T _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileEnumerator{T}" /> class
        /// </summary>
        /// <param name="parent">Serializer instance</param>
        public TextFileEnumerator(TextFileSerializer<T> parent)
        {
            _parent = parent;
        }
        
        /// <summary>
        /// Gets the current item position in the enumeration
        /// </summary>
        T IEnumerator<T>.Current
        {
            get { return _current; }
        }

        /// <summary>
        /// Gets the current item position in the enumeration
        /// </summary>
        public object Current
        {
            get { return _current; }
        }
                
        /// <summary>
        /// Clean up managed resources created
        /// </summary>
        public void Dispose()
        {
            return;
        }

        /// <summary>
        /// Move to the next item in the iteration
        /// </summary>
        /// <returns>True if move forward, false if the end</returns>
        public bool MoveNext()
        {
            if (_current == null)
            {
                return Read();
            }

            var ok = _parent.ReadNext();
            return Read();
        }

        /// <summary>
        /// Reset to the first position
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Read; progressing forward to the next line in the iteration
        /// </summary>
        /// <returns>True if a line was read, false if the end</returns>
        private bool Read()
        {
            if (_parent.CurrentLine == null) return false;
            _current = new T();

            foreach (ColumnMetaData<T> metaData in _parent.RelevantMetaData)
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
    }
}
