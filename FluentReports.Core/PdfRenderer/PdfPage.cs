using System.Globalization;
using System.Text;

namespace FluentReports.Core.PdfRenderer;

public class PdfPage : IDisposable
{
    private readonly PdfDocument _document;
    private readonly StringBuilder _content = new();
    private readonly float _width;
    private readonly float _height;
    private int? _objectId;
    private bool _disposed = false;

    internal PdfPage(PdfDocument document, float width, float height)
    {
        _document = document;
        _width = width;
        _height = height;
        
        // Initialize empty content stream
    }

    public float Width => _width;
    public float Height => _height;

    // Convert from top-left origin (0,0) to PDF bottom-left origin
    private float ConvertY(float y) => _height - y;

    public void DrawLine(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        // Convert coordinates to PDF coordinate system (bottom-left origin)
        var pdfY1 = ConvertY(y1);
        var pdfY2 = ConvertY(y2);
        
        _content.AppendLine($"q"); // Save graphics state
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} RG"); // Set stroke color
        }
        
        _content.AppendLine($"{lineWidth:F2} w"); // Set line width
        _content.AppendLine($"{x1:F2} {pdfY1:F2} m"); // Move to start point
        _content.AppendLine($"{x2:F2} {pdfY2:F2} l"); // Line to end point
        _content.AppendLine("S"); // Stroke
        _content.AppendLine("Q"); // Restore graphics state
    }

    public void DrawLineInches(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        DrawLine(
            _document.InchesToPoints(x1),
            _document.InchesToPoints(y1),
            _document.InchesToPoints(x2),
            _document.InchesToPoints(y2),
            lineWidth,
            color
        );
    }

    public void DrawLinePixels(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        DrawLine(
            _document.PixelsToPoints(x1),
            _document.PixelsToPoints(y1),
            _document.PixelsToPoints(x2),
            _document.PixelsToPoints(y2),
            lineWidth,
            color
        );
    }

    public void DrawText(string text, float x, float y, float fontSize = 12f, PdfColor? color = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        var pdfY = ConvertY(y);
        
        _content.AppendLine("BT"); // Begin text
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} rg"); // Set fill color
        }
        
        _content.AppendLine($"/F1 {fontSize:F2} Tf"); // Set font and size
        _content.AppendLine($"{x:F2} {pdfY:F2} Td"); // Set text position
        _content.AppendLine($"({EscapeText(text)}) Tj"); // Show text
        _content.AppendLine("ET"); // End text
    }

    public void DrawTextInches(string text, float x, float y, float fontSize = 12f, PdfColor? color = null)
    {
        DrawText(text, _document.InchesToPoints(x), _document.InchesToPoints(y), fontSize, color);
    }

    public void DrawTextPixels(string text, float x, float y, float fontSize = 12f, PdfColor? color = null)
    {
        DrawText(text, _document.PixelsToPoints(x), _document.PixelsToPoints(y), fontSize, color);
    }

    private string EscapeText(string text)
    {
        return text.Replace("\\", "\\\\")
                  .Replace("(", "\\(")
                  .Replace(")", "\\)")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t");
    }

    internal void PrepareObjects()
    {
        if (_objectId.HasValue)
            return;

        // Reserve object IDs
        var catalogId = 1;
        var pagesId = 2;
        var pageId = _document.ReserveObjectId();
        var fontId = _document.ReserveObjectId();
        var contentId = _document.ReserveObjectId();

        _objectId = pageId;

        // Create catalog
        _document.SetObject(catalogId, "<<\n/Type /Catalog\n/Pages 2 0 R\n>>");

        // Create pages object
        _document.SetObject(pagesId, $"<<\n/Type /Pages\n/Count 1\n/Kids [{pageId} 0 R]\n>>");

        // Create font resource
        _document.SetObject(fontId, "<<\n/Type /Font\n/Subtype /Type1\n/BaseFont /Helvetica\n>>");

        // Create content stream
        var contentStream = _content.ToString();
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);
        _document.SetObject(contentId, $"<<\n/Length {contentBytes.Length}\n>>\nstream\n{contentStream}endstream");

        // Create page object
        _document.SetObject(pageId, $"<<\n/Type /Page\n/Parent 2 0 R\n/MediaBox [0 0 {_width.ToString(CultureInfo.InvariantCulture)} {_height.ToString(CultureInfo.InvariantCulture)}]\n/Resources <<\n  /Font << /F1 {fontId} 0 R >>\n>>\n/Contents {contentId} 0 R\n>>");
    }

    internal int GetObjectId()
    {
        if (!_objectId.HasValue)
            PrepareObjects();
        return _objectId.Value;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _content.Clear();
            _disposed = true;
        }
    }
}