using System;
using System.IO;
using System.Text;
using TETL.Attributes;

namespace TETL.Tests.Mocks
{
    public class MockData
    {
        [TextFileMappingAttribute(ColumnName = "Name", ColumnOrdinal = 0)]
        public string Name { get; set; }
        [TextFileMappingAttribute(ColumnName = "Weight", ColumnOrdinal = 1)]
        public double Weight { get; set; }
        [TextFileMappingAttribute(ColumnName = "Height", ColumnOrdinal = 2)]
        public int Height { get; set; }
        [TextFileMappingAttribute(ColumnName = "DateOfBirth", ColumnOrdinal = 3, DateTimeFormat = "yyyyMMdd")]
        public DateTime? DateOfBirth { get; set; }
        [TextFileMappingAttribute(ColumnName = "IsMale", ColumnOrdinal = 4)]
        public bool IsMale { get; set; }


        public static Stream GetDataNoHeaderRow()
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 32768, true))
            {                
                writer.WriteLine("Fred;71.3;165;19870521;1");
                writer.WriteLine("Andy;80.2;180; ;1");
                writer.WriteLine("Jane;63.5;160;19890622;0");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public static Stream GetDataWithHeaderRow()
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 32768, true))
            {
                writer.WriteLine("Name;Weight;Height;DateOfBirth;IsMale");
                writer.WriteLine("Fred;71.3;165;19870521;1");
                writer.WriteLine("Andy;80.2;180; ;1");
                writer.WriteLine("Jane;63.5;160;19890622;0");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public static Stream GetDataWithHeaderRowAndFooter()
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 32768, true))
            {
                writer.WriteLine("Name;Weight;Height;DateOfBirth;IsMale");
                writer.WriteLine("Fred;71.3;165;19870521;1");
                writer.WriteLine("Andy;80.2;180; ;1");
                writer.WriteLine("Jane;63.5;160;19890622;0");
                writer.WriteLine("End Of File");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
