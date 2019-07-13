// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.

using System;

namespace TETL.Attributes
{
    /// <summary>
    /// Attribute for database mapping, allowing the property to be mapped
    /// to a specific column with-in a database table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DatabaseMappingAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMappingAttribute" /> class
        /// </summary>
        public DatabaseMappingAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMappingAttribute" /> class
        /// </summary>
        /// <param name="databaseColumn">Database column name</param>
        public DatabaseMappingAttribute(string databaseColumn)
        {
            this.DatabaseColumn = databaseColumn;
        }

        /// <summary>
        /// Gets or sets the name of the database column to map to
        /// </summary>
        public string DatabaseColumn { get; set; }

        /// <summary>
        /// Check if the attribute is correctly specified, 
        /// it must have a database column name
        /// </summary>
        public void ThrowIfInvalid()
        {
            if (string.IsNullOrWhiteSpace(this.DatabaseColumn))
                throw new ArgumentException("DatabaseColumn not specified");
        }
    }
}
