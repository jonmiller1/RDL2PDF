using System.Globalization;
using System.Text;

namespace FluentReports.Core.PdfRenderer;

public class SimplePdfRenderer : IDisposable
{
    private readonly StringBuilder _content = new();
    private readonly float _width;
    private readonly float _height;
    private bool _disposed = false;

    public float DPI { get; set; } = 72f;

    public SimplePdfRenderer(float widthPoints = 612f, float heightPoints = 792f)
    {
        _width = widthPoints;
        _height = heightPoints;
    }

    public static SimplePdfRenderer CreateFromInches(float widthInches, float heightInches, float dpi = 72f)
    {
        var widthPoints = widthInches * dpi;
        var heightPoints = heightInches * dpi;
        var renderer = new SimplePdfRenderer(widthPoints, heightPoints);
        renderer.DPI = dpi;
        return renderer;
    }

    public static SimplePdfRenderer CreateFromPixels(float widthPixels, float heightPixels, float dpi = 96f)
    {
        var widthPoints = widthPixels * 72f / dpi;
        var heightPoints = heightPixels * 72f / dpi;
        var renderer = new SimplePdfRenderer(widthPoints, heightPoints);
        renderer.DPI = dpi;
        return renderer;
    }

    public float InchesToPoints(float inches) => inches * DPI;
    public float PointsToInches(float points) => points / DPI;
    public float PixelsToPoints(float pixels) => pixels * 72f / DPI;
    public float PointsToPixels(float points) => points * DPI / 72f;

    private float ConvertY(float y) => _height - y;

    public void DrawLine(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        var pdfY1 = ConvertY(y1);
        var pdfY2 = ConvertY(y2);
        
        _content.AppendLine("q");
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} RG");
        }
        
        _content.AppendLine($"{lineWidth:F2} w");
        _content.AppendLine($"{x1:F2} {pdfY1:F2} m");
        _content.AppendLine($"{x2:F2} {pdfY2:F2} l");
        _content.AppendLine("S");
        _content.AppendLine("Q");
    }

    public void DrawLineInches(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        DrawLine(InchesToPoints(x1), InchesToPoints(y1), InchesToPoints(x2), InchesToPoints(y2), lineWidth, color);
    }

    public void DrawLinePixels(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        DrawLine(PixelsToPoints(x1), PixelsToPoints(y1), PixelsToPoints(x2), PixelsToPoints(y2), lineWidth, color);
    }

    public void DrawText(string text, float x, float y, float fontSize = 12f, PdfColor? color = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        var pdfY = ConvertY(y);
        
        _content.AppendLine("BT");
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} rg");
        }
        
        _content.AppendLine($"/F1 {fontSize:F2} Tf");
        _content.AppendLine($"{x:F2} {pdfY:F2} Td");
        _content.AppendLine($"({EscapeText(text)}) Tj");
        _content.AppendLine("ET");
    }

    public void DrawTextInches(string text, float x, float y, float fontSize = 12f, PdfColor? color = null)
    {
        DrawText(text, InchesToPoints(x), InchesToPoints(y), fontSize, color);
    }

    public void DrawTextPixels(string text, float x, float y, float fontSize = 12f, PdfColor? color = null)
    {
        DrawText(text, PixelsToPoints(x), PixelsToPoints(y), fontSize, color);
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

    public byte[] ToByteArray()
    {
        var contentStream = _content.ToString();
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);
        
        var pdf = new StringBuilder();
        
        // PDF Header
        pdf.AppendLine("%PDF-1.4");
        
        var positions = new List<long>();
        
        // Object 1: Catalog
        positions.Add(pdf.Length);
        pdf.AppendLine("1 0 obj");
        pdf.AppendLine("<<");
        pdf.AppendLine("/Type /Catalog");
        pdf.AppendLine("/Pages 2 0 R");
        pdf.AppendLine(">>");
        pdf.AppendLine("endobj");
        
        // Object 2: Pages
        positions.Add(pdf.Length);
        pdf.AppendLine("2 0 obj");
        pdf.AppendLine("<<");
        pdf.AppendLine("/Type /Pages");
        pdf.AppendLine("/Count 1");
        pdf.AppendLine("/Kids [3 0 R]");
        pdf.AppendLine(">>");
        pdf.AppendLine("endobj");
        
        // Object 3: Page
        positions.Add(pdf.Length);
        pdf.AppendLine("3 0 obj");
        pdf.AppendLine("<<");
        pdf.AppendLine("/Type /Page");
        pdf.AppendLine("/Parent 2 0 R");
        pdf.AppendLine($"/MediaBox [0 0 {_width.ToString(CultureInfo.InvariantCulture)} {_height.ToString(CultureInfo.InvariantCulture)}]");
        pdf.AppendLine("/Resources <<");
        pdf.AppendLine("  /Font << /F1 4 0 R >>");
        pdf.AppendLine(">>");
        pdf.AppendLine("/Contents 5 0 R");
        pdf.AppendLine(">>");
        pdf.AppendLine("endobj");
        
        // Object 4: Font
        positions.Add(pdf.Length);
        pdf.AppendLine("4 0 obj");
        pdf.AppendLine("<<");
        pdf.AppendLine("/Type /Font");
        pdf.AppendLine("/Subtype /Type1");
        pdf.AppendLine("/BaseFont /Helvetica");
        pdf.AppendLine(">>");
        pdf.AppendLine("endobj");
        
        // Object 5: Content Stream
        positions.Add(pdf.Length);
        pdf.AppendLine("5 0 obj");
        pdf.AppendLine("<<");
        pdf.AppendLine($"/Length {contentBytes.Length}");
        pdf.AppendLine(">>");
        pdf.AppendLine("stream");
        pdf.Append(contentStream);
        pdf.AppendLine("endstream");
        pdf.AppendLine("endobj");
        
        // Cross-reference table
        var xrefPos = pdf.Length;
        pdf.AppendLine("xref");
        pdf.AppendLine("0 6");
        pdf.AppendLine("0000000000 65535 f ");
        
        foreach (var pos in positions)
        {
            pdf.AppendLine($"{pos:D10} 00000 n ");
        }
        
        // Trailer
        pdf.AppendLine("trailer");
        pdf.AppendLine("<<");
        pdf.AppendLine("/Size 6");
        pdf.AppendLine("/Root 1 0 R");
        pdf.AppendLine(">>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefPos.ToString());
        pdf.AppendLine("%%EOF");
        
        return Encoding.ASCII.GetBytes(pdf.ToString());
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