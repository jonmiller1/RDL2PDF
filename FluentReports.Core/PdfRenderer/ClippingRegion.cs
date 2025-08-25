using System.Text;

namespace FluentReports.Core.PdfRenderer;

public abstract class ClippingRegion
{
    internal abstract void ApplyToContent(StringBuilder content, Func<float, float> convertY);
}

public class RectangularClip : ClippingRegion
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    
    public RectangularClip(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    internal override void ApplyToContent(StringBuilder content, Func<float, float> convertY)
    {
        var pdfY = convertY(Y + Height); // Convert to PDF coordinates and adjust for height
        content.AppendLine($"{X:F2} {pdfY:F2} {Width:F2} {Height:F2} re");
        content.AppendLine("W"); // Set clipping path
        content.AppendLine("n"); // End path without drawing
    }
}

public class CircularClip : ClippingRegion
{
    public float CenterX { get; }
    public float CenterY { get; }
    public float Radius { get; }
    
    public CircularClip(float centerX, float centerY, float radius)
    {
        CenterX = centerX;
        CenterY = centerY;
        Radius = radius;
    }
    
    internal override void ApplyToContent(StringBuilder content, Func<float, float> convertY)
    {
        var pdfY = convertY(CenterY); // Convert center Y to PDF coordinates
        
        // Draw circle using Bézier curves for clipping path
        var k = 0.5522847498f * Radius; // Magic number for circle approximation
        
        content.AppendLine($"{CenterX:F2} {pdfY + Radius:F2} m"); // Move to top
        content.AppendLine($"{CenterX + k:F2} {pdfY + Radius:F2} {CenterX + Radius:F2} {pdfY + k:F2} {CenterX + Radius:F2} {pdfY:F2} c"); // Top-right curve
        content.AppendLine($"{CenterX + Radius:F2} {pdfY - k:F2} {CenterX + k:F2} {pdfY - Radius:F2} {CenterX:F2} {pdfY - Radius:F2} c"); // Bottom-right curve
        content.AppendLine($"{CenterX - k:F2} {pdfY - Radius:F2} {CenterX - Radius:F2} {pdfY - k:F2} {CenterX - Radius:F2} {pdfY:F2} c"); // Bottom-left curve
        content.AppendLine($"{CenterX - Radius:F2} {pdfY + k:F2} {CenterX - k:F2} {pdfY + Radius:F2} {CenterX:F2} {pdfY + Radius:F2} c"); // Top-left curve
        
        content.AppendLine("W"); // Set clipping path
        content.AppendLine("n"); // End path without drawing
    }
}