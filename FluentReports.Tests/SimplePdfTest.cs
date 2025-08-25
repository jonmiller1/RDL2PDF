using FluentReports.Core.PdfRenderer;

namespace FluentReports.Tests;

public class SimplePdfTest
{
    [Fact]
    public void CanCreateValidPdf()
    {
        // Create 8.5x11 inch page at 300 DPI
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw border 0.5 inches from edges
        var margin = renderer.InchesToPoints(0.5f);
        var pageWidth = renderer.InchesToPoints(8.5f);
        var pageHeight = renderer.InchesToPoints(11f);
        
        renderer.DrawLine(margin, margin, pageWidth - margin, margin, 2f, PdfColor.Red);           // Top
        renderer.DrawLine(pageWidth - margin, margin, pageWidth - margin, pageHeight - margin, 2f, PdfColor.Green); // Right
        renderer.DrawLine(pageWidth - margin, pageHeight - margin, margin, pageHeight - margin, 2f, PdfColor.Blue); // Bottom
        renderer.DrawLine(margin, pageHeight - margin, margin, margin, 2f, PdfColor.Yellow);      // Left
        
        // Add title text
        renderer.DrawTextInches("8.5\" x 11\" Page at 300 DPI", 1f, 1f, 16f, PdfColor.Black);
        renderer.DrawTextInches("This document demonstrates high-resolution PDF rendering", 1f, 1.5f, 12f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        
        Directory.CreateDirectory(@"c:\output");
        File.WriteAllBytes(@"c:\output\Letter300DPI.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        
        // Verify PDF header
        var pdfString = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, Math.Min(10, pdfBytes.Length));
        Assert.StartsWith("%PDF-", pdfString);
    }

    [Fact]
    public void CanUseInchesAndPixels()
    {
        // Create 8.5x11 inch page at 300 DPI
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw using inch measurements
        renderer.DrawTextInches("Text positioned using inches", 1f, 1f, 14f, PdfColor.Blue);
        renderer.DrawLineInches(1f, 2f, 7.5f, 2f, 3f, PdfColor.Red);
        
        // Draw using pixel measurements (at 300 DPI)
        renderer.DrawTextPixels("Text positioned using pixels", 300, 900, 12f, PdfColor.Green);  // 1 inch = 300 pixels at 300 DPI
        renderer.DrawLinePixels(300, 1200, 2250, 1200, 2f, PdfColor.Magenta);  // 1-7.5 inches in pixels
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\Letter300DPI_Measurements.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanCreateFromPixels()
    {
        // Create page using pixel dimensions at 300 DPI (8.5x11 = 2550x3300 pixels)
        using var renderer = SimplePdfRenderer.CreateFromPixels(2550f, 3300f, 300f);
        
        // Draw corner markers to verify page size
        renderer.DrawTextPixels("TOP-LEFT", 10, 10, 12f, PdfColor.Red);
        renderer.DrawTextPixels("TOP-RIGHT", 2400, 10, 12f, PdfColor.Green);
        renderer.DrawTextPixels("BOTTOM-LEFT", 10, 3280, 12f, PdfColor.Blue);
        renderer.DrawTextPixels("BOTTOM-RIGHT", 2350, 3280, 12f, PdfColor.Magenta);
        
        // Draw center crosshairs
        renderer.DrawLinePixels(1275, 0, 1275, 3300, 1f, PdfColor.Black);   // Vertical center
        renderer.DrawLinePixels(0, 1650, 2550, 1650, 1f, PdfColor.Black);   // Horizontal center
        
        renderer.DrawTextPixels("Center: 1275x1650 pixels", 1000, 1650, 14f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\Letter300DPI_FromPixels.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanDrawGridLines()
    {
        // Create 8.5x11 inch page at 300 DPI
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw grid lines every 0.5 inches
        var pageWidth = 8.5f;
        var pageHeight = 11f;
        var gridSpacing = 0.5f;
        
        // Draw vertical grid lines (every 0.5 inches across width)
        for (float x = 0; x <= pageWidth; x += gridSpacing)
        {
            // Alternate between light gray and darker gray
            var color = (x % 1.0f == 0) ? new PdfColor(0.7f, 0.7f, 0.7f) : new PdfColor(0.9f, 0.9f, 0.9f);
            var lineWidth = (x % 1.0f == 0) ? 1f : 0.5f;
            
            renderer.DrawLineInches(x, 0, x, pageHeight, lineWidth, color);
        }
        
        // Draw horizontal grid lines (every 0.5 inches down height)
        for (float y = 0; y <= pageHeight; y += gridSpacing)
        {
            // Alternate between light gray and darker gray
            var color = (y % 1.0f == 0) ? new PdfColor(0.7f, 0.7f, 0.7f) : new PdfColor(0.9f, 0.9f, 0.9f);
            var lineWidth = (y % 1.0f == 0) ? 1f : 0.5f;
            
            renderer.DrawLineInches(0, y, pageWidth, y, lineWidth, color);
        }
        
        // Add labels at major grid intersections (every inch)
        for (float x = 1; x < pageWidth; x += 1.0f)
        {
            for (float y = 1; y < pageHeight; y += 1.0f)
            {
                renderer.DrawTextInches($"{x:0},{y:0}", x + 0.1f, y - 0.2f, 8f, PdfColor.Red);
            }
        }
        
        // Add title
        renderer.DrawTextInches("0.5 Inch Grid Test", 0.5f, 0.3f, 16f, PdfColor.Black);
        renderer.DrawTextInches("Grid lines every 0.5 inches, labels at inch marks", 0.5f, 0.7f, 12f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\GridTest_HalfInch.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }
}