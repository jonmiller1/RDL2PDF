using FluentRDLC.Renderer.Renderers;
using System.Data;
using System.Xml.Linq;
using FluentRDLC.Renderer;

class Program
{
    static void Main()
    {
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
        dataTable.Rows.Add("200004", "Samsung 970 EVO Plus SSD 2TB - NVMe M.2", 6, 230.00m, 1380.00m);
        dataTable.Rows.Add("200005", "EVGA GeForce RTX 3080 Ti FTW3 Ultra Gaming, 12GB GDDR6X", 3, 1200.00m, 3600.00m);
        dataTable.Rows.Add("200006", "Corsair RM850x, 850 Watt, 80+ Gold Certified, Fully Modular Power Supply", 5, 140.00m, 700.00m);

        // Create RDLC structure for Elegant Invoice with teal colors
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
            <!-- Company Name -->
            <Textbox Name='CompanyName'>
                <Top>0.3in</Top>
                <Left>1.2in</Left>
                <Width>3in</Width>
                <Height>0.5in</Height>
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
                <Left>5.5in</Left>
                <Width>2.3in</Width>
                <Height>0.3in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>INVOICE</Value>
                                <Style>
                                    <FontSize>24pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                    <Color>White</Color>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Invoice To Label -->
            <Textbox Name='InvoiceToLabel'>
                <Top>1.8in</Top>
                <Left>0.2in</Left>
                <Width>2in</Width>
                <Height>0.25in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>INVOICE TO:</Value>
                                <Style>
                                    <FontSize>12pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Invoice To Address -->
            <Textbox Name='InvoiceToAddress'>
                <Top>2.1in</Top>
                <Left>0.2in</Left>
                <Width>3in</Width>
                <Height>1in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>Tech Solutions Inc.
456 Innovation Drive
94016 San Francisco
California</Value>
                                <Style>
                                    <FontSize>10pt</FontSize>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Invoice Details -->
            <Textbox Name='InvoiceDetails'>
                <Top>1.8in</Top>
                <Left>5.5in</Left>
                <Width>2.5in</Width>
                <Height>0.8in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>Invoice No.:    INV-2024-00789
Date:    2024-07-11
Due date:    2024-08-11</Value>
                                <Style>
                                    <FontSize>10pt</FontSize>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Invoice Items Table -->
            <Tablix Name='InvoiceItemsTable'>
                <Top>3.5in</Top>
                <Left>0.2in</Left>
                <Width>7.6in</Width>
                <Height>3in</Height>
                <DataSetName>InvoiceItems</DataSetName>
            </Tablix>
            
            <!-- Teal Total Background -->
            <Rectangle Name='TotalBackground'>
                <Top>7.8in</Top>
                <Left>6in</Left>
                <Width>2in</Width>
                <Height>0.4in</Height>
                <Style>
                    <BackgroundColor>#4A9B8E</BackgroundColor>
                </Style>
            </Rectangle>
            
            <!-- Total Label -->
            <Textbox Name='TotalLabel'>
                <Top>7.85in</Top>
                <Left>6.1in</Left>
                <Width>1in</Width>
                <Height>0.3in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>Total:</Value>
                                <Style>
                                    <FontSize>12pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                    <Color>White</Color>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Total Amount -->
            <Textbox Name='TotalAmount'>
                <Top>7.85in</Top>
                <Left>7.1in</Left>
                <Width>0.8in</Width>
                <Height>0.3in</Height>
                <Paragraphs>
                    <Paragraph>
                        <TextRuns>
                            <TextRun>
                                <Value>€12,255.10</Value>
                                <Style>
                                    <FontSize>12pt</FontSize>
                                    <FontWeight>Bold</FontWeight>
                                    <Color>White</Color>
                                </Style>
                            </TextRun>
                        </TextRuns>
                    </Paragraph>
                </Paragraphs>
            </Textbox>
            
            <!-- Footer Teal Bar -->
            <Rectangle Name='FooterBar'>
                <Top>10.8in</Top>
                <Left>0in</Left>
                <Width>8in</Width>
                <Height>0.1in</Height>
                <Style>
                    <BackgroundColor>#4A9B8E</BackgroundColor>
                </Style>
            </Rectangle>
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

        // Test the PDF renderer
        var pdfRenderer = new PdfRenderer();
        var pdfBytes = pdfRenderer.Render(context);

        // Save to output
        var outputPath = @"C:\Output\ElegantInvoice_Generated_iText7.pdf";
        Directory.CreateDirectory(@"C:\Output");
        File.WriteAllBytes(outputPath, pdfBytes);

        Console.WriteLine($"Elegant Invoice PDF generated successfully: {outputPath}");
        Console.WriteLine($"PDF size: {pdfBytes.Length} bytes");
        Console.WriteLine("You can now compare this with the sample at: C:\\code\\RDL2PDF\\report samples\\Elegant_Invoice\\Elegant Invoice.pdf");
    }
}