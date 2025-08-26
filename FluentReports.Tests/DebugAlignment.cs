using FluentReports.Core.PdfRenderer;

namespace FluentReports.Tests;

public class DebugAlignment
{
    [Fact]
    public void DebugTextAlignment()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        var pageWidthPoints = 8.5f * 72f; // 612 points
        var margin = 10f;
        
        // Test text positioning at specific coordinates
        var testText = "TEST";
        var fontSize = 24f;
        var font = PdfFont.HelveticaBold;
        
        // Use watermarks to test positioning since I don't have direct access to SimplePdfRenderer methods
        // Position 1: Test left margin
        var leftWatermark = new TextWatermark("LEFT")
        {
            Position = WatermarkPosition.TopLeft,
            FontSize = fontSize,
            Color = PdfColor.Blue,
            Font = font
        };
        renderer.AddWatermark(leftWatermark);
        
        // Position 2: Test right margin - this is what's failing
        var rightWatermark = new TextWatermark("RIGHT")
        {
            Position = WatermarkPosition.TopRight,
            FontSize = fontSize,
            Color = PdfColor.Red,
            Font = font
        };
        renderer.AddWatermark(rightWatermark);
        
        // Position 3: Test center
        var centerWatermark = new TextWatermark("CENTER")
        {
            Position = WatermarkPosition.Center,
            FontSize = fontSize,
            Color = PdfColor.Green,
            Font = font
        };
        renderer.AddWatermark(centerWatermark);
        
        // Draw reference lines using inch measurements
        renderer.DrawLineInches(margin/72f, 0, margin/72f, 11f, 0.01f, PdfColor.Gray); // Left margin line
        renderer.DrawLineInches((pageWidthPoints - margin)/72f, 0, (pageWidthPoints - margin)/72f, 11f, 0.01f, PdfColor.Gray); // Right margin line
        
        // Add debug info
        renderer.DrawTextInches($"Page width: {pageWidthPoints:F0} pts", 1f, 1f, 0.1f, PdfColor.Black);
        renderer.DrawTextInches($"Margin: {margin:F0} pts", 1f, 1.2f, 0.1f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\DebugAlignment.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }
}