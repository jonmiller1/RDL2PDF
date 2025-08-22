using FluentRDLC;
using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Xml.Linq;
using QuestPDF.Infrastructure;

namespace FluetRDLC.Test;

[TestClass]
public class FancyInvoiceTest
{
    [TestMethod]
    public void GenerateFancyInvoice_CreatesProfessionalInvoicePDF()
    {
        // Set QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
        
        // Create sample invoice data
        var invoiceItems = CreateInvoiceItemsDataTable();
        
        // Build the fancy invoice report to match the exact design
        var reportBuilder = RdlcReportBuilder.Create("FancyInvoice")
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
            .WithEmbeddedImageFromBytes("CompanyLogo", File.ReadAllBytes(@"C:\code\RDL2PDF\report samples\Fancy_Invoice\logo.png"), "image/png")
            .WithImage("LogoImage", img => img
                .FromEmbedded("CompanyLogo")
                .WithBounds(0.2, 0.2, 0.8, 0.8))
            
            // Company name next to logo
            .WithTextBox("CompanyName", tb => tb
                .WithText("Tech Solutions Inc.")
                .WithBounds(1.2, 0.3, 3, 0.5)
                .WithFontSize(16)
                .Bold())
            
            // Large INVOICE title (left side)
            .WithTextBox("InvoiceTitle", tb => tb
                .WithText("INVOICE")
                .WithBounds(0.2, 1.5, 3, 1)
                .WithFontSize(48)
                .Bold())
            
            // Invoice metadata (top right)
            .WithTextBox("InvoiceNumber", tb => tb
                .WithText("Invoice No.:    INV-2024-00789")
                .WithBounds(4.5, 1.0, 3.5, 0.3)
                .WithFontSize(11))
            
            .WithTextBox("InvoiceDate", tb => tb
                .WithText("Date:    2024-07-11")
                .WithBounds(4.5, 1.3, 3.5, 0.3)
                .WithFontSize(11))
                
            .WithTextBox("DueDate", tb => tb
                .WithText("Due date:    2024-08-11")
                .WithBounds(4.5, 1.6, 3.5, 0.3)
                .WithFontSize(11))
            
            // INVOICE TO section with gray background
            .WithRectangle("InvoiceToBackground", rect => rect
                .WithBounds(4.3, 2.2, 3.9, 2.2)
                .LightGray()
                .NoBorder())
                
            .WithTextBox("InvoiceToLabel", tb => tb
                .WithText("INVOICE TO:")
                .WithBounds(4.5, 2.4, 2, 0.3)
                .WithFontSize(12)
                .Bold())
                
            .WithTextBox("CompanyNameTo", tb => tb
                .WithText("Tech Solutions Inc.")
                .WithBounds(4.5, 2.8, 3, 0.3)
                .WithFontSize(12)
                .Bold())
                
            .WithTextBox("CompanyAddress", tb => tb
                .WithText("456 Innovation Drive")
                .WithBounds(4.5, 3.1, 3, 0.3)
                .WithFontSize(11))
                
            .WithTextBox("CompanyLocation", tb => tb
                .WithText("94016 San Francisco")
                .WithBounds(4.5, 3.4, 3, 0.3)
                .WithFontSize(11))
                
            .WithTextBox("CompanyState", tb => tb
                .WithText("California")
                .WithBounds(4.5, 3.7, 3, 0.3)
                .WithFontSize(11))
            
            // Table header background (yellow)
            .WithRectangle("TableHeaderBackground", rect => rect
                .WithBounds(0.2, 5.0, 7.6, 0.5)
                .Yellow()
                .NoBorder())
                
            // Invoice items table
            .WithTablix("InvoiceItemsTable", tbl => tbl
                .UsingDataSet("InvoiceItems")
                .WithBounds(0.2, 5.0, 7.6, 3)
                .WithColumn(0.8) // SKU
                .WithColumn(3.8) // Description  
                .WithColumn(0.8) // Quantity
                .WithColumn(1.2) // Unit Price
                .WithColumn(1.0) // Subtotal
                
                // Header row
                .WithHeaderRow(0.5, "SKU", "DESCRIPTION", "QUANTITY", "UNIT PRICE", "SUBTOTAL")
                
                // Data rows
                .WithDataRow(0.4, 
                    "=Fields!SKU.Value",
                    "=Fields!Description.Value", 
                    "=Fields!Quantity.Value",
                    "=Format(Fields!UnitPrice.Value, 'C2')",
                    "=Format(Fields!Subtotal.Value, 'C2')"))
            
            // Summary section background (gray)
            .WithRectangle("SummaryBackground", rect => rect
                .WithBounds(5.0, 8.5, 2.8, 1.8)
                .LightGray()
                .NoBorder())
                
            // Summary items
            .WithTextBox("SubtotalLabel", tb => tb
                .WithText("Subtotal:")
                .WithBounds(6.5, 8.7, 1.2, 0.25)
                .WithFontSize(11))
            .WithTextBox("SubtotalAmount", tb => tb
                .WithText("€11,430.00")
                .WithBounds(6.5, 8.7, 1.2, 0.25)
                .WithFontSize(11))
                
            .WithTextBox("TaxLabel", tb => tb
                .WithText("Tax(7.00%):")
                .WithBounds(6.5, 9.0, 1.2, 0.25)
                .WithFontSize(11))
            .WithTextBox("TaxAmount", tb => tb
                .WithText("€800.10")
                .WithBounds(6.5, 9.0, 1.2, 0.25)
                .WithFontSize(11))
                
            .WithTextBox("ShippingLabel", tb => tb
                .WithText("Shipping Fee:")
                .WithBounds(6.5, 9.3, 1.2, 0.25)
                .WithFontSize(11))
            .WithTextBox("ShippingAmount", tb => tb
                .WithText("€25.00")
                .WithBounds(6.5, 9.3, 1.2, 0.25)
                .WithFontSize(11))
            
            // Total section with yellow background
            .WithRectangle("TotalBackground", rect => rect
                .WithBounds(5.0, 9.7, 2.8, 0.6)
                .Yellow()
                .NoBorder())
                
            .WithTextBox("TotalLabel", tb => tb
                .WithText("Total:")
                .WithBounds(6.5, 9.8, 1.2, 0.4)
                .WithFontSize(14)
                .Bold())
            .WithTextBox("TotalAmount", tb => tb
                .WithText("€12,255.10")
                .WithBounds(6.5, 9.8, 1.2, 0.4)
                .WithFontSize(14)
                .Bold())
            
            // Terms and conditions (bottom left)
            .WithTextBox("TermsTitle", tb => tb
                .WithText("TERMS AND CONDITIONS")
                .WithBounds(0.2, 8.5, 4, 0.3)
                .WithFontSize(11)
                .Bold())
                
            .WithTextBox("TermsContent", tb => tb
                .WithText("Lorem ipsum dolor sit amet consectetur. Nisi" + Environment.NewLine +
                         "pharetra pellentesque feugiat diam a phasellus" + Environment.NewLine +
                         "etiam eget convallis. Risus placerat turpis massa" + Environment.NewLine +
                         "nulla feugiat adipiscing ut fermentum scelerisque.")
                .WithBounds(0.2, 8.8, 4, 1.2)
                .WithFontSize(9))
            
            // Bottom footer
            .WithTextBox("ThankYou", tb => tb
                .WithText("Thank you for your business!")
                .WithBounds(0.2, 10.2, 3, 0.3)
                .WithFontSize(12)
                .Bold())
                
            // Company logo and name (bottom right)  
            .WithImage("FooterLogoImage", img => img
                .FromEmbedded("CompanyLogo")
                .WithBounds(6.2, 10.2, 0.3, 0.3))
            .WithTextBox("CompanyFooterName", tb => tb
                .WithText("Tech Solutions Inc.")
                .WithBounds(6.6, 10.2, 1.4, 0.3)
                .WithFontSize(12)
                .Bold())
            
            // Contact information (very bottom)
            .WithTextBox("ContactPhone", tb => tb
                .WithText("📞 +1 555-789-0123")
                .WithBounds(0.2, 10.6, 2.5, 0.25)
                .WithFontSize(9))
                
            .WithTextBox("ContactEmail", tb => tb
                .WithText("✉ info@techsolutions.com")
                .WithBounds(3.0, 10.6, 2.5, 0.25)
                .WithFontSize(9))
                
            .WithTextBox("ContactLocation", tb => tb
                .WithText("📍 San Francisco")
                .WithBounds(6.0, 10.6, 1.8, 0.25)
                .WithFontSize(9));

        // Generate the RDLC XML
        var rdlcXml = reportBuilder.ToXml();
        
        // Save the RDLC XML for debugging
        File.WriteAllText(@"C:\Output\FancyInvoice_Generated.rdlc", rdlcXml);
        
        // Create renderer and generate PDF  
        var renderer = new RDLCRenderer();
        
        // Add the data source to the renderer
        renderer.AddDataSource("InvoiceItems", invoiceItems);
        
        var pdfBytes = renderer.RenderFromContent(rdlcXml, RenderFormat.PDF);
        
        // Save the PDF
        var outputPath = @"C:\Output\FancyInvoice_Generated.pdf";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, pdfBytes);
        
