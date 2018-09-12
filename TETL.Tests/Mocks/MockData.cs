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


        public static Stream GetDataNoHeaderRow()
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 32768, true))
            {                
                writer.WriteLine("Fred;71.3;165");
                writer.WriteLine("Andy;80.2;180");
                writer.WriteLine("Jane;63.5;160");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public static Stream GetDataWithHeaderRow()
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 32768, true))
            {
                writer.WriteLine("Name;Weight;Height");
                writer.WriteLine("Fred;71.3;165");
                writer.WriteLine("Andy;80.2;180");
                writer.WriteLine("Jane;63.5;160");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public static Stream GetDataWithHeaderRowAndFooter()
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, 32768, true))
            {
                writer.WriteLine("Name;Weight;Height");
                writer.WriteLine("Fred;71.3;165");
                writer.WriteLine("Andy;80.2;180");
                writer.WriteLine("Jane;63.5;160");
                writer.WriteLine("End Of File");
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
