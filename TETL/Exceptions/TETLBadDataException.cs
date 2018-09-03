using System;

namespace TETL.Exceptions
{
    public class TETLBadDataException : Exception
    {
        public TETLBadDataException(string message) : base (message)
        {

        }

        public int LineNo { get; set; }
        public string[] Line { get; set; }
    }
}