        // Assertions
        Assert.IsNotNull(pdfBytes);
        Assert.IsTrue(pdfBytes.Length > 0);
        Assert.IsTrue(File.Exists(outputPath));
        
        Console.WriteLine($"Fancy Invoice PDF generated: {outputPath}");
        Console.WriteLine($"PDF size: {pdfBytes.Length / 1024.0:F1} KB");
    }
    
    private DataTable CreateInvoiceItemsDataTable()
    {
        var dataTable = new DataTable("InvoiceItems");
        dataTable.Columns.Add("SKU", typeof(string));
        dataTable.Columns.Add("Description", typeof(string));
        dataTable.Columns.Add("Quantity", typeof(int));
        dataTable.Columns.Add("UnitPrice", typeof(decimal));
        dataTable.Columns.Add("Subtotal", typeof(decimal));
        
        // Add sample invoice items matching the PDF
        dataTable.Rows.Add("200001", "Intel Core i9-12900K Processor", 5, 500.00m, 2500.00m);
        dataTable.Rows.Add("200002", "ASUS ROG Strix Z690-E Gaming WiFi 6E LGA 1700 ATX Motherboard", 5, 350.00m, 1750.00m);
        dataTable.Rows.Add("200003", "Corsair Vengeance LPX 32GB (2 x 16GB) DDR4 3200MHz C16 Desktop Memory", 10, 150.00m, 1500.00m);
        dataTable.Rows.Add("200004", "Samsung 970 EVO Plus SSD 2TB - NVMe M.2", 6, 230.00m, 1380.00m);
        dataTable.Rows.Add("200005", "EVGA GeForce RTX 3080 Ti FTW3 Ultra Gaming, 12GB GDDR6X", 3, 1200.00m, 3600.00m);
        dataTable.Rows.Add("200006", "Corsair RM850x, 850 Watt, 80+ Gold Certified, Fully Modular Power Supply", 5, 140.00m, 700.00m);
        
        return dataTable;
    }
}