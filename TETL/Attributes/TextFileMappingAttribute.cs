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
        public int? ColumnOrdinal { get; set; }
        public string DateTimeFormat { get; set; }

        internal void ThrowIfInvalid()
        {
            if (ColumnOrdinal.HasValue && String.IsNullOrWhiteSpace(ColumnName) == false)
                throw new ArgumentException($"You should not specify both the ColumnName ({ColumnName}) AND the ColumnOrdinal");
        }
    }
}
