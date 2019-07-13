// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;
using System.Reflection;
using TETL.Attributes;

namespace TETL.Converters
{
    /// <summary>
    /// Base converter type, providing the skeleton of all
    /// converter type classes
    /// </summary>
    /// <typeparam name="TRow">Target row type</typeparam>
    /// <typeparam name="TTarget">Target type</typeparam>
    public abstract class BaseConverter<TRow, TTarget>
    {
        /// <summary>
        /// Gets or sets the setting action when setting a property
        /// to a give value
        /// </summary>
        protected Action<TRow, TTarget> Setter { get; set; }

        /// <summary>
        /// Gets or sets the getting action when retrieving a value
        /// from the property
        /// </summary>
        protected Func<TRow, TTarget> Getter { get; set; }

        /// <summary>
        /// Gets or sets the mapping attribute providing details
        /// on the target property and other mapping information
        /// </summary>
        protected TextFileMappingAttribute MappingAttribute { get; set; }

        /// <summary>
        /// Setup the converter, assigning it to a property and
        /// providing additional mapping information
        /// </summary>
        /// <param name="propertyInfo">Property this converter is assigned to</param>
        /// <param name="mapper">Additional mapping information</param>
        public virtual void Setup(PropertyInfo propertyInfo, TextFileMappingAttribute mapper)
        {
            this.MappingAttribute = mapper;
            this.Setter = (Action<TRow, TTarget>)propertyInfo.GetSetMethod().CreateDelegate(typeof(Action<TRow, TTarget>));
            this.Getter = (Func<TRow, TTarget>)propertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TRow, TTarget>));
        }

        /// <summary>
        /// Sets the value on the target object to the specified
        /// string value for the property this converter is for
        /// </summary>
        /// <param name="target">Target object</param>
        /// <param name="value">Value to set</param>
        public abstract void SetValue(object target, string value);

        /// <summary>
        /// Gets the value on the given target object for the
        /// property this converter is for
        /// </summary>
        /// <param name="target">Target object</param>
        /// <returns>Value retrieved</returns>
        public abstract string GetValue(object target);
    }
}
