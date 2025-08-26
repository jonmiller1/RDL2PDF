namespace FluentReports.Core.PdfRenderer;

public enum WatermarkPosition
{
    Center,
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public enum WatermarkLayer
{
    Background,  // Behind all content
    Foreground   // In front of all content
}

public abstract class Watermark
{
    public WatermarkPosition Position { get; set; } = WatermarkPosition.Center;
    public WatermarkLayer Layer { get; set; } = WatermarkLayer.Background;
    public float OffsetX { get; set; } = 0f; // Additional offset in points
    public float OffsetY { get; set; } = 0f; // Additional offset in points
    public float Rotation { get; set; } = 0f; // Rotation in degrees
    public float Opacity { get; set; } = 0.3f; // 0.0 = transparent, 1.0 = opaque
    public bool ShowOnAllPages { get; set; } = true;
    public int[] SpecificPages { get; set; } = Array.Empty<int>(); // Only show on these pages (1-based)
    public bool SkipFirstPage { get; set; } = false;
    public bool SkipLastPage { get; set; } = false;
    
    internal abstract void Render(SimplePdfRenderer renderer, float pageWidth, float pageHeight, int pageNumber, int totalPages);
}

public class TextWatermark : Watermark
{
    public string Text { get; set; } = "";
    public float FontSize { get; set; } = 48f;
    public PdfFont? Font { get; set; } = PdfFont.HelveticaBold;
    public PdfColor? Color { get; set; } = PdfColor.Gray;
    
    public TextWatermark(string text)
    {
        Text = text;
    }
    
    internal override void Render(SimplePdfRenderer renderer, float pageWidth, float pageHeight, int pageNumber, int totalPages)
    {
        if (!this.ShouldShowOnPage(pageNumber, totalPages)) return;
        
        // Calculate position using renderer's text width estimation
        var (x, y) = CalculatePosition(pageWidth, pageHeight, Text, FontSize, Font, renderer);
        
        
        // Apply watermark rendering with rotation and opacity
        renderer.SaveGraphicsState();
        
        // Set opacity if supported (simplified approach)
        if (Opacity < 1.0f)
        {
            // For now, we'll simulate transparency by using a lighter color
            var adjustedColor = AdjustColorForOpacity(Color ?? PdfColor.Gray, Opacity);
            renderer.DrawTextWithRotation(Text, x + OffsetX, y + OffsetY, FontSize, adjustedColor, Font, Rotation);
        }
        else
        {
            renderer.DrawTextWithRotation(Text, x + OffsetX, y + OffsetY, FontSize, Color, Font, Rotation);
        }
        
        renderer.RestoreGraphicsState();
    }
    
    private PdfColor AdjustColorForOpacity(PdfColor originalColor, float opacity)
    {
        // Blend with white background to simulate transparency
        var r = originalColor.R + (1f - originalColor.R) * (1f - opacity);
        var g = originalColor.G + (1f - originalColor.G) * (1f - opacity);
        var b = originalColor.B + (1f - originalColor.B) * (1f - opacity);
        return new PdfColor(r, g, b);
    }
    
    private (float x, float y) CalculatePosition(float pageWidth, float pageHeight, string text, float fontSize, PdfFont? font, SimplePdfRenderer renderer)
    {
        // Use renderer's exact text width calculation
        var textWidth = renderer.EstimateTextWidthForFont(text, fontSize, font ?? PdfFont.HelveticaBold);
        
        var textHeight = fontSize;
        
        // Calculate rotated text bounding box dimensions
        var radians = Rotation * Math.PI / 180.0;
        var cos = Math.Abs(Math.Cos(radians));
        var sin = Math.Abs(Math.Sin(radians));
        var rotatedWidth = textWidth * cos + textHeight * sin;
        var rotatedHeight = textWidth * sin + textHeight * cos;
        
        // Add margins to prevent text from touching page edges
        var margin = 10f; // 10 points margin
        
        var position = Position switch
        {
            WatermarkPosition.TopLeft => (textWidth/2 + margin, rotatedHeight / 2 + margin),
            WatermarkPosition.TopCenter => (pageWidth / 2, rotatedHeight / 2 + margin),
            WatermarkPosition.TopRight => (pageWidth - textWidth*1.2f - margin, rotatedHeight / 2 + margin),
            WatermarkPosition.MiddleLeft => (textWidth/2 + margin, pageHeight / 2),
            WatermarkPosition.MiddleRight => (pageWidth - textWidth*1.2f - margin, pageHeight / 2),
            WatermarkPosition.BottomLeft => (textWidth/2 + margin, pageHeight - rotatedHeight / 2 - margin),
            WatermarkPosition.BottomCenter => (pageWidth / 2, pageHeight - rotatedHeight / 2 - margin),
            WatermarkPosition.BottomRight => (pageWidth - textWidth*1.2f - margin, pageHeight - rotatedHeight / 2 - margin),
            _ => (pageWidth / 2, pageHeight / 2) // Center
        };
        
        
        // Apply offsets - trust the positioning logic and user's offsets
        var finalX = position.Item1 + OffsetX;
        var finalY = position.Item2 + OffsetY;
        
        return ((float)finalX, (float)finalY);
    }
    
