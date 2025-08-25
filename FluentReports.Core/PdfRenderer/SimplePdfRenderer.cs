using System.Globalization;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace FluentReports.Core.PdfRenderer;

public class SimplePdfRenderer : IDisposable
{
    private readonly StringBuilder _content = new();
    private readonly float _width;
    private readonly float _height;
    private readonly Dictionary<string, int> _images = new();
    private readonly List<(string objectData, byte[] streamData)> _imageData = new();
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

    public void DrawImage(string imagePath, float x, float y, float width, float height)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}");

        var imageKey = imagePath;
        if (!_images.ContainsKey(imageKey))
        {
            // Load and process image
            var imageObjectName = $"Im{_images.Count + 1}";
            var (objectData, streamData) = ProcessImage(imagePath);
            _imageData.Add((objectData, streamData));
            _images[imageKey] = _images.Count + 1;
        }

        var pdfY = ConvertY(y + height); // Convert to PDF coordinates and adjust for image height
        var imageIndex = _images[imageKey];

        _content.AppendLine("q"); // Save graphics state
        _content.AppendLine($"{width:F2} 0 0 {height:F2} {x:F2} {pdfY:F2} cm"); // Transform matrix
        _content.AppendLine($"/Im{imageIndex} Do"); // Draw image
        _content.AppendLine("Q"); // Restore graphics state
    }

    public void DrawImageInches(string imagePath, float x, float y, float width, float height)
    {
        DrawImage(imagePath, InchesToPoints(x), InchesToPoints(y), InchesToPoints(width), InchesToPoints(height));
    }

    public void DrawImagePixels(string imagePath, float x, float y, float width, float height)
    {
        DrawImage(imagePath, PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(width), PixelsToPoints(height));
    }

    private (string objectData, byte[] streamData) ProcessImage(string imagePath)
    {
        using var image = System.Drawing.Image.FromFile(imagePath);
        
        // Convert to bitmap if needed
        using var bitmap = new Bitmap(image);
        
        // Convert to JPEG format for PDF embedding
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Jpeg);
        var imageBytes = memoryStream.ToArray();
        
        // Create the object data (without the stream)
        var objectData = $@"<<
/Type /XObject
/Subtype /Image
/Width {bitmap.Width}
/Height {bitmap.Height}
/ColorSpace /DeviceRGB
/BitsPerComponent 8
/Filter /DCTDecode
/Length {imageBytes.Length}
>>";
        
        return (objectData, imageBytes);
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
        
        using var pdfStream = new MemoryStream();
        using var writer = new BinaryWriter(pdfStream, Encoding.ASCII);
        
        var positions = new List<long>();
        
        // PDF Header
        WriteText(writer, "%PDF-1.4\n");
        
        // Object 1: Catalog
        positions.Add(pdfStream.Length);
        WriteText(writer, "1 0 obj\n<<\n/Type /Catalog\n/Pages 2 0 R\n>>\nendobj\n");
        
        // Object 2: Pages
        positions.Add(pdfStream.Length);
        WriteText(writer, "2 0 obj\n<<\n/Type /Pages\n/Count 1\n/Kids [3 0 R]\n>>\nendobj\n");
        
        // Object 3: Page
        positions.Add(pdfStream.Length);
        WriteText(writer, "3 0 obj\n<<\n/Type /Page\n/Parent 2 0 R\n");
        WriteText(writer, $"/MediaBox [0 0 {_width.ToString(CultureInfo.InvariantCulture)} {_height.ToString(CultureInfo.InvariantCulture)}]\n");
        WriteText(writer, "/Resources <<\n  /Font << /F1 4 0 R >>\n");
        
        // Add image resources if any
        if (_images.Count > 0)
        {
            WriteText(writer, "  /XObject <<");
            for (int i = 1; i <= _images.Count; i++)
            {
                WriteText(writer, $" /Im{i} {5 + i} 0 R");
            }
            WriteText(writer, " >>\n");
        }
        
        WriteText(writer, ">>\n/Contents 5 0 R\n>>\nendobj\n");
        
        // Object 4: Font
        positions.Add(pdfStream.Length);
        WriteText(writer, "4 0 obj\n<<\n/Type /Font\n/Subtype /Type1\n/BaseFont /Helvetica\n>>\nendobj\n");
        
        // Object 5: Content Stream
        positions.Add(pdfStream.Length);
        WriteText(writer, $"5 0 obj\n<<\n/Length {contentBytes.Length}\n>>\nstream\n");
        writer.Write(contentBytes);
        WriteText(writer, "endstream\nendobj\n");
        
        // Image objects (starting from object 6)
        for (int i = 0; i < _imageData.Count; i++)
        {
            positions.Add(pdfStream.Length);
            WriteText(writer, $"{6 + i} 0 obj\n");
            WriteText(writer, _imageData[i].objectData);
            WriteText(writer, "\nstream\n");
            writer.Write(_imageData[i].streamData);
            WriteText(writer, "endstream\nendobj\n");
        }
        
        // Cross-reference table
        var xrefPos = pdfStream.Length;
        var totalObjects = 5 + _imageData.Count + 1;
        WriteText(writer, $"xref\n0 {totalObjects}\n0000000000 65535 f \n");
        
        foreach (var pos in positions)
        {
            WriteText(writer, $"{pos:D10} 00000 n \n");
        }
        
        // Trailer
        WriteText(writer, $"trailer\n<<\n/Size {totalObjects}\n/Root 1 0 R\n>>\n");
        WriteText(writer, $"startxref\n{xrefPos}\n%%EOF");
        
        return pdfStream.ToArray();
    }

    private void WriteText(BinaryWriter writer, string text)
    {
        writer.Write(Encoding.ASCII.GetBytes(text));
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