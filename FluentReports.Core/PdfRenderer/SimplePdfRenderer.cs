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
    private readonly Dictionary<string, int> _fonts = new();
    private readonly List<(PdfFont font, string objectData, byte[]? fontData)> _fontData = new();
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

    private int GetOrAddFont(PdfFont font)
    {
        var fontKey = $"{font.Name}_{font.Style}";
        if (!_fonts.ContainsKey(fontKey))
        {
            var fontIndex = _fonts.Count + 1;
            _fonts[fontKey] = fontIndex;

            if (font.IsEmbedded && font.FilePath != null)
            {
                // For now, treat embedded fonts as built-in fonts to avoid PDF corruption
                // TODO: Implement proper TrueType font embedding
                var fallbackFont = GetFallbackFont(font);
                var fontObject = CreateBuiltInFontObject(fallbackFont);
                _fontData.Add((fallbackFont, fontObject, null));
            }
            else
            {
                // Built-in font
                var fontObject = CreateBuiltInFontObject(font);
                _fontData.Add((font, fontObject, null));
            }
        }
        
        return _fonts[fontKey];
    }

    private PdfFont GetFallbackFont(PdfFont originalFont)
    {
        // Map system fonts to similar built-in PDF fonts
        var fontName = originalFont.Name.ToLowerInvariant();
        
        if (fontName.Contains("arial") || fontName.Contains("helvetica"))
        {
            return originalFont.Style switch
            {
                PdfFontStyle.Bold => PdfFont.HelveticaBold,
                PdfFontStyle.Italic => PdfFont.HelveticaOblique,
                PdfFontStyle.BoldItalic => PdfFont.HelveticaBoldOblique,
                _ => PdfFont.Helvetica
            };
        }
        else if (fontName.Contains("times"))
        {
            return originalFont.Style switch
            {
                PdfFontStyle.Bold => PdfFont.TimesBold,
                PdfFontStyle.Italic => PdfFont.TimesItalic,
                PdfFontStyle.BoldItalic => PdfFont.TimesBoldItalic,
                _ => PdfFont.TimesRoman
            };
        }
        else if (fontName.Contains("courier"))
        {
            return originalFont.Style switch
            {
                PdfFontStyle.Bold => PdfFont.CourierBold,
                PdfFontStyle.Italic => PdfFont.CourierOblique,
                PdfFontStyle.BoldItalic => PdfFont.CourierBoldOblique,
                _ => PdfFont.Courier
            };
        }
        
        // Default fallback to Helvetica
        return originalFont.Style switch
        {
            PdfFontStyle.Bold => PdfFont.HelveticaBold,
            PdfFontStyle.Italic => PdfFont.HelveticaOblique,
            PdfFontStyle.BoldItalic => PdfFont.HelveticaBoldOblique,
            _ => PdfFont.Helvetica
        };
    }

    private string CreateBuiltInFontObject(PdfFont font)
    {
        return $@"<<
/Type /Font
/Subtype /Type1
/BaseFont /{font.Name}
>>";
    }

    private string CreateEmbeddedFontObject(PdfFont font, byte[] fontBytes)
    {
        // For TrueType fonts, we create a more complex font object
        var fontFileObjectId = 5 + _imageData.Count + _fontData.Count * 2 + 1; // Calculate next available object ID
        
        return $@"<<
/Type /Font
/Subtype /TrueType
/BaseFont /{font.Name.Replace(" ", "")}
/FirstChar 32
/LastChar 255
/Widths [250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250 250]
/FontDescriptor {fontFileObjectId + 1} 0 R
>>";
    }

    private string CreateFontDescriptor(PdfFont font, int fontFileObjectId)
    {
        return $@"<<
/Type /FontDescriptor
/FontName /{font.Name.Replace(" ", "")}
/FontFile2 {fontFileObjectId} 0 R
/FontBBox [-100 -200 1000 800]
/ItalicAngle 0
/Ascent 800
/Descent -200
/CapHeight 700
/XHeight 500
/StemV 80
>>";
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

    public void DrawText(string text, float x, float y, float fontSize = 12f, PdfColor? color = null, PdfFont? font = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        // Use Helvetica as default font if none specified
        font ??= PdfFont.Helvetica;
        var fontIndex = GetOrAddFont(font);
        
        var pdfY = ConvertY(y);
        
        _content.AppendLine("BT");
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} rg");
        }
        
        _content.AppendLine($"/F{fontIndex} {fontSize:F2} Tf");
        _content.AppendLine($"{x:F2} {pdfY:F2} Td");
        _content.AppendLine($"({EscapeText(text)}) Tj");
        _content.AppendLine("ET");
    }

    public void DrawTextInches(string text, float x, float y, float fontSize = 12f, PdfColor? color = null, PdfFont? font = null)
    {
        DrawText(text, InchesToPoints(x), InchesToPoints(y), fontSize, color, font);
    }

    public void DrawTextPixels(string text, float x, float y, float fontSize = 12f, PdfColor? color = null, PdfFont? font = null)
    {
        DrawText(text, PixelsToPoints(x), PixelsToPoints(y), fontSize, color, font);
    }

    public void DrawRectangle(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null)
    {
        var pdfY = ConvertY(y + height); // Convert to PDF coordinates and adjust for rectangle height
        
        _content.AppendLine("q"); // Save graphics state
        
        // Set stroke color if provided
        if (strokeColor != null)
        {
            _content.AppendLine($"{strokeColor.R:F3} {strokeColor.G:F3} {strokeColor.B:F3} RG");
            _content.AppendLine($"{lineWidth:F2} w");
        }
        
        // Set fill color if provided
        if (fillColor != null)
        {
            _content.AppendLine($"{fillColor.R:F3} {fillColor.G:F3} {fillColor.B:F3} rg");
        }
        
        // Draw rectangle
        _content.AppendLine($"{x:F2} {pdfY:F2} {width:F2} {height:F2} re");
        
        // Choose drawing operation based on what's specified
        if (fillColor != null && strokeColor != null)
            _content.AppendLine("B"); // Fill and stroke
        else if (fillColor != null)
            _content.AppendLine("f"); // Fill only
        else
            _content.AppendLine("S"); // Stroke only (default)
            
        _content.AppendLine("Q"); // Restore graphics state
    }

    public void DrawRectangleInches(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null)
    {
        DrawRectangle(InchesToPoints(x), InchesToPoints(y), InchesToPoints(width), InchesToPoints(height), lineWidth, strokeColor, fillColor);
    }

    public void DrawRectanglePixels(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null)
    {
        DrawRectangle(PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(width), PixelsToPoints(height), lineWidth, strokeColor, fillColor);
    }

    public void DrawCircle(float centerX, float centerY, float radius, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null)
    {
        var pdfY = ConvertY(centerY); // Convert center Y to PDF coordinates
        
        _content.AppendLine("q"); // Save graphics state
        
        // Set stroke color if provided
        if (strokeColor != null)
        {
            _content.AppendLine($"{strokeColor.R:F3} {strokeColor.G:F3} {strokeColor.B:F3} RG");
            _content.AppendLine($"{lineWidth:F2} w");
        }
        
        // Set fill color if provided
        if (fillColor != null)
        {
            _content.AppendLine($"{fillColor.R:F3} {fillColor.G:F3} {fillColor.B:F3} rg");
        }
        
        // Draw circle using Bézier curves (4 curves for a complete circle)
        var k = 0.5522847498f * radius; // Magic number for circle approximation
        
        _content.AppendLine($"{centerX:F2} {pdfY + radius:F2} m"); // Move to top
        _content.AppendLine($"{centerX + k:F2} {pdfY + radius:F2} {centerX + radius:F2} {pdfY + k:F2} {centerX + radius:F2} {pdfY:F2} c"); // Top-right curve
        _content.AppendLine($"{centerX + radius:F2} {pdfY - k:F2} {centerX + k:F2} {pdfY - radius:F2} {centerX:F2} {pdfY - radius:F2} c"); // Bottom-right curve
        _content.AppendLine($"{centerX - k:F2} {pdfY - radius:F2} {centerX - radius:F2} {pdfY - k:F2} {centerX - radius:F2} {pdfY:F2} c"); // Bottom-left curve
        _content.AppendLine($"{centerX - radius:F2} {pdfY + k:F2} {centerX - k:F2} {pdfY + radius:F2} {centerX:F2} {pdfY + radius:F2} c"); // Top-left curve
        
        // Choose drawing operation
        if (fillColor != null && strokeColor != null)
            _content.AppendLine("B"); // Fill and stroke
        else if (fillColor != null)
            _content.AppendLine("f"); // Fill only
        else
            _content.AppendLine("S"); // Stroke only
            
        _content.AppendLine("Q"); // Restore graphics state
    }

    public void DrawCircleInches(float centerX, float centerY, float radius, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null)
    {
        DrawCircle(InchesToPoints(centerX), InchesToPoints(centerY), InchesToPoints(radius), lineWidth, strokeColor, fillColor);
    }

    public void DrawCirclePixels(float centerX, float centerY, float radius, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null)
    {
        DrawCircle(PixelsToPoints(centerX), PixelsToPoints(centerY), PixelsToPoints(radius), lineWidth, strokeColor, fillColor);
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
        WriteText(writer, "/Resources <<\n  /Font <<");
        
        // Add font resources - ensure we have at least the default Helvetica
        if (_fonts.Count == 0)
        {
            GetOrAddFont(PdfFont.Helvetica);
        }
        
        for (int i = 1; i <= _fonts.Count; i++)
        {
            WriteText(writer, $" /F{i} {3 + i} 0 R");
        }
        WriteText(writer, " >>\n");
        
        // Add image resources if any
        if (_images.Count > 0)
        {
            WriteText(writer, "  /XObject <<");
            for (int i = 1; i <= _images.Count; i++)
            {
                var imageObjectId = 4 + _fonts.Count + i;
                WriteText(writer, $" /Im{i} {imageObjectId} 0 R");
            }
            WriteText(writer, " >>\n");
        }
        
        var contentObjectId = 4 + _fonts.Count;
        WriteText(writer, $">>\n/Contents {contentObjectId} 0 R\n>>\nendobj\n");
        
        // Font objects (starting from object 4) - all built-in fonts now
        for (int i = 0; i < _fontData.Count; i++)
        {
            var fontObjectId = 4 + i;
            positions.Add(pdfStream.Length);
            WriteText(writer, $"{fontObjectId} 0 obj\n");
            WriteText(writer, _fontData[i].objectData);
            WriteText(writer, "\nendobj\n");
        }
        
        // Content Stream
        positions.Add(pdfStream.Length);
        WriteText(writer, $"{contentObjectId} 0 obj\n<<\n/Length {contentBytes.Length}\n>>\nstream\n");
        writer.Write(contentBytes);
        WriteText(writer, "endstream\nendobj\n");
        
        // Image objects
        for (int i = 0; i < _imageData.Count; i++)
        {
            var imageObjectId = contentObjectId + 1 + i;
            positions.Add(pdfStream.Length);
            WriteText(writer, $"{imageObjectId} 0 obj\n");
            WriteText(writer, _imageData[i].objectData);
            WriteText(writer, "\nstream\n");
            writer.Write(_imageData[i].streamData);
            WriteText(writer, "endstream\nendobj\n");
        }
        
        // Cross-reference table
        var xrefPos = pdfStream.Length;
        var totalObjects = 3 + _fontData.Count + 1 + _imageData.Count + 1; // catalog + pages + page + fonts + content + images + 1 for 0-index
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