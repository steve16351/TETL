// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System;
using System.Reflection;

namespace TETL.Exceptions
{
    /// <summary>
    /// Exception type indicating that a conversion to/from string failed on
    /// some column, due to bad input data
    /// </summary>
    public class TETLConversionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TETLConversionException" /> class
        /// </summary>
        /// <param name="message">Message providing details of the conversion data</param>
        /// <param name="baseException">Base conversion exception</param>
        public TETLConversionException(string message, Exception baseException) : base(message, baseException)
        {
        }

        /// <summary>
        /// Gets or sets the data of the line where the exception occurred
        /// </summary>
        public string[] Line { get; set; }

        /// <summary>
        /// Gets or sets the text file line-number where the exception occurred
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the target type for the failing conversion
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Gets or sets the target property for the failing conversion
        /// </summary>
        public PropertyInfo TargetProperty { get; set; }

        /// <summary>
        /// Gets or sets the source column name from the text-file where the conversion failure occurred
        /// </summary>
        public string SourceColumn { get; set; }
    }
}
