// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System;

namespace TETL.Converters
{
    /// <summary>
    /// Provides get/set capability for Int32 properties to strings
    /// </summary>
    /// <typeparam name="T">Target object type</typeparam>
    public class Int32Converter<T> : BaseConverter<T, int>, IConvertAndSet
    {
        /// <summary>
        /// Converts and sets a value on the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="value">Value to set as a string which will be converted</param>
        public override void SetValue(object target, string value)
        {
            this.Setter((T)target, int.Parse(value));
        }

        /// <summary>
        /// Converts and gets a value from the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Boolean string value</returns>
        public override string GetValue(object target)
        {
            return Getter((T)target).ToString();
        }
    }
}
