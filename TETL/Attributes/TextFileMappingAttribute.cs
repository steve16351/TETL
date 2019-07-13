// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;

namespace TETL.Attributes
{
    /// <summary>
    /// Attribute for text file mapping, indicating the column name of the
    /// text file this maps to, or the column ordinal. Also provides
    /// parsing hints for date times.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TextFileMappingAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileMappingAttribute" /> class
        /// </summary>
        public TextFileMappingAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileMappingAttribute" /> class
        /// </summary>
        /// <param name="columnName">Text file column name</param>
        public TextFileMappingAttribute(string columnName)
        {
            this.ColumnName = columnName;
        }

        /// <summary>
        /// Gets or sets the name of the column the property refers to
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the ordinal of the column the property refers to
        /// </summary>
        public int ColumnOrdinal
        {
            get
            {
                return this.InternalColumnOrdinal.HasValue ? this.InternalColumnOrdinal.Value : 0;
            }

            set
            {
                this.InternalColumnOrdinal = value;
            }
        }

        /// <summary>
        /// Gets or sets the expected DateTime format if the property/column is a DateTime
        /// </summary>
        public string DateTimeFormat { get; set; }

        /// <summary>
        /// Gets or sets the internal column ordinal reference used during serialization
        /// </summary>
        internal int? InternalColumnOrdinal { get; set; }

        /// <summary>
        /// Throw an exception if the attribute is not correctly specified
        /// </summary>
        internal void ThrowIfInvalid()
        {
            if (!this.InternalColumnOrdinal.HasValue && string.IsNullOrWhiteSpace(this.ColumnName))
                throw new ArgumentException($"You must specify either the column name or column ordinal");
        }
    }
}
