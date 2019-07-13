// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;
using System.Reflection;
using TETL.Attributes;
using TETL.Converters;

namespace TETL
{
    /// <summary>
    /// Column meta data, the position of the column, the column heading,
    /// the mapping information to the target object
    /// </summary>
    /// <typeparam name="T">Type of the class we are serializing or deserializing from</typeparam>
    public class ColumnMetaData<T> where T : new()
    {
        /// <summary>
        /// The converter that will transform to/from string values
        /// to the target property's actual type
        /// </summary>
        private IConvertAndSet _converter;

        /// <summary>
        /// Gets or sets the ordinal of this column in the text file
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// Gets or sets the column heading in the text file
        /// </summary>
        public string ColumnHeader { get; set; }

        /// <summary>
        /// Gets or sets the property on the target type
        /// </summary>
        public PropertyInfo TargetProperty { get; set; }

        /// <summary>
        /// Gets or sets the text-file mapping attribute of the column
        /// </summary>
        public TextFileMappingAttribute MappingAttribute { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is this relevant for serialization
        /// or deserialization i.e. does it have a mapping to/from the target type
        /// </summary>
        public bool Ignore
        {
            get
            {
                return this.TargetProperty == null;
            }
        }

        /// <summary>
        /// Does this mapping attribute match this column?
        /// </summary>
        /// <param name="mappingAttribute">Mapping attribute to check</param>
        /// <returns>True if a match, via either the column name or the ordinal</returns>
        public bool IsMatch(TextFileMappingAttribute mappingAttribute)
        {
            if (mappingAttribute.InternalColumnOrdinal.HasValue && this.Ordinal == mappingAttribute.InternalColumnOrdinal.Value)
                return true;
            if (mappingAttribute.ColumnName != null && this.ColumnHeader != null && this.ColumnHeader.Equals(mappingAttribute.ColumnName, StringComparison.OrdinalIgnoreCase))
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
            this.TargetProperty = target;
            this.MappingAttribute = mappingAttribute;
            _converter = StringConverterFactory<T>.Get(this.TargetProperty, this.MappingAttribute);
        }        

        /// <summary>
        /// Retrieves the string value of this column
        /// </summary>
        /// <param name="target">Target type</param>
        /// <returns>Value of the column from the target, as a string</returns>
        public string GetValue(T target)
        {
            return _converter.GetValue(target);
        }
        
        /// <summary>
        /// Gets the value on the target type, from a string
        /// </summary>
        /// <param name="target">Target type</param>
        /// <param name="value">String value to convert to the target property type</param>
        public void SetValue(T target, string value)
        {
            _converter.SetValue(target, value);
        }
    }
}
