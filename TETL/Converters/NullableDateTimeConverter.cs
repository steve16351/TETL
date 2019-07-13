// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;

namespace TETL.Converters
{
    /// <summary>
    /// Provides get/set capability for nullable DateTime properties to strings
    /// </summary>
    /// <typeparam name="T">Target object type</typeparam>
    public class NullableDateTimeConverter<T> : BaseConverter<T, DateTime?>, IConvertAndSet
    {
        /// <summary>
        /// Converts and sets a value on the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="value">Value to set as a string which will be converted</param>
        public override void SetValue(object target, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            this.Setter((T)target, DateTimeConverter<DateTime>.ParseDate(value, this.MappingAttribute));
        }

        /// <summary>
        /// Converts and gets a value from the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Boolean string value</returns>
        public override string GetValue(object target)
        {
            DateTime? value = Getter((T)target);
            if (value.HasValue == false) return string.Empty;
            return DateTimeConverter<DateTime>.GetDateAsString(value.Value, this.MappingAttribute);
        }
    }
}
