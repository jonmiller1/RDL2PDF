using FluentRDLC;
using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace FluetRDLC.Test;

[TestClass]
public class RdlcConverterIntegrationTests
{
    private RdlcToFluentConverter _converter;
    private RDLCRenderer _renderer;

    [TestInitialize]
    public void Setup()
    {
        _converter = new RdlcToFluentConverter();
        _renderer = new RDLCRenderer();
    }

    [TestMethod]
    public void ConvertAndRender_SimpleInvoice_GeneratesWorkingCode()
    {
        // Create a simple invoice RDLC
        var originalBuilder = RdlcReportBuilder.Create("SimpleInvoice")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5)
                .WithBodyHeight(11))
            .WithObjectDataSource<InvoiceItem>("InvoiceDataSource")
            .WithDataSet("InvoiceItems", ds => ds
                .UsingDataSource("InvoiceDataSource")
                .WithFieldsFromType<InvoiceItem>())
            .WithTextBox("InvoiceTitle", tb => tb
                .WithText("INVOICE")
                .WithBounds(0, 0, 8, 0.5)
                .WithFontSize(18)
                .Bold())
            .WithTextBox("CompanyInfo", tb => tb
                .WithExpression("=Parameters!CompanyName.Value + vbCrLf + Parameters!CompanyAddress.Value")
                .WithBounds(0, 1, 4, 1)
                .WithFontSize(11));

        var originalXml = originalBuilder.ToXml();

        // Convert the RDLC back to fluent code
        var fluentCode = _converter.ConvertToFluentCode(originalXml, "ConvertedInvoice");

        // Verify the generated code contains expected elements
        Assert.IsTrue(fluentCode.Contains("RdlcReportBuilder.Create(\"ConvertedInvoice\")"));
        Assert.IsTrue(fluentCode.Contains(".WithLayout(layout => layout"));
        Assert.IsTrue(fluentCode.Contains(".WithPageSize(8.5, 11)"));
        Assert.IsTrue(fluentCode.Contains(".WithTextBox(\"InvoiceTitle\", tb => tb"));
        Assert.IsTrue(fluentCode.Contains(".WithText(\"INVOICE\")"));
        Assert.IsTrue(fluentCode.Contains(".WithFontSize(18)"));
        Assert.IsTrue(fluentCode.Contains(".Bold()"));
        Assert.IsTrue(fluentCode.Contains(".WithTextBox(\"CompanyInfo\", tb => tb"));
        Assert.IsTrue(fluentCode.Contains(".WithExpression(\"=Parameters!CompanyName.Value + vbCrLf + Parameters!CompanyAddress.Value\")"));

        Console.WriteLine("Generated Fluent Code:");
        Console.WriteLine(fluentCode);
    }

    [TestMethod]
    public void ConvertAndRender_InvoiceWithTable_GeneratesWorkingCode()
    {
        // Create an invoice with a table
        var originalBuilder = RdlcReportBuilder.Create("InvoiceWithTable")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5)
                .WithBodyHeight(11))
            .WithObjectDataSource<InvoiceItem>("InvoiceDataSource")
            .WithDataSet("InvoiceItems", ds => ds
                .UsingDataSource("InvoiceDataSource")
                .WithFieldsFromType<InvoiceItem>())
            .WithTextBox("Title", tb => tb
                .WithText("Invoice Report")
                .WithBounds(0, 0, 8, 0.5)
                .WithFontSize(16)
                .Bold())
            .WithTablix("InvoiceTable", tbl => tbl
                .UsingDataSet("InvoiceItems")
                .WithBounds(0, 1, 8, 2)
                .WithColumn(3)
                .WithColumn(1)
                .WithColumn(2)
                .WithColumn(2)
                .WithHeaderRow(0.25, "Description", "Qty", "Unit Price", "Total")
                .WithDataRow(0.25, "=Fields!Description.Value", "=Fields!Quantity.Value", "=Format(Fields!UnitPrice.Value, \"C2\")", "=Format(Fields!LineTotal.Value, \"C2\")"));

        var originalXml = originalBuilder.ToXml();

        // Convert back to fluent code
        var fluentCode = _converter.ConvertToFluentCode(originalXml, "ConvertedInvoiceWithTable");

        // Verify table conversion
        Assert.IsTrue(fluentCode.Contains(".WithTablix(\"InvoiceTable\", tbl => tbl"));
        Assert.IsTrue(fluentCode.Contains(".WithDataSet(\"InvoiceItems\")"));
        Assert.IsTrue(fluentCode.Contains(".WithColumn(3)"));
        Assert.IsTrue(fluentCode.Contains(".WithColumn(1)"));
        Assert.IsTrue(fluentCode.Contains(".WithColumn(2)"));
        Assert.IsTrue(fluentCode.Contains(".WithHeaderRow(row => row"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"Description\")"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"Qty\")"));
        Assert.IsTrue(fluentCode.Contains(".WithDataRow(row => row"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"=Fields!Description.Value\")"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"=Format(Fields!UnitPrice.Value, \\\"C2\\\")\")"));

        Console.WriteLine("Generated Fluent Code with Table:");
        Console.WriteLine(fluentCode);
    }

    [TestMethod]
    public void ConvertAndRender_ReportWithImage_GeneratesWorkingCode()
    {
        // Create a report with an embedded image
        var originalBuilder = RdlcReportBuilder.Create("ReportWithImage")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5)
                .WithBodyHeight(11))
            .WithEmbeddedImageFromBytes("TestLogo", CreateTestImageBytes(), "image/png")
            .WithTextBox("Title", tb => tb
                .WithText("Company Report")
                .WithBounds(2, 0, 6, 0.5)
                .WithFontSize(18)
                .Bold())
            .WithImage("CompanyLogo", img => img
                .FromEmbedded("TestLogo")
                .WithBounds(0, 0, 1.5, 1));

        var originalXml = originalBuilder.ToXml();

        // Convert back to fluent code
        var fluentCode = _converter.ConvertToFluentCode(originalXml, "ConvertedReportWithImage");

        // Verify image conversion
        Assert.IsTrue(fluentCode.Contains(".WithEmbeddedImageFromBytes(\"TestLogo\", Convert.FromBase64String("));
        Assert.IsTrue(fluentCode.Contains(".WithImage(\"CompanyLogo\", img => img"));
        Assert.IsTrue(fluentCode.Contains(".WithEmbeddedImageSource(\"TestLogo\")"));
        Assert.IsTrue(fluentCode.Contains(".WithBounds(0, 0, 1.5, 1)"));

        Console.WriteLine("Generated Fluent Code with Image:");
        Console.WriteLine(fluentCode);
    }

    [TestMethod]
    public void ConvertExistingInvoiceTest_ProducesEquivalentOutput()
    {
        // Read the existing invoice test RDLC and convert it
        var existingInvoiceBuilder = RdlcReportBuilder.Create("ExistingInvoice")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5)
                .WithBodyHeight(11))
            .WithObjectDataSource<InvoiceItem>("InvoiceDataSource")
            .WithDataSet("InvoiceItems", ds => ds
                .UsingDataSource("InvoiceDataSource")
                .WithFieldsFromType<InvoiceItem>())
            .WithTextBox("CompanyInfo", tb => tb
                .WithExpression("=Parameters!CompanyName.Value + vbCrLf + " +
                               "Parameters!CompanyAddress.Value + vbCrLf + " +
                               "Parameters!CompanyCityState.Value + vbCrLf + " +
                               "'Phone: ' + Parameters!CompanyPhone.Value + vbCrLf + " +
                               "'Email: ' + Parameters!CompanyEmail.Value")
                .WithBounds(3, 0.2, 4.5, 1.5)
                .WithFontSize(11)
                .Bold())
            .WithTextBox("InvoiceTitle", tb => tb
                .WithText("INVOICE")
                .WithBounds(0, 2.2, 8, 0.5)
                .WithFontSize(24)
                .Bold())
            .WithInvoiceTable("InvoiceItems", 0, 4.2, 8, 1.5);

        var existingXml = existingInvoiceBuilder.ToXml();

        // Convert to fluent code
        var convertedCode = _converter.ConvertToFluentCode(existingXml, "ConvertedExistingInvoice");

        // Verify key elements are preserved
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"CompanyInfo\""));
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"InvoiceTitle\""));
        Assert.IsTrue(convertedCode.Contains("WithTablix(\"InvoiceItems\""));
        Assert.IsTrue(convertedCode.Contains("WithExpression(\"=Parameters!CompanyName.Value"));
        Assert.IsTrue(convertedCode.Contains("WithText(\"INVOICE\")"));
        Assert.IsTrue(convertedCode.Contains("WithFontSize(24)"));
        Assert.IsTrue(convertedCode.Contains("Bold()"));

        Console.WriteLine("Converted Existing Invoice:");
        Console.WriteLine(convertedCode);
    }

    [TestMethod]
    public void ConvertComplexReport_WithAllElements_GeneratesCompleteCode()
    {
        // Create a complex report with all supported elements
        var complexBuilder = RdlcReportBuilder.Create("ComplexReport")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5, 0.5, 0.25, 0.75)
                .WithBodyHeight(10.5))
            .WithObjectDataSource<InvoiceItem>("InvoiceDataSource")
            .WithDataSet("InvoiceItems", ds => ds
                .UsingDataSource("InvoiceDataSource")
                .WithFieldsFromType<InvoiceItem>())
            .WithEmbeddedImageFromBytes("CompanyLogo", CreateTestImageBytes(), "image/png")
            .WithImage("LogoImage", img => img
                .FromEmbedded("CompanyLogo")
                .WithBounds(0, 0, 2, 1))
            .WithTextBox("Header", tb => tb
                .WithText("Complex Business Report")
                .WithBounds(2.5, 0, 5.5, 0.5)
                .WithFontSize(20)
                .Bold())
            .WithTextBox("Subtitle", tb => tb
                .WithExpression("='Generated on: ' + Format(Today(), 'MM/dd/yyyy')")
                .WithBounds(2.5, 0.5, 5.5, 0.25)
                .WithFontSize(10)
                )
            .WithTextBox("CompanyDetails", tb => tb
                .WithExpression("=Parameters!CompanyName.Value + vbCrLf + " +
                               "Parameters!CompanyAddress.Value + vbCrLf + " +
                               "'Phone: ' + Parameters!CompanyPhone.Value")
                .WithBounds(0, 1.5, 4, 1)
                .WithFontSize(11))
            .WithTablix("DataTable", tbl => tbl
                .UsingDataSet("InvoiceItems")
                .WithBounds(0, 3, 8, 3)
                .WithColumn(2.5)
                .WithColumn(1)
                .WithColumn(1.5)
                .WithColumn(1.5)
                .WithColumn(1.5)
                .WithHeaderRow(0.25, "Description", "Qty", "Unit Price", "Tax", "Total")
                .WithDataRow(0.25, "=Fields!Description.Value", "=Fields!Quantity.Value", "=Format(Fields!UnitPrice.Value, \"C2\")", "=Format(Fields!TaxAmount.Value, \"C2\")", "=Format(Fields!TotalWithTax.Value, \"C2\")"))
            .WithTextBox("Summary", tb => tb
                .WithExpression("='Total Items: ' + CStr(Sum(Fields!Quantity.Value, 'InvoiceItems')) + vbCrLf + " +
                               "'Grand Total: ' + Format(Sum(Fields!TotalWithTax.Value, 'InvoiceItems'), 'C2')")
                .WithBounds(5, 6.5, 3, 1)
                .WithFontSize(12)
                .Bold())
            .WithTextBox("Footer", tb => tb
                .WithText("© 2024 Company Name. All rights reserved.")
                .WithBounds(0, 9.5, 8, 0.25)
                .WithFontSize(8));

        var complexXml = complexBuilder.ToXml();

        // Convert to fluent code
        var convertedCode = _converter.ConvertToFluentCode(complexXml, "ConvertedComplexReport");

        // Verify all elements are converted
        Assert.IsTrue(convertedCode.Contains("WithMargins(0.5, 0.5, 0.25, 0.75)"));
        Assert.IsTrue(convertedCode.Contains("WithBodyHeight(10.5)"));
        Assert.IsTrue(convertedCode.Contains("WithEmbeddedImageFromBytes(\"CompanyLogo\""));
        Assert.IsTrue(convertedCode.Contains("WithImage(\"LogoImage\""));
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"Header\""));
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"Subtitle\""));
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"CompanyDetails\""));
        Assert.IsTrue(convertedCode.Contains("WithTablix(\"DataTable\""));
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"Summary\""));
        Assert.IsTrue(convertedCode.Contains("WithTextBox(\"Footer\""));
        Assert.IsTrue(convertedCode.Contains("WithColumn(2.5)"));
        Assert.IsTrue(convertedCode.Contains("WithColumn(1)"));
        Assert.IsTrue(convertedCode.Contains("WithColumn(1.5)"));
        Assert.IsTrue(convertedCode.Contains("WithHeaderRow(row => row"));
        Assert.IsTrue(convertedCode.Contains("WithDataRow(row => row"));

        Console.WriteLine("Converted Complex Report:");
        Console.WriteLine(convertedCode);
        
        // Verify the code length is substantial (indicates complete conversion)
        Assert.IsTrue(convertedCode.Length > 2000, "Generated code should be substantial for complex report");
    }

    [TestMethod]
    public void ConvertAndCompare_SimpleReport_StructuralEquivalence()
    {
        // Create a simple report
        var originalBuilder = RdlcReportBuilder.Create("OriginalReport")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5)
                .WithBodyHeight(11))
            .WithTextBox("Title", tb => tb
                .WithText("Test Report")
                .WithBounds(0, 0, 8, 0.5)
                .WithFontSize(16)
                .Bold())
            .WithTextBox("Content", tb => tb
                .WithExpression("='Report Date: ' + Format(Today(), 'MM/dd/yyyy')")
                .WithBounds(0, 1, 8, 0.25)
                .WithFontSize(11));

        var originalXml = originalBuilder.ToXml();

        // Convert to fluent code
        var convertedCode = _converter.ConvertToFluentCode(originalXml, "ConvertedReport");

        // Verify structural elements match
        var originalElementCount = CountXmlElements(originalXml, "Textbox");
        var convertedTextBoxCount = CountStringOccurrences(convertedCode, ".WithTextBox(");
        
        Assert.AreEqual(originalElementCount, convertedTextBoxCount, "Number of textboxes should match");

        // Verify key content is preserved
        Assert.IsTrue(convertedCode.Contains("Test Report"));
        Assert.IsTrue(convertedCode.Contains("Format(Today(), \"MM/dd/yyyy\")"));
        Assert.IsTrue(convertedCode.Contains("WithFontSize(16)"));
        Assert.IsTrue(convertedCode.Contains("Bold()"));
    }

    private byte[] CreateTestImageBytes()
    {
        // Create a simple 1x1 PNG image for testing
        var base64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
        return Convert.FromBase64String(base64Image);
    }

    private int CountXmlElements(string xml, string elementName)
    {
        return xml.Split(new string[] { $"<{elementName}" }, StringSplitOptions.None).Length - 1;
    }

    private int CountStringOccurrences(string text, string pattern)
    {
        return text.Split(new string[] { pattern }, StringSplitOptions.None).Length - 1;
    }

    private static string GetOutputPath(string fileName)
    {
        var outputDir = @"C:\Output";
        Directory.CreateDirectory(outputDir);
        return Path.Combine(outputDir, fileName);
    }
}