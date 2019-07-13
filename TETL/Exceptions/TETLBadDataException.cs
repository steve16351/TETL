// TETL Copyright (c) Steve Hart. All Rights Reserved.
// Licensed under the MIT Licence. See LICENSE in the project root for license information.
using System;

namespace TETL.Exceptions
{
    /// <summary>
    /// Exception type indicating that the source data is bad,
    /// for example when a line contains an insufficient number of fields
    /// </summary>
    public class TETLBadDataException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TETLBadDataException" /> class
        /// </summary>
        /// <param name="message">Message providing details of the bad data</param>
        public TETLBadDataException(string message) : base(message)
        {
        }

        /// <summary>
        /// Gets or sets the line number where the exception occurred
        /// </summary>
        public int LineNo { get; set; }

        /// <summary>
        /// Gets or sets the data of the line where the exception occurred
        /// </summary>
        public string[] Line { get; set; }
    }
}
