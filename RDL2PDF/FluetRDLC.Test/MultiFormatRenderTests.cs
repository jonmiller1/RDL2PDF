using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluetRDLC.Test
{
    [TestClass]
    public class MultiFormatRenderTests
    {
        [TestMethod]
        public void TestPdfRenderer()
        {
            var renderer = new RDLCRenderer();
            var salesData = TestDataFactory.CreateSalesData();
            renderer.AddObjectDataSource("SalesData", salesData);
            
            var rdlcContent = @"<?xml version='1.0' encoding='utf-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <Body>
    <ReportItems>
      <Textbox Name='Title'>
        <CanGrow>true</CanGrow>
        <KeepTogether>true</KeepTogether>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Sales Report - PDF Format</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Top>0in</Top>
        <Left>0in</Left>
        <Height>0.25in</Height>
        <Width>6in</Width>
      </Textbox>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>6.5in</Width>
</Report>";

            var pdfBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.PDF);
            Assert.IsTrue(pdfBytes.Length > 1000, "PDF should be generated with substantial content");
            
            File.WriteAllBytes(GetOutputPath("MultiFormat_Test.pdf"), pdfBytes);
        }

        [TestMethod]
        public void TestHtmlRenderer()
        {
            var renderer = new RDLCRenderer();
            var salesData = TestDataFactory.CreateSalesData();
            renderer.AddObjectDataSource("SalesData", salesData);
            
            var rdlcContent = @"<?xml version='1.0' encoding='utf-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <Body>
    <ReportItems>
      <Textbox Name='Title'>
        <CanGrow>true</CanGrow>
        <KeepTogether>true</KeepTogether>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Sales Report - HTML Format</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Top>0in</Top>
        <Left>0in</Left>
        <Height>0.25in</Height>
        <Width>6in</Width>
      </Textbox>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>6.5in</Width>
</Report>";

            var htmlBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.HTML);
            var htmlContent = System.Text.Encoding.UTF8.GetString(htmlBytes);
            
            Assert.IsTrue(htmlBytes.Length > 500, "HTML should be generated with substantial content");
            Assert.IsTrue(htmlContent.Contains("<!DOCTYPE html>"), "Should be valid HTML");
            Assert.IsTrue(htmlContent.Contains("Sales Report - HTML Format"), "Should contain report content");
            
            File.WriteAllText(GetOutputPath("MultiFormat_Test.html"), htmlContent);
        }

        [TestMethod]
        public void TestWordRenderer()
        {
            var renderer = new RDLCRenderer();
            var salesData = TestDataFactory.CreateSalesData();
            renderer.AddObjectDataSource("SalesData", salesData);
            
            var rdlcContent = @"<?xml version='1.0' encoding='utf-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <Body>
    <ReportItems>
      <Textbox Name='Title'>
        <CanGrow>true</CanGrow>
        <KeepTogether>true</KeepTogether>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Sales Report - Word Format</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Top>0in</Top>
        <Left>0in</Left>
        <Height>0.25in</Height>
        <Width>6in</Width>
      </Textbox>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>6.5in</Width>
</Report>";

            var wordBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.Word);
            var wordContent = System.Text.Encoding.UTF8.GetString(wordBytes);
            
            Assert.IsTrue(wordBytes.Length > 500, "Word document should be generated with substantial content");
            Assert.IsTrue(wordContent.Contains("Sales Report - Word Format"), "Should contain report content");
            
            File.WriteAllText(GetOutputPath("MultiFormat_Test_Word.html"), wordContent);
        }

        [TestMethod]
        public void TestExcelRenderer()
        {
            var renderer = new RDLCRenderer();
            var salesData = TestDataFactory.CreateSalesData();
            renderer.AddObjectDataSource("SalesData", salesData);
            
            var rdlcContent = @"<?xml version='1.0' encoding='utf-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <Body>
    <ReportItems>
      <Textbox Name='Title'>
        <CanGrow>true</CanGrow>
        <KeepTogether>true</KeepTogether>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Sales Report - Excel Format</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Top>0in</Top>
        <Left>0in</Left>
        <Height>0.25in</Height>
        <Width>6in</Width>
      </Textbox>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>6.5in</Width>
</Report>";

            var excelBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.Excel);
            var excelContent = System.Text.Encoding.UTF8.GetString(excelBytes);
            
            Assert.IsTrue(excelBytes.Length > 20, "Excel document should be generated with substantial content");
            Assert.IsTrue(excelContent.Contains("Sales Report - Excel Format"), "Should contain report content");
            
            File.WriteAllText(GetOutputPath("MultiFormat_Test_Excel.csv"), excelContent);
        }

        [TestMethod]
        public void TestAllRenderFormats()
        {
            var formats = RendererFactory.GetSupportedFormats();
            Assert.IsTrue(formats.Contains(RenderFormat.PDF), "Should support PDF");
            Assert.IsTrue(formats.Contains(RenderFormat.HTML), "Should support HTML");
            Assert.IsTrue(formats.Contains(RenderFormat.Word), "Should support Word");
            Assert.IsTrue(formats.Contains(RenderFormat.Excel), "Should support Excel");
            Assert.IsTrue(formats.Contains(RenderFormat.PNG), "Should support PNG");
        }

        [TestMethod]
        public void TestRendererFactory()
        {
            Assert.AreEqual(".pdf", RendererFactory.GetFileExtension(RenderFormat.PDF));
            Assert.AreEqual(".html", RendererFactory.GetFileExtension(RenderFormat.HTML));
            Assert.AreEqual(".html", RendererFactory.GetFileExtension(RenderFormat.Word));
            Assert.AreEqual(".csv", RendererFactory.GetFileExtension(RenderFormat.Excel));
            Assert.AreEqual(".png", RendererFactory.GetFileExtension(RenderFormat.PNG));
            
            Assert.AreEqual("application/pdf", RendererFactory.GetMimeType(RenderFormat.PDF));
            Assert.AreEqual("text/html", RendererFactory.GetMimeType(RenderFormat.HTML));
            Assert.AreEqual("text/html", RendererFactory.GetMimeType(RenderFormat.Word));
            Assert.AreEqual("text/csv", RendererFactory.GetMimeType(RenderFormat.Excel));
            Assert.AreEqual("image/png", RendererFactory.GetMimeType(RenderFormat.PNG));
        }

        private static string GetOutputPath(string fileName)
        {
            var outputDir = @"C:\Output";
            Directory.CreateDirectory(outputDir);
            return Path.Combine(outputDir, fileName);
        }
    }
}