using FluentRDLC;
using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Xml.Linq;
using QuestPDF.Infrastructure;

namespace FluetRDLC.Test;

[TestClass]
public class ElegantInvoiceTest
{
    [TestMethod]
    public void GenerateElegantInvoice_CreatesProfessionalInvoicePDF()
    {
        // Set QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
        
        // Create sample invoice data
        var invoiceItems = CreateInvoiceItemsDataTable();
        
        // Build the elegant invoice report with teal accent colors
        var reportBuilder = RdlcReportBuilder.Create("ElegantInvoice")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5, 0.5, 0.5, 0.5)
                .WithBodyHeight(10))
            
            // Data sources
            .WithObjectDataSource<InvoiceItem>("InvoiceItemsDataSource")
            .WithDataSet("InvoiceItems", ds => ds
                .UsingDataSource("InvoiceItemsDataSource")
                .WithQuery("/* Invoice Items Query */")
                .WithField("SKU", "SKU")
                .WithField("Description", "Description")
                .WithField("Quantity", "Quantity")
                .WithField("UnitPrice", "UnitPrice")
                .WithField("Subtotal", "Subtotal"))
            
            // Company logo (top left corner)
            .WithEmbeddedImageFromBytes("CompanyLogo", File.ReadAllBytes(@"C:\code\RDL2PDF\report samples\Elegant_Invoice\logo.png"), "image/png")
            .WithImage("LogoImage", img => img
                .FromEmbedded("CompanyLogo")
                .WithBounds(0.2, 0.2, 0.8, 0.8))
            
            // Company name next to logo
            .WithTextBox("CompanyName", tb => tb
                .WithText("Tech Solutions Inc.")
                .WithBounds(1.2, 0.3, 3, 0.5)
                .WithFontSize(16)
                .Bold())
            
            // Teal header bar
            .WithRectangle("TealHeaderBar", rect => rect
                .WithBounds(0, 1.2, 8, 0.3)
                .WithBackgroundColor("#4A9B8E"))
            
            // INVOICE title on the right
            .WithTextBox("InvoiceTitle", tb => tb
                .WithText("INVOICE")
                .WithBounds(5.5, 1.25, 2.3, 0.3)
                .WithFontSize(24)
                .Bold()
                .WithColor("White")
                .WithTextAlign("Center"))
            
            // Invoice details section
            .WithTextBox("InvoiceDetails", tb => tb
                .WithExpression("='Invoice No.:    INV-2024-00789' + vbCrLf + 'Date:    2024-07-11' + vbCrLf + 'Due date:    2024-08-11'")
                .WithBounds(5.5, 1.8, 2.5, 0.8)
                .WithFontSize(10))
            
            // Invoice To section
            .WithTextBox("InvoiceToLabel", tb => tb
                .WithText("INVOICE TO:")
                .WithBounds(0.2, 1.8, 2, 0.25)
                .WithFontSize(12)
                .Bold())
            
            .WithTextBox("InvoiceToAddress", tb => tb
                .WithExpression("='Tech Solutions Inc.' + vbCrLf + '456 Innovation Drive' + vbCrLf + '94016 San Francisco' + vbCrLf + 'California'")
                .WithBounds(0.2, 2.1, 3, 1)
                .WithFontSize(10))
            
            // Table for invoice items with gray header
            .WithTablix("InvoiceItemsTable", tbl => tbl
                .UsingDataSet("InvoiceItems")
                .WithBounds(0.2, 3.5, 7.6, 3)
                .WithColumn(0.8)  // SKU
                .WithColumn(3.2)  // Description  
                .WithColumn(0.8)  // Quantity
                .WithColumn(1.0)  // Unit Price
                .WithColumn(1.0)  // Subtotal
                .WithHeaderRow(0.3, row => row
                    .WithCell("SKU").WithBackgroundColor("#E8E8E8").Bold()
                    .WithCell("DESCRIPTION").WithBackgroundColor("#E8E8E8").Bold()
                    .WithCell("QUANTITY").WithBackgroundColor("#E8E8E8").Bold().WithTextAlign("Center")
                    .WithCell("UNIT PRICE").WithBackgroundColor("#E8E8E8").Bold().WithTextAlign("Right")
                    .WithCell("SUBTOTAL").WithBackgroundColor("#E8E8E8").Bold().WithTextAlign("Right"))
                .WithDataRow(0.4, row => row
                    .WithCell("=Fields!SKU.Value")
                    .WithCell("=Fields!Description.Value")
                    .WithCell("=Fields!Quantity.Value").WithTextAlign("Center")
                    .WithCell("=Format(Fields!UnitPrice.Value, 'C2')").WithTextAlign("Right")
                    .WithCell("=Format(Fields!Subtotal.Value, 'C2')").WithTextAlign("Right")))
            
            // Summary section (right aligned)
            .WithTextBox("Subtotal", tb => tb
                .WithText("Subtotal:")
                .WithBounds(6, 6.8, 1, 0.25)
                .WithFontSize(11)
                .WithTextAlign("Right"))
            
            .WithTextBox("SubtotalAmount", tb => tb
                .WithText("€11,430.00")
                .WithBounds(7.2, 6.8, 0.8, 0.25)
                .WithFontSize(11)
                .WithTextAlign("Right"))
            
            .WithTextBox("Tax", tb => tb
                .WithText("Tax(7.00%):")
                .WithBounds(6, 7.1, 1, 0.25)
                .WithFontSize(11)
                .WithTextAlign("Right"))
            
            .WithTextBox("TaxAmount", tb => tb
                .WithText("€800.10")
                .WithBounds(7.2, 7.1, 0.8, 0.25)
                .WithFontSize(11)
                .WithTextAlign("Right"))
            
            .WithTextBox("Shipping", tb => tb
                .WithText("Shipping Fee:")
                .WithBounds(6, 7.4, 1, 0.25)
                .WithFontSize(11)
                .WithTextAlign("Right"))
            
            .WithTextBox("ShippingAmount", tb => tb
                .WithText("€25.00")
                .WithBounds(7.2, 7.4, 0.8, 0.25)
                .WithFontSize(11)
                .WithTextAlign("Right"))
            
            // Teal total section
            .WithRectangle("TotalBackground", rect => rect
                .WithBounds(6, 7.8, 2, 0.4)
                .WithBackgroundColor("#4A9B8E"))
            
            .WithTextBox("TotalLabel", tb => tb
                .WithText("Total:")
                .WithBounds(6.1, 7.85, 1, 0.3)
                .WithFontSize(12)
                .Bold()
                .WithColor("White"))
            
            .WithTextBox("TotalAmount", tb => tb
                .WithText("€12,255.10")
                .WithBounds(7.1, 7.85, 0.8, 0.3)
                .WithFontSize(12)
                .Bold()
                .WithColor("White")
                .WithTextAlign("Right"))
            
            // Notes section with border
            .WithRectangle("NotesBox", rect => rect
                .WithBounds(0.2, 8.5, 4, 1.2)
                .WithBorderColor("#CCCCCC")
                .WithBorderWidth(1))
            
            .WithTextBox("NotesLabel", tb => tb
                .WithText("NOTES")
                .WithBounds(0.4, 8.6, 1, 0.25)
                .WithFontSize(11)
                .Bold())
            
            .WithTextBox("NotesContent", tb => tb
                .WithText("Thank you for your business!")
                .WithBounds(0.4, 8.9, 3.5, 0.7)
                .WithFontSize(10))
            
            // Terms and Conditions
            .WithTextBox("TermsLabel", tb => tb
                .WithText("TERMS AND CONDITIONS")
                .WithBounds(0.2, 9.9, 7.6, 0.25)
                .WithFontSize(11)
                .Bold())
            
            .WithTextBox("TermsContent", tb => tb
                .WithText("Lorem ipsum dolor sit amet consectetur. Nisi pharetra pellentesque feugiat diam a phasellus etiam eget convallis. Risus placerat turpis massa nulla feugiat adipiscing ut fermentum scelerisque.")
                .WithBounds(0.2, 10.2, 7.6, 0.4)
                .WithFontSize(9))
            
            // Footer teal bar
            .WithRectangle("FooterBar", rect => rect
                .WithBounds(0, 10.8, 8, 0.1)
                .WithBackgroundColor("#4A9B8E"))
            
            // Footer contact info
            .WithTextBox("FooterContact", tb => tb
                .WithText("📞 +1 555-789-0123        ✉ info@techsolutions.com        📍 San Francisco")
                .WithBounds(0.2, 10.9, 7.6, 0.3)
                .WithFontSize(10)
                .WithTextAlign("Center"));

        // Generate RDLC XML
        var rdlcXml = reportBuilder.ToXml();
        
        // Save the generated RDLC to output folder for inspection
        var rdlcOutputPath = @"C:\Output\ElegantInvoice_Generated.rdlc";
        Directory.CreateDirectory(@"C:\Output");
        File.WriteAllText(rdlcOutputPath, rdlcXml);
        
        // Validate the XML structure
        var doc = XDocument.Parse(rdlcXml);
        Assert.IsNotNull(doc.Root);
        
        // Create renderer and generate PDF
        var renderer = RendererFactory.CreateRenderer(RenderFormat.PDF) as PdfRenderer;
        Assert.IsNotNull(renderer);
        
        // Add data source
        renderer.AddDataSource("InvoiceItems", invoiceItems);
        
        // Render to PDF
        var pdfBytes = renderer.RenderFromContent(rdlcXml);
        Assert.IsTrue(pdfBytes.Length > 0);
        
        // Save PDF to output folder
        var pdfOutputPath = @"C:\Output\ElegantInvoice_Generated.pdf";
        File.WriteAllBytes(pdfOutputPath, pdfBytes);
        
        Console.WriteLine($"Elegant Invoice generated successfully!");
        Console.WriteLine($"RDLC saved to: {rdlcOutputPath}");
        Console.WriteLine($"PDF saved to: {pdfOutputPath}");
        Console.WriteLine($"PDF file size: {pdfBytes.Length / 1024.0:F1} KB");
        
        // Verify minimum file size (should be substantial for a professional invoice)
        Assert.IsTrue(pdfBytes.Length > 50000, "Generated PDF should be substantial in size");
    }
    
    private DataTable CreateInvoiceItemsDataTable()
    {
        var table = new DataTable("InvoiceItems");
        
        // Add columns
        table.Columns.Add("SKU", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("UnitPrice", typeof(decimal));
        table.Columns.Add("Subtotal", typeof(decimal));
        
        // Add sample data (same as Fancy Invoice)
        table.Rows.Add("200001", "Intel Core i9-12900K Processor", 5, 500.00m, 2500.00m);
        table.Rows.Add("200002", "ASUS ROG Strix Z690-E Gaming WiFi 6E LGA 1700 ATX Motherboard", 5, 350.00m, 1750.00m);
        table.Rows.Add("200003", "Corsair Vengeance LPX 32GB (2 x 16GB) DDR4 3200MHz C16 Desktop Memory", 10, 150.00m, 1500.00m);
        table.Rows.Add("200004", "Samsung 970 EVO Plus SSD 2TB - NVMe M.2", 6, 230.00m, 1380.00m);
        table.Rows.Add("200005", "EVGA GeForce RTX 3080 Ti FTW3 Ultra Gaming, 12GB GDDR6X", 3, 1200.00m, 3600.00m);
        table.Rows.Add("200006", "Corsair RM850x, 850 Watt, 80+ Gold Certified, Fully Modular Power Supply", 5, 140.00m, 700.00m);
        
        return table;
    }
    
    public class InvoiceItem
    {
        public string SKU { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}