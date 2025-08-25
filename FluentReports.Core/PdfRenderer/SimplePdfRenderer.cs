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

    public void DrawLine(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null, LineStyle? style = null)
    {
        var pdfY1 = ConvertY(y1);
        var pdfY2 = ConvertY(y2);
        
        _content.AppendLine("q");
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} RG");
        }
        
        _content.AppendLine($"{lineWidth:F2} w");
        
        // Apply line style
        if (style != null)
        {
            // Set line cap
            _content.AppendLine($"{(int)style.Cap} J");
            
            // Set line join  
            _content.AppendLine($"{(int)style.Join} j");
            
            // Set dash pattern
            if (style.DashArray != null && style.DashArray.Length > 0)
            {
                var dashPattern = string.Join(" ", style.DashArray.Select(d => d.ToString("F2")));
                _content.AppendLine($"[{dashPattern}] 0 d");
            }
        }
        
        _content.AppendLine($"{x1:F2} {pdfY1:F2} m");
        _content.AppendLine($"{x2:F2} {pdfY2:F2} l");
        _content.AppendLine("S");
        _content.AppendLine("Q");
    }

    public void DrawLineInches(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null, LineStyle? style = null)
    {
        DrawLine(InchesToPoints(x1), InchesToPoints(y1), InchesToPoints(x2), InchesToPoints(y2), lineWidth, color, style);
    }

    public void DrawLinePixels(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null, LineStyle? style = null)
    {
        DrawLine(PixelsToPoints(x1), PixelsToPoints(y1), PixelsToPoints(x2), PixelsToPoints(y2), lineWidth, color, style);
    }

    public void DrawText(string text, float x, float y, float fontSize = 12f, PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        // Use Helvetica as default font if none specified
        font ??= PdfFont.Helvetica;
        var fontIndex = GetOrAddFont(font);
        
        // Calculate text width for alignment
        var textWidth = EstimateTextWidth(text, fontSize);
        var adjustedX = alignment switch
        {
            TextAlignment.Center => x - (textWidth / 2),
            TextAlignment.Right => x - textWidth,
            TextAlignment.Justified => x, // For now, treat justified as left-aligned
            _ => x // Left alignment (default)
        };
        
        var pdfY = ConvertY(y);
        
        _content.AppendLine("BT");
        
        if (color != null)
        {
            _content.AppendLine($"{color.R:F3} {color.G:F3} {color.B:F3} rg");
        }
        
        _content.AppendLine($"/F{fontIndex} {fontSize:F2} Tf");
        _content.AppendLine($"{adjustedX:F2} {pdfY:F2} Td");
        _content.AppendLine($"({EscapeText(text)}) Tj");
        _content.AppendLine("ET");
    }

    private float EstimateTextWidth(string text, float fontSize)
    {
        // More accurate character width estimation based on Helvetica metrics
        // Character widths in thousandths of an em unit (font size)
        var totalWidth = 0f;
        
        foreach (char c in text)
        {
            var charWidth = GetCharacterWidth(c);
            totalWidth += charWidth;
        }
        
        // Convert from thousandths of em to points
        return totalWidth * fontSize / 1000f;
    }
    
    private float GetCharacterWidth(char c)
    {
        // Helvetica character widths in thousandths of em unit
        // These are approximate values for common characters
        return c switch
        {
            ' ' => 278f,  // space
            '!' => 278f,
            '"' => 355f,
            '#' => 556f,
            '$' => 556f,
            '%' => 889f,
            '&' => 667f,
            '\'' => 191f,
            '(' => 333f,
            ')' => 333f,
            '*' => 389f,
            '+' => 584f,
            ',' => 278f,
            '-' => 333f,
            '.' => 278f,
            '/' => 278f,
            '0' => 556f,
            '1' => 556f,
            '2' => 556f,
            '3' => 556f,
            '4' => 556f,
            '5' => 556f,
            '6' => 556f,
            '7' => 556f,
            '8' => 556f,
            '9' => 556f,
            ':' => 278f,
            ';' => 278f,
            '<' => 584f,
            '=' => 584f,
            '>' => 584f,
            '?' => 556f,
            '@' => 1015f,
            'A' => 667f,
            'B' => 667f,
            'C' => 722f,
            'D' => 722f,
            'E' => 667f,
            'F' => 611f,
            'G' => 778f,
            'H' => 722f,
            'I' => 278f,
            'J' => 500f,
            'K' => 667f,
            'L' => 556f,
            'M' => 833f,
            'N' => 722f,
            'O' => 778f,
            'P' => 667f,
            'Q' => 778f,
            'R' => 722f,
            'S' => 667f,
            'T' => 611f,
            'U' => 722f,
            'V' => 667f,
            'W' => 944f,
            'X' => 667f,
            'Y' => 667f,
            'Z' => 611f,
            '[' => 278f,
            '\\' => 278f,
            ']' => 278f,
            '^' => 469f,
            '_' => 556f,
            '`' => 333f,
            'a' => 556f,
            'b' => 556f,
            'c' => 500f,
            'd' => 556f,
            'e' => 556f,
            'f' => 278f,
            'g' => 556f,
            'h' => 556f,
            'i' => 222f,
            'j' => 222f,
            'k' => 500f,
            'l' => 222f,
            'm' => 833f,
            'n' => 556f,
            'o' => 556f,
            'p' => 556f,
            'q' => 556f,
            'r' => 333f,
            's' => 500f,
            't' => 278f,
            'u' => 556f,
            'v' => 500f,
            'w' => 722f,
            'x' => 500f,
            'y' => 500f,
            'z' => 500f,
            '{' => 334f,
            '|' => 260f,
            '}' => 334f,
            '~' => 584f,
            _ => 556f  // Default width for unknown characters
        };
    }

    public float MeasureTextWidth(string text, float fontSize)
    {
        return EstimateTextWidth(text, fontSize);
    }
    
    public float MeasureTextWidthInches(string text, float fontSizeInches)
    {
        return PointsToInches(EstimateTextWidth(text, InchesToPoints(fontSizeInches)));
    }
    
    public float MeasureTextWidthPixels(string text, float fontSizePixels)
    {
        return PointsToPixels(EstimateTextWidth(text, PixelsToPoints(fontSizePixels)));
    }

    public void DrawTextInches(string text, float x, float y, float fontSizeInches = 0.167f, PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left)
    {
        DrawText(text, InchesToPoints(x), InchesToPoints(y), InchesToPoints(fontSizeInches), color, font, alignment);
    }

    public void DrawTextPixels(string text, float x, float y, float fontSizePixels = 16f, PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left)
    {
        DrawText(text, PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(fontSizePixels), color, font, alignment);
    }

    public void DrawTextPoints(string text, float x, float y, float fontSizePoints = 12f, PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left)
    {
        DrawText(text, x, y, fontSizePoints, color, font, alignment);
    }

    public void DrawRectangle(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? strokeStyle = null)
    {
        var pdfY = ConvertY(y + height); // Convert to PDF coordinates and adjust for rectangle height
        
        _content.AppendLine("q"); // Save graphics state
        
        // Set stroke properties if provided
        if (strokeColor != null)
        {
            _content.AppendLine($"{strokeColor.R:F3} {strokeColor.G:F3} {strokeColor.B:F3} RG");
            _content.AppendLine($"{lineWidth:F2} w");
            
            // Apply stroke style
            if (strokeStyle != null)
            {
                // Set line cap
                _content.AppendLine($"{(int)strokeStyle.Cap} J");
                
                // Set line join  
                _content.AppendLine($"{(int)strokeStyle.Join} j");
                
                // Set dash pattern
                if (strokeStyle.DashArray != null && strokeStyle.DashArray.Length > 0)
                {
                    var dashPattern = string.Join(" ", strokeStyle.DashArray.Select(d => d.ToString("F2")));
                    _content.AppendLine($"[{dashPattern}] 0 d");
                }
            }
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

    public void DrawRectangleInches(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? strokeStyle = null)
    {
        DrawRectangle(InchesToPoints(x), InchesToPoints(y), InchesToPoints(width), InchesToPoints(height), lineWidth, strokeColor, fillColor, strokeStyle);
    }

    public void DrawRectanglePixels(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? strokeStyle = null)
    {
        DrawRectangle(PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(width), PixelsToPoints(height), lineWidth, strokeColor, fillColor, strokeStyle);
    }

    public void DrawCircle(float centerX, float centerY, float radius, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? strokeStyle = null)
    {
        var pdfY = ConvertY(centerY); // Convert center Y to PDF coordinates
        
        _content.AppendLine("q"); // Save graphics state
        
        // Set stroke properties if provided
        if (strokeColor != null)
        {
            _content.AppendLine($"{strokeColor.R:F3} {strokeColor.G:F3} {strokeColor.B:F3} RG");
            _content.AppendLine($"{lineWidth:F2} w");
            
            // Apply stroke style
            if (strokeStyle != null)
            {
                // Set line cap
                _content.AppendLine($"{(int)strokeStyle.Cap} J");
                
                // Set line join  
                _content.AppendLine($"{(int)strokeStyle.Join} j");
                
                // Set dash pattern
                if (strokeStyle.DashArray != null && strokeStyle.DashArray.Length > 0)
                {
                    var dashPattern = string.Join(" ", strokeStyle.DashArray.Select(d => d.ToString("F2")));
                    _content.AppendLine($"[{dashPattern}] 0 d");
                }
            }
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

    public void DrawCircleInches(float centerX, float centerY, float radius, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? strokeStyle = null)
    {
        DrawCircle(InchesToPoints(centerX), InchesToPoints(centerY), InchesToPoints(radius), lineWidth, strokeColor, fillColor, strokeStyle);
    }

    public void DrawCirclePixels(float centerX, float centerY, float radius, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? strokeStyle = null)
    {
        DrawCircle(PixelsToPoints(centerX), PixelsToPoints(centerY), PixelsToPoints(radius), lineWidth, strokeColor, fillColor, strokeStyle);
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

    public void SetClippingRegion(ClippingRegion clippingRegion)
    {
        _content.AppendLine("q"); // Save graphics state
        clippingRegion.ApplyToContent(_content, ConvertY);
    }

    public void SetRectangularClip(float x, float y, float width, float height)
    {
        SetClippingRegion(new RectangularClip(x, y, width, height));
    }

    public void SetRectangularClipInches(float x, float y, float width, float height)
    {
        SetRectangularClip(InchesToPoints(x), InchesToPoints(y), InchesToPoints(width), InchesToPoints(height));
    }

    public void SetRectangularClipPixels(float x, float y, float width, float height)
    {
        SetRectangularClip(PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(width), PixelsToPoints(height));
    }

    public void SetCircularClip(float centerX, float centerY, float radius)
    {
        SetClippingRegion(new CircularClip(centerX, centerY, radius));
    }

    public void SetCircularClipInches(float centerX, float centerY, float radius)
    {
        SetCircularClip(InchesToPoints(centerX), InchesToPoints(centerY), InchesToPoints(radius));
    }

    public void SetCircularClipPixels(float centerX, float centerY, float radius)
    {
        SetCircularClip(PixelsToPoints(centerX), PixelsToPoints(centerY), PixelsToPoints(radius));
    }

    public void RestoreGraphicsState()
    {
        _content.AppendLine("Q"); // Restore graphics state (removes clipping)
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