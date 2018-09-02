using System;

namespace TETL.Attributes
{
    public class DatabaseMappingAttribute : Attribute
    {
        public string DatabaseColumn { get; set; }

        public void ThrowIfInvalid()
        {
            if (String.IsNullOrWhiteSpace(DatabaseColumn))
                throw new ArgumentException("DatabaseColumn not specified");
        }

    }
}
