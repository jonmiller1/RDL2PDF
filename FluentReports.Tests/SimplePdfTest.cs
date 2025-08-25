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

    [Fact]
    public void CanEmbedImages()
    {
        // Create a test image first
        var testImagePath = @"c:\output\TestImage.jpg";
        CreateTestImage(testImagePath);

        // Create 8.5x11 inch page at 300 DPI
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Add title
        renderer.DrawTextInches("Image Embedding Test", 1f, 0.5f, 18f, PdfColor.Black);
        
        // Draw images at different sizes and positions
        renderer.DrawImageInches(testImagePath, 1f, 1f, 2f, 1.5f);  // 2" x 1.5" image
        renderer.DrawTextInches("2\" x 1.5\" image", 1f, 2.7f, 12f, PdfColor.Blue);
        
        renderer.DrawImageInches(testImagePath, 4f, 1f, 1f, 1f);    // 1" x 1" square
        renderer.DrawTextInches("1\" x 1\" square", 4f, 2.2f, 12f, PdfColor.Blue);
        
        renderer.DrawImageInches(testImagePath, 6f, 1f, 1.5f, 2f);  // 1.5" x 2" portrait
        renderer.DrawTextInches("1.5\" x 2\" portrait", 6f, 3.2f, 12f, PdfColor.Blue);
        
        // Draw same image multiple times (should reuse)
        renderer.DrawImageInches(testImagePath, 1f, 4f, 0.75f, 0.75f);
        renderer.DrawImageInches(testImagePath, 2f, 4f, 0.75f, 0.75f);
        renderer.DrawImageInches(testImagePath, 3f, 4f, 0.75f, 0.75f);
        renderer.DrawTextInches("Same image reused 3 times", 1f, 5f, 12f, PdfColor.Green);
        
        // Test pixel-based positioning
        renderer.DrawImagePixels(testImagePath, 300, 1800, 150, 150); // 1" square at 300 DPI
        renderer.DrawTextPixels("1\" square using pixels", 300, 1980, 12f, PdfColor.Red);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\ImageTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    private void CreateTestImage(string path)
    {
        // Create a simple test image with some graphics
        using var bitmap = new System.Drawing.Bitmap(400, 300);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        
        // Fill background
        graphics.Clear(System.Drawing.Color.LightBlue);
        
        // Draw some shapes
        using var redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
        using var greenPen = new System.Drawing.Pen(System.Drawing.Color.Green, 3);
        using var font = new System.Drawing.Font("Arial", 24, System.Drawing.FontStyle.Bold);
        using var blackBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
        
        graphics.FillEllipse(redBrush, 50, 50, 100, 100);
        graphics.DrawRectangle(greenPen, 200, 50, 120, 80);
        graphics.DrawString("TEST", font, blackBrush, 150, 180);
        
        // Save as JPEG
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
    }

    [Fact]
    public void CanUseDifferentFonts()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Test built-in PDF fonts
        renderer.DrawTextInches("Helvetica Regular", 1f, 1f, 14f, PdfColor.Black, PdfFont.Helvetica);
        renderer.DrawTextInches("Helvetica Bold", 1f, 1.5f, 14f, PdfColor.Black, PdfFont.HelveticaBold);
        renderer.DrawTextInches("Helvetica Italic", 1f, 2f, 14f, PdfColor.Black, PdfFont.HelveticaOblique);
        renderer.DrawTextInches("Helvetica Bold Italic", 1f, 2.5f, 14f, PdfColor.Black, PdfFont.HelveticaBoldOblique);
        
        renderer.DrawTextInches("Times Roman", 1f, 3.5f, 14f, PdfColor.Blue, PdfFont.TimesRoman);
        renderer.DrawTextInches("Times Bold", 1f, 4f, 14f, PdfColor.Blue, PdfFont.TimesBold);
        renderer.DrawTextInches("Times Italic", 1f, 4.5f, 14f, PdfColor.Blue, PdfFont.TimesItalic);
        renderer.DrawTextInches("Times Bold Italic", 1f, 5f, 14f, PdfColor.Blue, PdfFont.TimesBoldItalic);
        
        renderer.DrawTextInches("Courier (Monospace)", 1f, 6f, 14f, PdfColor.Green, PdfFont.Courier);
        renderer.DrawTextInches("Courier Bold", 1f, 6.5f, 14f, PdfColor.Green, PdfFont.CourierBold);
        renderer.DrawTextInches("Courier Italic", 1f, 7f, 14f, PdfColor.Green, PdfFont.CourierOblique);
        renderer.DrawTextInches("Courier Bold Italic", 1f, 7.5f, 14f, PdfColor.Green, PdfFont.CourierBoldOblique);
        
        // Test font reuse (same font used multiple times should only create one font object)
        renderer.DrawTextInches("Helvetica reused", 5f, 1f, 12f, PdfColor.Red, PdfFont.Helvetica);
        renderer.DrawTextInches("Times reused", 5f, 1.5f, 12f, PdfColor.Red, PdfFont.TimesRoman);
        
        // Test default font (should use Helvetica when null)
        renderer.DrawTextInches("Default font (Helvetica)", 1f, 8.5f, 14f, PdfColor.Magenta);
        
        // Title
        renderer.DrawTextInches("Font Test - Built-in PDF Fonts", 1f, 0.5f, 18f, PdfColor.Black, PdfFont.HelveticaBold);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\FontTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact] 
    public void CanTrySystemFont()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        renderer.DrawTextInches("System Font Fallback Test", 1f, 0.5f, 18f, PdfColor.Black, PdfFont.HelveticaBold);
        
        // Try to use a system font (will fallback to built-in fonts)
        try
        {
            var arialFont = PdfFont.FromSystemFont("Arial");
            renderer.DrawTextInches("Arial System Font → Fallback to Helvetica", 1f, 1f, 16f, PdfColor.Black, arialFont);
            renderer.DrawTextInches("(System fonts automatically fallback to built-in PDF fonts)", 1f, 1.5f, 12f, PdfColor.Blue, arialFont);
        }
        catch (FileNotFoundException)
        {
            // System font not found, use built-in instead
            renderer.DrawTextInches("Arial not found - using Helvetica directly", 1f, 1f, 16f, PdfColor.Red, PdfFont.Helvetica);
            renderer.DrawTextInches("System fonts directory did not contain Arial", 1f, 1.5f, 12f, PdfColor.Red, PdfFont.Helvetica);
        }
        
        // Show the fallback system working
        renderer.DrawTextInches("Fallback System Examples:", 1f, 2.5f, 14f, PdfColor.Green, PdfFont.HelveticaBold);
        renderer.DrawTextInches("• Arial → Helvetica", 1f, 3f, 12f, PdfColor.Green, PdfFont.Helvetica);
        renderer.DrawTextInches("• Times → Times-Roman", 1f, 3.5f, 12f, PdfColor.Green, PdfFont.TimesRoman);
        renderer.DrawTextInches("• Courier → Courier", 1f, 4f, 12f, PdfColor.Green, PdfFont.Courier);
        
        // Always include built-in fonts for comparison
        renderer.DrawTextInches("Pure Built-in Fonts:", 1f, 5f, 14f, PdfColor.Blue, PdfFont.HelveticaBold);
        renderer.DrawTextInches("Built-in Helvetica", 1f, 5.5f, 12f, PdfColor.Blue, PdfFont.Helvetica);
        renderer.DrawTextInches("Built-in Times Roman", 1f, 6f, 12f, PdfColor.Blue, PdfFont.TimesRoman);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\SystemFontTest_Fixed.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanDrawShapesAndFills()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Title
        renderer.DrawTextInches("Shape and Fill Pattern Test", 1f, 0.5f, 18f, PdfColor.Black, PdfFont.HelveticaBold);
        
        // Rectangle examples
        renderer.DrawTextInches("Rectangles:", 0.5f, 1.2f, 14f, PdfColor.Black, PdfFont.HelveticaBold);
        
        // Stroke only rectangle
        renderer.DrawRectangleInches(0.5f, 1.5f, 1.5f, 1f, 2f, PdfColor.Red);
        renderer.DrawTextInches("Stroke Only", 0.5f, 2.7f, 10f, PdfColor.Red);
        
        // Fill only rectangle  
        renderer.DrawRectangleInches(2.5f, 1.5f, 1.5f, 1f, fillColor: PdfColor.Blue);
        renderer.DrawTextInches("Fill Only", 2.5f, 2.7f, 10f, PdfColor.Blue);
        
        // Fill and stroke rectangle
        renderer.DrawRectangleInches(4.5f, 1.5f, 1.5f, 1f, 3f, PdfColor.Green, new PdfColor(0.8f, 1f, 0.8f));
        renderer.DrawTextInches("Fill + Stroke", 4.5f, 2.7f, 10f, PdfColor.Green);
        
        // Different fill colors
        renderer.DrawRectangleInches(6.5f, 1.5f, 1.5f, 1f, 1f, PdfColor.Black, PdfColor.Yellow);
        renderer.DrawTextInches("Yellow Fill", 6.5f, 2.7f, 10f, PdfColor.Black);
        
        // Circle examples
        renderer.DrawTextInches("Circles:", 0.5f, 3.5f, 14f, PdfColor.Black, PdfFont.HelveticaBold);
        
        // Stroke only circle
        renderer.DrawCircleInches(1.25f, 4.5f, 0.5f, 2f, PdfColor.Magenta);
        renderer.DrawTextInches("Stroke Only", 0.75f, 5.2f, 10f, PdfColor.Magenta);
        
        // Fill only circle
        renderer.DrawCircleInches(3.25f, 4.5f, 0.5f, fillColor: new PdfColor(1f, 0.5f, 0f)); // Orange
        renderer.DrawTextInches("Fill Only", 2.75f, 5.2f, 10f, new PdfColor(1f, 0.5f, 0f));
        
        // Fill and stroke circle
        renderer.DrawCircleInches(5.25f, 4.5f, 0.5f, 2f, PdfColor.Black, PdfColor.Cyan);
        renderer.DrawTextInches("Fill + Stroke", 4.75f, 5.2f, 10f, PdfColor.Black);
        
        // Small circles
        renderer.DrawCircleInches(7.25f, 4.5f, 0.25f, 1f, PdfColor.Red, PdfColor.White);
        renderer.DrawTextInches("Small Circle", 6.75f, 5.2f, 10f, PdfColor.Red);
        
        // Using pixel coordinates for precision
        renderer.DrawTextInches("Pixel-based Shapes:", 0.5f, 6f, 14f, PdfColor.Black, PdfFont.HelveticaBold);
        
        // Small rectangles using pixel coordinates
        for (int i = 0; i < 5; i++)
        {
            var color = i switch
            {
                0 => PdfColor.Red,
                1 => new PdfColor(1f, 0.5f, 0f), // Orange
                2 => PdfColor.Yellow,
                3 => PdfColor.Green,
                4 => PdfColor.Blue,
                _ => PdfColor.Black
            };
            renderer.DrawRectanglePixels(150 + i * 60, 2040, 50, 80, 1f, PdfColor.Black, color);
        }
        renderer.DrawTextInches("Color gradient using pixels", 0.5f, 7.2f, 10f, PdfColor.Black);
        
        // Overlapping shapes for layering test
        renderer.DrawTextInches("Layered Shapes:", 0.5f, 7.8f, 14f, PdfColor.Black, PdfFont.HelveticaBold);
        
        renderer.DrawRectangleInches(1f, 8.2f, 1f, 0.8f, fillColor: PdfColor.Red);
        renderer.DrawCircleInches(1.7f, 8.6f, 0.4f, fillColor: PdfColor.Blue);
        renderer.DrawTextInches("Overlapping", 0.8f, 9.3f, 10f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\ShapesTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanUseDifferentFontSizeUnits()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Test font sizes in points (PDF native)
        renderer.DrawTextPoints("12pt text", 72f, 72f, 12f, PdfColor.Black);
        renderer.DrawTextPoints("18pt text", 72f, 100f, 18f, PdfColor.Black);
        renderer.DrawTextPoints("24pt text", 72f, 140f, 24f, PdfColor.Black);
        
        // Test font sizes in inches  
        renderer.DrawTextInches("0.167\" text (12pt)", 1f, 2.5f, 0.167f, PdfColor.Blue);
        renderer.DrawTextInches("0.25\" text (18pt)", 1f, 3f, 0.25f, PdfColor.Blue);
        renderer.DrawTextInches("0.33\" text (24pt)", 1f, 3.7f, 0.33f, PdfColor.Blue);
        
        // Test font sizes in pixels (at 300 DPI)
        renderer.DrawTextPixels("50px text (12pt)", 300f, 1050f, 50f, PdfColor.Red);
        renderer.DrawTextPixels("75px text (18pt)", 300f, 1150f, 75f, PdfColor.Red);
        renderer.DrawTextPixels("100px text (24pt)", 300f, 1300f, 100f, PdfColor.Red);
        
        // Add labels to show the units
        renderer.DrawTextPoints("Points:", 72f, 50f, 14f, PdfColor.Black);
        renderer.DrawTextInches("Inches:", 1f, 2f, 0.194f, PdfColor.Blue);
        renderer.DrawTextPixels("Pixels:", 300f, 900f, 58f, PdfColor.Red);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\FontSizeUnitsTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanAlignText()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw reference lines to show alignment positions
        float leftPos = 1f;
        float centerPos = 4.25f; // 8.5/2
        float rightPos = 7.5f;
        
        // Vertical lines to show alignment positions
        renderer.DrawLineInches(leftPos, 1f, leftPos, 10f, 0.5f, PdfColor.LightGray);
        renderer.DrawLineInches(centerPos, 1f, centerPos, 10f, 0.5f, PdfColor.LightGray);
        renderer.DrawLineInches(rightPos, 1f, rightPos, 10f, 0.5f, PdfColor.LightGray);
        
        // Left aligned text
        renderer.DrawTextInches("Left Aligned Text", leftPos, 2f, 0.2f, PdfColor.Black, alignment: TextAlignment.Left);
        renderer.DrawTextInches("This text starts at the left position", leftPos, 2.5f, 0.15f, PdfColor.Blue, alignment: TextAlignment.Left);
        
        // Center aligned text  
        renderer.DrawTextInches("Center Aligned Text", centerPos, 4f, 0.2f, PdfColor.Black, alignment: TextAlignment.Center);
        renderer.DrawTextInches("This text is centered", centerPos, 4.5f, 0.15f, PdfColor.Green, alignment: TextAlignment.Center);
        
        // Right aligned text
        renderer.DrawTextInches("Right Aligned Text", rightPos, 6f, 0.2f, PdfColor.Black, alignment: TextAlignment.Right);
        renderer.DrawTextInches("This text ends at the right position", rightPos, 6.5f, 0.15f, PdfColor.Red, alignment: TextAlignment.Right);
        
        // Mixed alignment demonstration
        renderer.DrawTextInches("Left", leftPos, 8f, 0.18f, PdfColor.Blue, alignment: TextAlignment.Left);
        renderer.DrawTextInches("Center", centerPos, 8f, 0.18f, PdfColor.Green, alignment: TextAlignment.Center);
        renderer.DrawTextInches("Right", rightPos, 8f, 0.18f, PdfColor.Red, alignment: TextAlignment.Right);
        
        // Labels for the reference lines
        renderer.DrawTextInches("L", leftPos, 0.5f, 0.12f, PdfColor.Black, alignment: TextAlignment.Center);
        renderer.DrawTextInches("C", centerPos, 0.5f, 0.12f, PdfColor.Black, alignment: TextAlignment.Center);
        renderer.DrawTextInches("R", rightPos, 0.5f, 0.12f, PdfColor.Black, alignment: TextAlignment.Center);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\TextAlignmentTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanUseLineStyles()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        float y = 1f;
        float lineSpacing = 0.5f;
        
        // Draw lines with different patterns
        renderer.DrawTextInches("Solid Line:", 0.5f, y, 0.15f, PdfColor.Black);
        renderer.DrawLineInches(2f, y + 0.05f, 7.5f, y + 0.05f, 2f, PdfColor.Black, LineStyle.Solid);
        y += lineSpacing;
        
        renderer.DrawTextInches("Dashed Line:", 0.5f, y, 0.15f, PdfColor.Black);
        renderer.DrawLineInches(2f, y + 0.05f, 7.5f, y + 0.05f, 2f, PdfColor.Blue, LineStyle.Dashed);
        y += lineSpacing;
        
        renderer.DrawTextInches("Dotted Line:", 0.5f, y, 0.15f, PdfColor.Black);
        renderer.DrawLineInches(2f, y + 0.05f, 7.5f, y + 0.05f, 2f, PdfColor.Red, LineStyle.Dotted);
        y += lineSpacing;
        
        renderer.DrawTextInches("Dash-Dot Line:", 0.5f, y, 0.15f, PdfColor.Black);
        renderer.DrawLineInches(2f, y + 0.05f, 7.5f, y + 0.05f, 2f, PdfColor.Green, LineStyle.DashDot);
        y += lineSpacing;
        
        renderer.DrawTextInches("Dash-Dot-Dot Line:", 0.5f, y, 0.15f, PdfColor.Black);
        renderer.DrawLineInches(2f, y + 0.05f, 7.5f, y + 0.05f, 2f, PdfColor.Magenta, LineStyle.DashDotDot);
        y += lineSpacing;
        
        // Custom dash pattern
        renderer.DrawTextInches("Custom Pattern:", 0.5f, y, 0.15f, PdfColor.Black);
        var customStyle = new LineStyle(new[] { 10f, 5f, 2f, 5f }, LineCap.Round);
        renderer.DrawLineInches(2f, y + 0.05f, 7.5f, y + 0.05f, 3f, PdfColor.Cyan, customStyle);
        y += lineSpacing * 2;
        
        // Test line caps with thick lines
        renderer.DrawTextInches("Line Caps:", 0.5f, y, 0.18f, PdfColor.Black);
        y += 0.3f;
        
        renderer.DrawTextInches("Butt Cap:", 1f, y, 0.12f, PdfColor.Black);
        renderer.DrawLineInches(2.5f, y + 0.05f, 4.5f, y + 0.05f, 8f, PdfColor.Red, new LineStyle(LineCap.Butt));
        
        renderer.DrawTextInches("Round Cap:", 1f, y + 0.3f, 0.12f, PdfColor.Black);
        renderer.DrawLineInches(2.5f, y + 0.35f, 4.5f, y + 0.35f, 8f, PdfColor.Blue, new LineStyle(LineCap.Round));
        
        renderer.DrawTextInches("Square Cap:", 1f, y + 0.6f, 0.12f, PdfColor.Black);
        renderer.DrawLineInches(2.5f, y + 0.65f, 4.5f, y + 0.65f, 8f, PdfColor.Green, new LineStyle(LineCap.Square));
        
        y += 1.2f;
        
        // Test shapes with line styles
        renderer.DrawTextInches("Shapes with Line Styles:", 0.5f, y, 0.18f, PdfColor.Black);
        y += 0.4f;
        
        // Dashed rectangle
        renderer.DrawRectangleInches(1f, y, 2f, 1f, 3f, PdfColor.Blue, strokeStyle: LineStyle.Dashed);
        
        // Dotted circle
        renderer.DrawCircleInches(5f, y + 0.5f, 0.5f, 2f, PdfColor.Red, strokeStyle: LineStyle.Dotted);
        
        // Custom pattern rectangle with round joins
        var roundJoinStyle = new LineStyle(new[] { 8f, 4f }, LineCap.Round, LineJoin.Round);
        renderer.DrawRectangleInches(6.5f, y, 1.5f, 1f, 4f, PdfColor.Green, strokeStyle: roundJoinStyle);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\LineStylesTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanUseClipping()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        // Draw some background shapes without clipping
        renderer.DrawTextInches("Without Clipping:", 0.5f, 1f, 0.18f, PdfColor.Black);
        renderer.DrawRectangleInches(1f, 1.5f, 3f, 1.5f, 2f, PdfColor.Blue, PdfColor.LightGray);
        renderer.DrawCircleInches(2.5f, 2.8f, 0.8f, 2f, PdfColor.Red, PdfColor.Yellow);
        renderer.DrawLineInches(0.5f, 2.2f, 4.5f, 2.2f, 3f, PdfColor.Green);
        
        // Test rectangular clipping
        renderer.DrawTextInches("Rectangular Clipping:", 0.5f, 4f, 0.18f, PdfColor.Black);
        
        // Set rectangular clipping region
        renderer.SetRectangularClipInches(1f, 4.5f, 2f, 1.5f);
        
        // Draw the same shapes - they should be clipped
        renderer.DrawRectangleInches(0.5f, 4.5f, 3f, 1.5f, 2f, PdfColor.Blue, PdfColor.LightGray);
        renderer.DrawCircleInches(2f, 5.7f, 0.8f, 2f, PdfColor.Red, PdfColor.Yellow);
        renderer.DrawLineInches(0f, 5.2f, 4f, 5.2f, 3f, PdfColor.Green);
        
        // Restore graphics state (remove clipping)
        renderer.RestoreGraphicsState();
        
        // Draw clipping region boundary for reference
        renderer.DrawRectangleInches(1f, 4.5f, 2f, 1.5f, 1f, PdfColor.Black, strokeStyle: LineStyle.Dashed);
        
        // Test circular clipping
        renderer.DrawTextInches("Circular Clipping:", 0.5f, 7f, 0.18f, PdfColor.Black);
        
        // Set circular clipping region
        renderer.SetCircularClipInches(2.5f, 8.5f, 1f);
        
        // Draw shapes that will be clipped to circle
        renderer.DrawRectangleInches(1.5f, 7.5f, 2f, 2f, 2f, PdfColor.Magenta, PdfColor.Cyan);
        renderer.DrawLineInches(1f, 8f, 4f, 9f, 4f, PdfColor.Red);
        renderer.DrawLineInches(4f, 8f, 1f, 9f, 4f, PdfColor.Blue);
        renderer.DrawTextInches("CLIPPED", 2f, 8.3f, 0.15f, PdfColor.Black);
        
        // Restore graphics state
        renderer.RestoreGraphicsState();
        
        // Draw circle boundary for reference
        renderer.DrawCircleInches(2.5f, 8.5f, 1f, 1f, PdfColor.Black, strokeStyle: LineStyle.Dotted);
        
        // Test nested clipping (clip within clip)
        renderer.DrawTextInches("Nested Clipping:", 5f, 2f, 0.18f, PdfColor.Black);
        
        // First level clipping - large rectangle
        renderer.SetRectangularClipInches(5.5f, 2.5f, 2.5f, 3f);
        renderer.DrawRectangleInches(5f, 2.5f, 3.5f, 3f, 2f, PdfColor.Green, PdfColor.LightGray);
        
        // Second level clipping - smaller circle inside rectangle
        renderer.SetCircularClipInches(6.75f, 4f, 0.8f);
        renderer.DrawRectangleInches(5.5f, 3f, 2.5f, 2f, 3f, PdfColor.Red, PdfColor.Yellow);
        renderer.DrawTextInches("NESTED", 6.2f, 3.8f, 0.12f, PdfColor.Black);
        
        // Restore both clipping levels
        renderer.RestoreGraphicsState(); // Remove circular clip
        renderer.RestoreGraphicsState(); // Remove rectangular clip
        
        // Draw reference boundaries
        renderer.DrawRectangleInches(5.5f, 2.5f, 2.5f, 3f, 1f, PdfColor.Black, strokeStyle: LineStyle.Dashed);
        renderer.DrawCircleInches(6.75f, 4f, 0.8f, 1f, PdfColor.Black, strokeStyle: LineStyle.Dotted);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\ClippingTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void CanMeasureTextWidth()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        var testStrings = new[]
        {
            "Hello World",
            "iiiiiii",     // narrow characters
            "MMMMMMMM",    // wide characters
            "123.45",
            "Text Alignment Test"
        };
        
        float y = 1f;
        renderer.DrawTextInches("Text Width Measurement Demo:", 0.5f, y, 0.2f, PdfColor.Black);
        y += 0.5f;
        
        foreach (var text in testStrings)
        {
            var fontSize = 0.167f; // 12pt
            var textWidth = renderer.MeasureTextWidthInches(text, fontSize);
            
            // Draw text aligned to left at 2 inches
            renderer.DrawTextInches(text, 2f, y, fontSize, PdfColor.Black);
            
            // Draw a line showing the measured width
            renderer.DrawLineInches(2f, y - 0.05f, 2f + textWidth, y - 0.05f, 1f, PdfColor.Red);
            
            // Show the measurement
            renderer.DrawTextInches($"Width: {textWidth:F3}\"", 5f, y, 0.12f, PdfColor.Blue);
            
            y += 0.3f;
        }
        
        // Test different font sizes
        y += 0.3f;
        renderer.DrawTextInches("Font Size Scaling:", 0.5f, y, 0.18f, PdfColor.Black);
        y += 0.3f;
        
        var testText = "Same text";
        var fontSizes = new[] { 0.1f, 0.15f, 0.2f, 0.25f }; // Different sizes in inches
        
        foreach (var fontSize in fontSizes)
        {
            var textWidth = renderer.MeasureTextWidthInches(testText, fontSize);
            
            renderer.DrawTextInches(testText, 2f, y, fontSize, PdfColor.Black);
            renderer.DrawLineInches(2f, y - 0.02f, 2f + textWidth, y - 0.02f, 1f, PdfColor.Green);
            renderer.DrawTextInches($"{fontSize * 72:F0}pt - {textWidth:F3}\"", 5f, y, 0.1f, PdfColor.Blue);
            
            y += fontSize + 0.1f; // Space based on font size
        }
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\TextMeasurementTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        
        // Test that measurements are reasonable
        var helloWidth = renderer.MeasureTextWidthInches("Hello World", 0.167f);
        Assert.True(helloWidth > 0.5f && helloWidth < 2f); // Should be reasonable width
        
        // Test that wider text measures larger
        var narrowWidth = renderer.MeasureTextWidthInches("iii", 0.167f);
        var wideWidth = renderer.MeasureTextWidthInches("MMM", 0.167f);
        Assert.True(wideWidth > narrowWidth);
    }
}