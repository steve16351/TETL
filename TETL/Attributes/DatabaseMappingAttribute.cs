using System;

namespace TETL.Attributes
{
    public class DatabaseMappingAttribute : Attribute
    {
        public DatabaseMappingAttribute()
        {

        }

        public DatabaseMappingAttribute(string databaseColumn)
        {
            DatabaseColumn = databaseColumn;
        }

        public string DatabaseColumn { get; set; }

        public void ThrowIfInvalid()
        {
            if (String.IsNullOrWhiteSpace(DatabaseColumn))
                throw new ArgumentException("DatabaseColumn not specified");
        }

    }
}
