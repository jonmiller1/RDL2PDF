using FluentRDLC.Renderer.Renderers;
using System.Data;
using System.Xml.Linq;

namespace FluentRDLC.Renderer
{
    public class SimpleTest
    {
        public static void TestPdfRenderer()
        {
            // Create simple test data
            var dataTable = new DataTable("TestData");
            dataTable.Columns.Add("SKU", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("Quantity", typeof(int));
            dataTable.Columns.Add("UnitPrice", typeof(decimal));
            dataTable.Columns.Add("Subtotal", typeof(decimal));
            
            dataTable.Rows.Add("TEST001", "Test Product", 1, 10.00m, 10.00m);
            dataTable.Rows.Add("TEST002", "Another Product", 2, 15.00m, 30.00m);

            // Create minimal RDLC structure
            var rdlcXml = @"<?xml version='1.0' encoding='utf-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition'>
    <PageWidth>8.5in</PageWidth>
    <PageHeight>11in</PageHeight>
    <LeftMargin>0.5in</LeftMargin>
    <RightMargin>0.5in</RightMargin>
    <TopMargin>0.5in</TopMargin>
    <BottomMargin>0.5in</BottomMargin>
    <Body>
        <Height>10in</Height>
        <ReportItems>
            <Textbox Name='Title'>
                <Top>0.2in</Top>
                <Left>0.2in</Left>
                <Width>7.6in</Width>
                <Height>0.5in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>Test Invoice</Value>
                                <Style>
                                    <FontSize>24pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            <Tablix Name='DataTable'>
                <Top>1in</Top>
                <Left>0.2in</Left>
                <Width>7.6in</Width>
                <Height>2in</Height>
                <DataSetName>TestData</DataSetName>
            </Tablix>
        </ReportItems>
    </Body>
</Report>";

            var rdlcDocument = XDocument.Parse(rdlcXml);
            var rdlcNamespace = rdlcDocument.Root?.Name.Namespace ?? XNamespace.None;

            // Create render context
            var context = new RenderContext
            {
                RdlcDocument = rdlcDocument,
                RdlcNamespace = rdlcNamespace,
                DataSources = new Dictionary<string, DataTable> { { "TestData", dataTable } },
                Parameters = new Dictionary<string, object>()
            };

            // Test the PDF renderer
            var pdfRenderer = new PdfRenderer();
            var pdfBytes = pdfRenderer.Render(context);

            // Save to output
            var outputPath = @"C:\Output\SimpleTest.pdf";
            Directory.CreateDirectory(@"C:\Output");
            File.WriteAllBytes(outputPath, pdfBytes);

            Console.WriteLine($"PDF generated successfully: {outputPath}");
            Console.WriteLine($"PDF size: {pdfBytes.Length} bytes");
        }
    }
}