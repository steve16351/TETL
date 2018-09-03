using System;
using System.Reflection;

namespace TETL.Exceptions
{
    public class TETLConversionException : Exception
    {
        public TETLConversionException(string message, Exception baseException) : base(message, baseException)
        {

        }

        public string[] Line { get; set; }
        public int LineNumber { get; set; }
        public Type TargetType { get; set; }
        public PropertyInfo TargetProperty { get; set; }
        public string SourceColumn { get; set; }
    }
}
