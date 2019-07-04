using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TETL.Attributes;
using TETL.Converters;
using TETL.Tests.Mocks;
using TETL.Tests.Utils;

namespace TETL.Tests
{
    [TestClass]
    public class TextFileSerializerTests
    {
        [TestMethod]
        public void TestTextFileSerializer_WillSkipFooterRow_WithHeaderRow()
        {
            using (var data = MockData.GetDataWithHeaderRowAndFooter())
            {
                TextFileSerializer<MockData> tfs = new TextFileSerializer<MockData>(data)
                {
                    Delimiter = ";",
                    FirstRowHeader = true,
                    SkipFooterRows = 1
                };

                string[] lines = data.ReadLines();
                var rowCount = tfs.Count();
                Assert.AreEqual(rowCount, lines.Count() - 2);
                Assert.AreEqual(tfs.LineNo, lines.Count() - tfs.SkipFooterRows);
            }
        }

        public class MyObject {
            [TextFileMapping("MyDecimal")]
            public decimal MyProperty { get; set; }
        }

        [TestMethod]
        public void TestDecimalConverter_CanHandleExponential()
        {
            DecimalConverter<MyObject> converter = new DecimalConverter<MyObject>();
            MyObject o = new MyObject();
            var myProp = typeof(MyObject).GetProperty("MyProperty");
            converter.Setup(myProp, new TextFileMappingAttribute() { ColumnName = "MyDecimal" });
            converter.SetValue(o, "3.08600834621439E-05");
            Assert.IsTrue(o.MyProperty == 3.08600834621439E-05M);
        }

        [TestMethod]
        public void TestTextFileSerializer_LineNo_IncrementsCorrectly_WithHeaderRow()
        {
            using (var data = MockData.GetDataWithHeaderRow())
            {
                TextFileSerializer<MockData> tfs = new TextFileSerializer<MockData>(data)
                {
                    Delimiter = ";",
                    FirstRowHeader = true
                };

                string[] lines = data.ReadLines();
                var enumerator = tfs.GetEnumerator();
                int lastLineNo = 0;

                for (int i = 0; i < (lines.Length - 1); i++)
                {
                    enumerator.MoveNext();
                    var currentMock = enumerator.Current;
                    lastLineNo = tfs.LineNo;
                    Assert.IsTrue(lastLineNo == i + 2);
                }

                Assert.IsTrue(lastLineNo == lines.Length);
            };
        }


        [TestMethod]
        public void TestTextFileSerializer_WillHandle_FieldInQuotes()
        {
            using (var data = MockData.GetDataNoHeaderRow())
            {
                TextFileSerializer<MockData> tfs = new TextFileSerializer<MockData>(data)
                {
                    Delimiter = ";",
                    FirstRowHeader = false,
                    FieldsInQuotes = true
                };

                string[] lines = data.ReadLines();
                var enumerator = tfs.GetEnumerator();

                var firstRow = tfs.First();
                Assert.AreEqual(firstRow.Comment, "Hello;World");
            };
        }


        [TestMethod]
        public void TestTextFileSerializer_LineNo_IncrementsCorrectly_WithNoHeaderRow()
        {
            using (var data = MockData.GetDataNoHeaderRow())
            {
                TextFileSerializer<MockData> tfs = new TextFileSerializer<MockData>(data)
                {
                    Delimiter = ";",
                    FirstRowHeader = false
                };

                string[] lines = data.ReadLines();
                var enumerator = tfs.GetEnumerator();
                int lastLineNo = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    enumerator.MoveNext();
                    lastLineNo = tfs.LineNo;
                    Assert.IsTrue(lastLineNo == i + 1);
                }

                Assert.IsTrue(lastLineNo == lines.Length);
            };
        }
    }
}
