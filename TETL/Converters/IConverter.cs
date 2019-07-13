// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System.Reflection;
using TETL.Attributes;

namespace TETL.Converters
{
    /// <summary>
    /// Interface for a conversion class that can get and set a string value
    /// on a target object of any type, and can be fed a specific property
    /// with additional mapping information
    /// </summary>
    public interface IConvertAndSet
    {
        /// <summary>
        /// Set a string value on the target object for the property this
        /// converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="value">Value to set</param>
        void SetValue(object target, string value);

        /// <summary>
        /// Gets a string value from the target object for the property
        /// this converter is associated with
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Value retrieved</returns>
        string GetValue(object target);

        /// <summary>
        /// Setup the converter so it is associated with a particular
        /// property info which should be present on the target object type.
        /// Also taking additional mapping information in which hints for
        /// conversion can be specified
        /// </summary>
        /// <param name="propertyInfo">PropertyInfo mapping</param>
        /// <param name="mapper">Mapping attribute</param>
        void Setup(PropertyInfo propertyInfo, TextFileMappingAttribute mapper);
    }
}
