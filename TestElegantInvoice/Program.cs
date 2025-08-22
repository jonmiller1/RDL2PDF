using FluentRDLC.Renderer.Renderers;
using System.Data;
using System.Xml.Linq;
using FluentRDLC.Renderer;

class Program
{
    static void Main()
    {
        try 
        {
            Console.WriteLine("Starting Elegant Invoice generation test...");
            
            // Create test data similar to Elegant Invoice
            var dataTable = new DataTable("InvoiceItems");
            dataTable.Columns.Add("SKU", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("Quantity", typeof(int));
            dataTable.Columns.Add("UnitPrice", typeof(decimal));
            dataTable.Columns.Add("Subtotal", typeof(decimal));
            
            dataTable.Rows.Add("200001", "Intel Core i9-12900K Processor", 5, 500.00m, 2500.00m);
            dataTable.Rows.Add("200002", "ASUS ROG Strix Z690-E Gaming WiFi 6E LGA 1700 ATX Motherboard", 5, 350.00m, 1750.00m);
            dataTable.Rows.Add("200003", "Corsair Vengeance LPX 32GB (2 x 16GB) DDR4 3200MHz C16 Desktop Memory", 10, 150.00m, 1500.00m);

            // Create minimal RDLC structure for testing
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
            <!-- Teal Header Bar -->
            <Rectangle Name='TealHeaderBar'>
                <Top>1.2in</Top>
                <Left>0in</Left>
                <Width>8in</Width>
                <Height>0.3in</Height>
                <Style>
                    <BackgroundColor>#4A9B8E</BackgroundColor>
                </Style>
            </Rectangle>
            
            <!-- INVOICE Title -->
            <Textbox Name='InvoiceTitle'>
                <Top>1.25in</Top>
                <Left>6in</Left>
                <Width>1.5in</Width>
                <Height>0.2in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>INVOICE</Value>
                                <Style>
                                    <FontSize>20pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                    <Color>White</Color>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Company Name -->
            <Textbox Name='CompanyName'>
                <Top>0.5in</Top>
                <Left>1in</Left>
                <Width>3in</Width>
                <Height>0.4in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>Tech Solutions Inc.</Value>
                                <Style>
                                    <FontSize>16pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Invoice Items Table -->
            <Tablix Name='InvoiceItemsTable'>
                <Top>2.5in</Top>
                <Left>0.2in</Left>
                <Width>7.6in</Width>
                <Height>2in</Height>
                <DataSetName>InvoiceItems</DataSetName>
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
                DataSources = new Dictionary<string, DataTable> { { "InvoiceItems", dataTable } },
                Parameters = new Dictionary<string, object>()
            };

            Console.WriteLine("Creating PdfRenderer...");
            var pdfRenderer = new PdfRenderer();
            
            Console.WriteLine("Rendering PDF...");
            var pdfBytes = pdfRenderer.Render(context);

            // Save to output
            var outputPath = @"C:\Output\ElegantInvoice_Generated_iText7.pdf";
            Directory.CreateDirectory(@"C:\Output");
            File.WriteAllBytes(outputPath, pdfBytes);

            Console.WriteLine($"✅ Elegant Invoice PDF generated successfully!");
            Console.WriteLine($"📄 Generated PDF: {outputPath}");
            Console.WriteLine($"📏 PDF size: {pdfBytes.Length} bytes");
            Console.WriteLine($"🔍 Sample PDF: C:\\code\\RDL2PDF\\report samples\\Elegant_Invoice\\Elegant Invoice.pdf");
            Console.WriteLine();
            Console.WriteLine("Now you can compare the generated PDF with the sample to see the differences.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
