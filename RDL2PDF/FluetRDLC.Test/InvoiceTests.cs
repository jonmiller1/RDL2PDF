using FluentRDLC;
using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Data;

namespace FluetRDLC.Test
{
    [TestClass]
    public class InvoiceTests
    {
        [TestMethod]
        public void CreateInvoiceWithCompanyLogo()
        {
            // Create invoice test data
            var renderer = new RDLCRenderer();
            var invoiceData = TestDataFactory.CreateInvoiceData();
            renderer.AddDataSource("InvoiceItems", CreateInvoiceDataTable(invoiceData));
            
            // Add invoice parameters
            renderer.AddParameter("InvoiceNumber", "INV-2024-001");
            renderer.AddParameter("InvoiceDate", DateTime.Now.ToString("MM/dd/yyyy"));
            renderer.AddParameter("DueDate", DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"));
            renderer.AddParameter("CompanyName", "TechSolutions LLC");
            renderer.AddParameter("CompanyAddress", "123 Business Ave");
            renderer.AddParameter("CompanyCityState", "Seattle, WA 98101");
            renderer.AddParameter("CompanyPhone", "(555) 123-4567");
            renderer.AddParameter("CompanyEmail", "info@techsolutions.com");
            renderer.AddParameter("CustomerName", "ABC Corporation");
            renderer.AddParameter("CustomerAddress", "456 Client Street");
            renderer.AddParameter("CustomerCityState", "Portland, OR 97201");
            
            // Create RDLC content for invoice with company logo
            var rdlcContent = CreateInvoiceRdlcContent();
            
            Console.WriteLine("Generating Invoice PDF with Company Logo...");
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            var outputPath = GetOutputPath("Invoice_CompanyLogo.pdf");
            File.WriteAllBytes(outputPath, pdfBytes);
            Console.WriteLine($"Invoice saved to: {outputPath}");
            Console.WriteLine($"File size: {pdfBytes.Length} bytes");
            
            // Verify the PDF was generated with substantial content
            Assert.IsTrue(pdfBytes.Length > 5000, "Invoice PDF should be generated with substantial content including logo and data");
            
            // Test multi-format generation
            Console.WriteLine("Generating Invoice in multiple formats...");
            
            // HTML version
            var htmlBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.HTML);
            File.WriteAllBytes(GetOutputPath("Invoice_CompanyLogo.html"), htmlBytes);
            
            // CSV version (for line items)
            var csvBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.Excel);
            File.WriteAllBytes(GetOutputPath("Invoice_CompanyLogo.csv"), csvBytes);
            
            Console.WriteLine("Invoice generated in PDF, HTML, and CSV formats");
        }
        
        private DataTable CreateInvoiceDataTable(System.Collections.Generic.List<InvoiceItem> invoiceItems)
        {
            var table = new DataTable();
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("LineTotal", typeof(decimal));
            table.Columns.Add("TaxAmount", typeof(decimal));
            table.Columns.Add("TotalWithTax", typeof(decimal));
            
            foreach (var item in invoiceItems)
            {
                table.Rows.Add(
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    item.TaxAmount,
                    item.TotalWithTax
                );
            }
            
            return table;
        }
        
        private string CreateInvoiceRdlcContent()
        {
            // Create the report using fully fluent builder - no manual XML needed!
            var reportBuilder = RdlcReportBuilder.Create("InvoiceReport")
                // Set up page layout
                .WithLayout(layout => layout
                    .WithPageSize(8.5, 11)
                    .WithMargins(0.5)
                    .WithBodyHeight(11))
                
                // Add embedded company logo
                .WithEmbeddedImageFromFile("CompanyLogo", @"C:\Output\company_logo.png")
                
                // Add object data source for invoice items
                .WithObjectDataSource<InvoiceItem>("InvoiceDataSource")
                
                // Add dataset with invoice item fields
                .WithDataSet("InvoiceItems", ds => ds
                    .UsingDataSource("InvoiceDataSource")
                    .WithFieldsFromType<InvoiceItem>())
                
                // Company logo image control
                .WithLogo("CompanyLogo", 0.2, 0.1)
                
                // Company information section (top right)
                .WithTextBox("CompanyInfo", tb => tb
                    .WithExpression("=Parameters!CompanyName.Value + vbCrLf + " +
                                   "Parameters!CompanyAddress.Value + vbCrLf + " +
                                   "Parameters!CompanyCityState.Value + vbCrLf + " +
                                   "'Phone: ' + Parameters!CompanyPhone.Value + vbCrLf + " +
                                   "'Email: ' + Parameters!CompanyEmail.Value")
                    .WithBounds(3, 0.2, 4.5, 1.5)
                    .WithFontSize(11)
                    .Bold())
                
                // Invoice title (centered)
                .WithTextBox("InvoiceTitle", tb => tb
                    .WithText("INVOICE")
                    .WithBounds(0, 2.2, 8, 0.5)
                    .WithFontSize(24)
                    .Bold())
                
                // Invoice details section (left)
                .WithTextBox("InvoiceDetails", tb => tb
                    .WithExpression("='Invoice #: ' + Parameters!InvoiceNumber.Value + vbCrLf + " +
                                   "'Invoice Date: ' + Parameters!InvoiceDate.Value + vbCrLf + " +
                                   "'Due Date: ' + Parameters!DueDate.Value")
                    .WithBounds(0, 2.8, 4, 1.2)
                    .WithFontSize(11))
                
                // Customer details section (right)
                .WithTextBox("CustomerDetails", tb => tb
                    .WithExpression("='Bill To:' + vbCrLf + " +
                                   "Parameters!CustomerName.Value + vbCrLf + " +
                                   "Parameters!CustomerAddress.Value + vbCrLf + " +
                                   "Parameters!CustomerCityState.Value")
                    .WithBounds(4.5, 2.8, 3.5, 1.2)
                    .WithFontSize(11))
                
                // Invoice items table using fluent tablix builder
                .WithInvoiceTable("InvoiceItems", 0, 4.2, 8, 1.5)
                
                // Invoice totals section (bottom right)
                .WithTextBox("InvoiceTotal", tb => tb
                    .WithExpression("='Subtotal: $' + Format(Sum(Fields!LineTotal.Value, 'InvoiceItems'), 'N2') + vbCrLf + " +
                                   "'Tax: $' + Format(Sum(Fields!TaxAmount.Value, 'InvoiceItems'), 'N2') + vbCrLf + " +
                                   "'TOTAL: $' + Format(Sum(Fields!TotalWithTax.Value, 'InvoiceItems'), 'N2')")
                    .WithBounds(5.5, 6.0, 2.5, 0.8)
                    .WithFontSize(12)
                    .Bold())
                
                // Payment terms and footer
                .WithTextBox("PaymentTerms", tb => tb
                    .WithExpression("='Payment Terms: Net 30 days' + vbCrLf + vbCrLf + " +
                                   "'Thank you for your business!' + vbCrLf + " +
                                   "'For questions about this invoice, please contact us at ' + Parameters!CompanyPhone.Value")
                    .WithBounds(0, 7.0, 8, 1.0)
                    .WithFontSize(10));
                                   
            return reportBuilder.ToXml();
        }
        
        private static string GetOutputPath(string fileName)
        {
            var outputDir = @"C:\Output";
            Directory.CreateDirectory(outputDir);
            return Path.Combine(outputDir, fileName);
        }
    }
}