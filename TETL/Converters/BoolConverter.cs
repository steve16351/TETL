// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;

namespace TETL.Converters
{
    /// <summary>
    /// Provides get/set capability for boolean properties to strings
    /// </summary>
    /// <typeparam name="T">Target object type</typeparam>
    public class BoolConverter<T> : BaseConverter<T, bool>, IConvertAndSet
    {
        /// <summary>
        /// Parse a boolean value from a string value
        /// </summary>
        /// <remarks>
        /// This will use standard string to bool conversion methods first,
        /// but will also handle integer to bool conversion, accepting strings
        /// that represent 1 or 0 and converting to true or false respectively.
        /// </remarks>
        /// <param name="value">String value to parse to a boolean</param>
        /// <returns>True or false</returns>
        public static bool ParseBool(string value)
        {
            bool convertedValue;
            if (bool.TryParse(value, out convertedValue))
                return convertedValue;

            int integerValue;
            if (int.TryParse(value, out integerValue))
            {
                if (integerValue == 0) return false;
                if (integerValue == 1) return true;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Converts and sets a value on the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="value">Value to set as a string which will be converted</param>
        public override void SetValue(object target, string value)
        {
            this.Setter((T)target, ParseBool(value));
        }

        /// <summary>
        /// Converts and gets a value from the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Boolean string value</returns>
        public override string GetValue(object target)
        {
            return this.Getter((T)target).ToString();
        }
    }
}
