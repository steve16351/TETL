// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;
using TETL.Attributes;

namespace TETL.Converters
{
    /// <summary>
    /// Provides get/set capability for DateTime properties to strings
    /// </summary>
    /// <typeparam name="T">Target object type</typeparam>
    public class DateTimeConverter<T> : BaseConverter<T, DateTime>, IConvertAndSet
    {
        /// <summary>
        /// Gets a date as a string, applying the appropriate formatting
        /// specified in the text file mapping attribute, if specified
        /// </summary>
        /// <param name="value">DateTime value to format</param>
        /// <param name="mappingAttribute">Mapping attribute with formatting information</param>
        /// <returns>Formatted DateTime string</returns>
        public static string GetDateAsString(DateTime value, TextFileMappingAttribute mappingAttribute)
        {
            if (mappingAttribute.DateTimeFormat != null)
                return value.ToString(mappingAttribute.DateTimeFormat);

            return value.ToString();
        }

        /// <summary>
        /// Parse a string value to a DateTime, using formatting
        /// hints provided in the TextFileMappingAttribute associated
        /// with the property, if provided.
        /// </summary>
        /// <param name="value">String value to parse to a DateTime</param>
        /// <param name="mappingAttribute">Mapping attribute for the property</param>
        /// <returns>DateTime parsed from value</returns>
        public static DateTime ParseDate(string value, TextFileMappingAttribute mappingAttribute)
        {
            DateTime parsedDate = DateTime.MinValue;

            if (mappingAttribute.DateTimeFormat != null)
                parsedDate = DateTime.ParseExact(value, mappingAttribute.DateTimeFormat, null);
            else
                parsedDate = DateTime.Parse(value);

            return parsedDate;
        }

        /// <summary>
        /// Converts and sets a value on the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="value">Value to set as a string which will be converted</param>
        public override void SetValue(object target, string value)
        {
            this.Setter((T)target, ParseDate(value, this.MappingAttribute));
        }

        /// <summary>
        /// Converts and gets a value from the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Boolean string value</returns>
        public override string GetValue(object target)
        {
            return GetDateAsString(this.Getter((T)target), this.MappingAttribute);
        }
    }
}
