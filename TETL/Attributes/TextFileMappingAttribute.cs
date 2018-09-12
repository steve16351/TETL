using System;

namespace TETL.Attributes
{
    public class TextFileMappingAttribute : Attribute
    {
        public TextFileMappingAttribute()
        {

        }

        public TextFileMappingAttribute(string columnName)
        {
            ColumnName = columnName;
        }

        public string ColumnName { get; set; }     
        public int ColumnOrdinal
        {
            get
            {
                return InternalColumnOrdinal.HasValue ? InternalColumnOrdinal.Value : 0;
            }
            set
            {
                InternalColumnOrdinal = value;
            }
        }

        internal Nullable<int> InternalColumnOrdinal { get; set; }

        public string DateTimeFormat { get; set; }        

        internal void ThrowIfInvalid()
        {
            if (!InternalColumnOrdinal.HasValue && String.IsNullOrWhiteSpace(ColumnName))
                throw new ArgumentException($"You must specify either the column name or column ordinal");
        }
    }
}
