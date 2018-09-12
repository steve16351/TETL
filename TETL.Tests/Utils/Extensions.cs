using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TETL.Tests.Utils
{
    public static class Extensions
    {
        public static string[] ReadLines(this Stream stream)
        {
            List<string> lines = new List<string>();
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            stream.Position = 0;
            return lines.ToArray();
        }
    }
}
