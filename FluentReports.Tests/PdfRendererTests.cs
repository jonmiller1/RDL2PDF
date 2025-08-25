using FluentReports.Core.PdfRenderer;

namespace FluentReports.Tests;

public class PdfRendererTests
{
    [Fact]
    public void CanCreateBasicPdf()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw a border using points (converted from inches)
        var margin = 30f; // 30 points margin
        var pageWidth = renderer.InchesToPoints(8.5f);
        var pageHeight = renderer.InchesToPoints(11f);
        
        renderer.DrawLine(margin, margin, pageWidth - margin, margin, 2f, PdfColor.Red);    // Top
        renderer.DrawLine(pageWidth - margin, margin, pageWidth - margin, pageHeight - margin, 2f, PdfColor.Green); // Right  
        renderer.DrawLine(pageWidth - margin, pageHeight - margin, margin, pageHeight - margin, 2f, PdfColor.Blue); // Bottom
        renderer.DrawLine(margin, pageHeight - margin, margin, margin, 2f, PdfColor.Yellow); // Left
        
        // Add some text
        renderer.DrawText("Hello World - Fixed Basic Test", 50, 50, 16f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        
        Directory.CreateDirectory(@"c:\output");
        File.WriteAllBytes(@"c:\output\BasicTest_Fixed.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        
        // Verify PDF header
        var pdfString = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, Math.Min(10, pdfBytes.Length));
        Assert.StartsWith("%PDF-", pdfString);
    }

    [Fact]
    public void CanUseInchMeasurements()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw using inch measurements
        renderer.DrawLineInches(0.5f, 0.5f, 7.5f, 0.5f, 3f, PdfColor.Red);
        renderer.DrawTextInches("Text at 1 inch from top - Fixed Test", 1f, 1f, 14f, PdfColor.Blue);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\InchTest_Fixed.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanUsePixelMeasurements()
    {
        using var renderer = SimplePdfRenderer.CreateFromPixels(2550f, 3300f, 300f); // 8.5x11 at 300 DPI
        
        // Draw using pixel measurements
        renderer.DrawLinePixels(300, 300, 1500, 300, 2f, PdfColor.Green);  // 1-5 inches at 300 DPI
        renderer.DrawTextPixels("Text at 450 pixels from top - Fixed Test", 300, 450, 12f, PdfColor.Magenta);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\PixelTest_Fixed.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CoordinateSystemTest()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        var pageWidth = renderer.InchesToPoints(8.5f);
        var pageHeight = renderer.InchesToPoints(11f);
        
        // Test that (0,0) is top-left by drawing corner indicators
        renderer.DrawText("TOP-LEFT (0,0) - Fixed", 5, 5, 12f, PdfColor.Red);
        renderer.DrawText("TOP-RIGHT", pageWidth - 200, 5, 12f, PdfColor.Green);
        renderer.DrawText("BOTTOM-LEFT", 5, pageHeight - 25, 12f, PdfColor.Blue);
        renderer.DrawText("BOTTOM-RIGHT", pageWidth - 200, pageHeight - 25, 12f, PdfColor.Magenta);
        
        // Draw crosshairs at origin
        renderer.DrawLine(0, 0, 150, 0, 2f, PdfColor.Black);
        renderer.DrawLine(0, 0, 0, 150, 2f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\CoordinateTest_Fixed.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }
}