    private float EstimateTextWidth(string text, float fontSize, PdfFont? font)
    {
        // Use the same estimation logic as SimplePdfRenderer
        var totalWidth = 0f;
        foreach (char c in text)
        {
            // Rough character width based on typical font metrics
            var charWidth = c switch
            {
                ' ' => 0.25f,
                'i' or 'l' or 'I' or 'j' or 't' => 0.3f,
                'f' or 'r' => 0.35f,
                'a' or 'c' or 'e' or 'g' or 'n' or 'o' or 's' or 'u' or 'v' or 'x' or 'z' => 0.5f,
                'b' or 'd' or 'h' or 'k' or 'p' or 'q' or 'y' => 0.55f,
                'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G' or 'H' or 'K' or 'L' or 'N' or 'O' or 'P' or 'R' or 'S' or 'T' or 'U' or 'V' or 'X' or 'Y' or 'Z' => 0.7f,
                'w' => 0.75f,
                'W' or 'M' => 0.85f,
                'm' => 0.8f,
                _ => 0.5f // Default for other characters including numbers and symbols
            };
            totalWidth += charWidth * fontSize;
        }
        return totalWidth;
    }
}

public class ImageWatermark : Watermark
{
    public string ImagePath { get; set; } = "";
    public float Width { get; set; } = 200f; // Width in points
    public float Height { get; set; } = 200f; // Height in points
    public bool MaintainAspectRatio { get; set; } = true;
    
    public ImageWatermark(string imagePath)
    {
        ImagePath = imagePath;
    }
    
    internal override void Render(SimplePdfRenderer renderer, float pageWidth, float pageHeight, int pageNumber, int totalPages)
    {
        if (!this.ShouldShowOnPage(pageNumber, totalPages)) return;
        if (string.IsNullOrEmpty(ImagePath)) return;
        
        // Calculate position
        var (x, y) = CalculatePosition(pageWidth, pageHeight);
        
        // Apply watermark rendering with rotation and opacity
        renderer.SaveGraphicsState();
        
        try
        {
            renderer.DrawImageWithRotation(ImagePath, x + OffsetX, y + OffsetY, Width, Height, Rotation, Opacity);
        }
        catch
        {
            // If image fails to load, skip silently
        }
        
        renderer.RestoreGraphicsState();
    }
    
    private (float x, float y) CalculatePosition(float pageWidth, float pageHeight)
    {
        return Position switch
        {
            WatermarkPosition.TopLeft => (Width / 2, pageHeight - Height / 2),
            WatermarkPosition.TopCenter => (pageWidth / 2, pageHeight - Height / 2),
            WatermarkPosition.TopRight => (pageWidth - Width / 2, pageHeight - Height / 2),
            WatermarkPosition.MiddleLeft => (Width / 2, pageHeight / 2),
            WatermarkPosition.MiddleRight => (pageWidth - Width / 2, pageHeight / 2),
            WatermarkPosition.BottomLeft => (Width / 2, Height / 2),
            WatermarkPosition.BottomCenter => (pageWidth / 2, Height / 2),
            WatermarkPosition.BottomRight => (pageWidth - Width / 2, Height / 2),
            _ => (pageWidth / 2, pageHeight / 2) // Center
        };
    }
}

// Extension methods for the base Watermark class
public static class WatermarkExtensions
{
    public static bool ShouldShowOnPage(this Watermark watermark, int pageNumber, int totalPages)
    {
        // Check if specific pages are set
        if (watermark.SpecificPages.Length > 0)
        {
            return watermark.SpecificPages.Contains(pageNumber);
        }
        
        // Check skip conditions
        if (watermark.SkipFirstPage && pageNumber == 1) return false;
        if (watermark.SkipLastPage && pageNumber == totalPages) return false;
        
        return watermark.ShowOnAllPages;
    }
}