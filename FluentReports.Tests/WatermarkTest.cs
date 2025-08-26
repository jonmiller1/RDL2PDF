using FluentReports.Core.PdfRenderer;

namespace FluentReports.Tests;

public class WatermarkTest
{
    [Fact]
    public void CanCreateTextWatermarks()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        // Add a center watermark
        var centerWatermark = new TextWatermark("CONFIDENTIAL")
        {
            Position = WatermarkPosition.Center,
            FontSize = 72f,
            Color = PdfColor.LightGray,
            Font = PdfFont.HelveticaBold,
            Rotation = 45f,
            Opacity = 0.3f,
            Layer = WatermarkLayer.Background
        };
        
        renderer.AddWatermark(centerWatermark);
        
        // Add a "TL" watermark in the top left for comparison
        var tlWatermark = new TextWatermark("TL")
        {
            Position = WatermarkPosition.TopLeft,
            FontSize = 24f,
            Color = PdfColor.Blue,
            Font = PdfFont.HelveticaBold,
            Opacity = 0.8f,
            Layer = WatermarkLayer.Foreground
        };
        renderer.AddWatermark(tlWatermark);
        
        // Add a "TR" watermark in the top right
        var trWatermark = new TextWatermark("TR")
        {
            Position = WatermarkPosition.TopRight,
            FontSize = 24f,
            Color = PdfColor.Red,
            Font = PdfFont.HelveticaBold,
            Opacity = 0.8f,
            Layer = WatermarkLayer.Foreground
        };
        renderer.AddWatermark(trWatermark);
        
        // Add a "BL" watermark in the bottom left
        var blWatermark = new TextWatermark("BL")
        {
            Position = WatermarkPosition.BottomLeft,
            FontSize = 24f,
            Color = PdfColor.Green,
            Font = PdfFont.HelveticaBold,
            Opacity = 0.8f,
            Layer = WatermarkLayer.Foreground
        };
        renderer.AddWatermark(blWatermark);
        
        // Add a "BR" watermark in the bottom right
        var brWatermark = new TextWatermark("BR")
        {
            Position = WatermarkPosition.BottomRight,
            FontSize = 24f,
            Color = PdfColor.Magenta,
            Font = PdfFont.HelveticaBold,
            Opacity = 0.8f,
            Layer = WatermarkLayer.Foreground
        };
        renderer.AddWatermark(brWatermark);
        
        // Add content to demonstrate watermarks
        renderer.DrawTextInches("Sample Document", 1f, 1f, 0.3f, PdfColor.Black, PdfFont.HelveticaBold);
        renderer.DrawTextInches("This document demonstrates text watermarks.", 1f, 1.5f, 0.12f, PdfColor.Black);
        renderer.DrawTextInches("The 'CONFIDENTIAL' watermark should appear behind this text.", 1f, 2f, 0.12f, PdfColor.Black);
        renderer.DrawTextInches("The 'DRAFT' watermark should appear in front in the top right.", 1f, 2.5f, 0.12f, PdfColor.Black);
        
        // Add another page to test watermarks on multiple pages
        renderer.AddNewPage();
        renderer.DrawTextInches("Page 2", 1f, 1f, 0.3f, PdfColor.Black, PdfFont.HelveticaBold);
        renderer.DrawTextInches("Watermarks should appear on all pages by default.", 1f, 1.5f, 0.12f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\WatermarkTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal(2, renderer.TotalPages);
    }
    
    [Fact]
    public void CanCreatePositionalWatermarks()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        // Add watermarks in different positions to demonstrate positioning
        var positions = new[]
        {
            WatermarkPosition.TopLeft,
            WatermarkPosition.TopCenter,
            WatermarkPosition.TopRight,
            WatermarkPosition.MiddleLeft,
            WatermarkPosition.Center,
            WatermarkPosition.MiddleRight,
            WatermarkPosition.BottomLeft,
            WatermarkPosition.BottomCenter,
            WatermarkPosition.BottomRight
        };
        
        for (int i = 0; i < positions.Length; i++)
        {
            var watermark = new TextWatermark($"{positions[i]}")
            {
                Position = positions[i],
                FontSize = 16f,
                Color = i < 3 ? PdfColor.Red : i < 6 ? PdfColor.Green : PdfColor.Blue,
                Font = PdfFont.Helvetica,
                Opacity = 0.7f
            };
            
            renderer.AddWatermark(watermark);
        }
        
        // Add content
        renderer.DrawTextInches("Positional Watermarks Test", 2f, 5f, 0.2f, PdfColor.Black, PdfFont.HelveticaBold);
        renderer.DrawTextInches("This page demonstrates all 9 watermark positions.", 1f, 5.5f, 0.12f, PdfColor.Black);
        renderer.DrawTextInches("Top positions are in red, middle in green, bottom in blue.", 1f, 6f, 0.12f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\PositionalWatermarksTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }
    
    [Fact]
    public void CanCreatePageSpecificWatermarks()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        // Watermark that only appears on first page
        var titlePageWatermark = new TextWatermark("TITLE PAGE")
        {
            Position = WatermarkPosition.BottomCenter,
            FontSize = 24f,
            Color = PdfColor.DarkBlue,
            Font = PdfFont.HelveticaBold,
            SpecificPages = new[] { 1 }, // Only on page 1
            Opacity = 0.6f
        };
        
        renderer.AddWatermark(titlePageWatermark);
        
        // Watermark that appears on all pages except first
        var contentWatermark = new TextWatermark("INTERNAL USE")
        {
            Position = WatermarkPosition.TopCenter,
            FontSize = 18f,
            Color = PdfColor.Gray,
            Font = PdfFont.Helvetica,
            SkipFirstPage = true,
            Opacity = 0.4f
        };
        
        renderer.AddWatermark(contentWatermark);
        
        // Watermark only on last page
        var finalPageWatermark = new TextWatermark("END OF DOCUMENT")
        {
            Position = WatermarkPosition.Center,
            FontSize = 32f,
            Color = PdfColor.DarkRed,
            Font = PdfFont.HelveticaBold,
            Rotation = -15f,
            SkipLastPage = false,
            ShowOnAllPages = false,
            Opacity = 0.5f
        };
        
        // Create 3 pages
        for (int page = 1; page <= 3; page++)
        {
            if (page > 1) renderer.AddNewPage();
            
            renderer.DrawTextInches($"Page {page}", 1f, 1f, 0.25f, PdfColor.Black, PdfFont.HelveticaBold);
            
            if (page == 1)
            {
                renderer.DrawTextInches("This is the title page. Should show 'TITLE PAGE' watermark.", 1f, 1.5f, 0.12f, PdfColor.Black);
            }
            else if (page == 3)
            {
                renderer.DrawTextInches("This is the last page. Should show 'INTERNAL USE' and 'END OF DOCUMENT'.", 1f, 1.5f, 0.12f, PdfColor.Black);
                // Add the final page watermark only to the last page
                finalPageWatermark.SpecificPages = new[] { page };
                renderer.AddWatermark(finalPageWatermark);
            }
            else
            {
                renderer.DrawTextInches("This is a content page. Should only show 'INTERNAL USE'.", 1f, 1.5f, 0.12f, PdfColor.Black);
            }
        }
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\PageSpecificWatermarksTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal(3, renderer.TotalPages);
    }
}