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
        var widthPoints = widthInches * 72f;  // PDF pages are always 72 points per inch
        var heightPoints = heightInches * 72f;
        var renderer = new SimplePdfRenderer(widthPoints, heightPoints);
        renderer.DPI = dpi;  // Store DPI only for pixel conversion methods
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

    public float InchesToPoints(float inches) => inches * 72f;
    public float PointsToInches(float points) => points / 72f;
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
        return EstimateTextWidthForFont(text, fontSize, PdfFont.Helvetica);
    }

    private float EstimateTextWidthForFont(string text, float fontSize, PdfFont font)
    {
        // More accurate character width estimation based on font metrics
        // Character widths in thousandths of an em unit (font size)
        var totalWidth = 0f;
        
        foreach (char c in text)
        {
            var charWidth = GetCharacterWidthForFont(c, font);
            totalWidth += charWidth;
        }
        
        // Convert from thousandths of em to points
        return totalWidth * fontSize / 1000f;
    }
    
    private float GetCharacterWidthForFont(char c, PdfFont font)
    {
        // Get base width for Helvetica
        var baseWidth = GetCharacterWidth(c);
        
        // Apply font-specific multipliers for different fonts
        return font.Name.ToLowerInvariant() switch
        {
            "helvetica-bold" or "helvetica-boldoblique" => baseWidth * 1.05f, // Bold is slightly wider
            "times-roman" or "times-bold" or "times-italic" or "times-bolditalic" => baseWidth * 0.95f, // Times is slightly narrower
            "courier" or "courier-bold" or "courier-oblique" or "courier-boldoblique" => 600f, // Courier is monospace
            _ => baseWidth // Helvetica and Helvetica-Oblique
        };
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

    public float DrawMultiLineText(string text, float x, float y, float maxWidth, float fontSize = 12f, 
        PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left, 
        float lineSpacing = 1.2f)
    {
        if (string.IsNullOrEmpty(text)) return 0f;

        var lines = WrapText(text, maxWidth, fontSize, font);
        var lineHeight = fontSize * lineSpacing;
        var currentY = y;

        foreach (var line in lines)
        {
            DrawText(line, x, currentY, fontSize, color, font, alignment);
            currentY += lineHeight;
        }

        return (lines.Count - 1) * lineHeight; // Return total height consumed
    }

    public float DrawMultiLineTextInches(string text, float x, float y, float maxWidthInches, float fontSizeInches = 0.167f,
        PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left,
        float lineSpacing = 1.2f)
    {
        var heightPoints = DrawMultiLineText(text, InchesToPoints(x), InchesToPoints(y), InchesToPoints(maxWidthInches),
            InchesToPoints(fontSizeInches), color, font, alignment, lineSpacing);
        return PointsToInches(heightPoints);
    }

    public float DrawMultiLineTextPixels(string text, float x, float y, float maxWidthPixels, float fontSizePixels = 16f,
        PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left,
        float lineSpacing = 1.2f)
    {
        var heightPoints = DrawMultiLineText(text, PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(maxWidthPixels),
            PixelsToPoints(fontSizePixels), color, font, alignment, lineSpacing);
        return PointsToPixels(heightPoints);
    }

    public float DrawMultiLineTextPoints(string text, float x, float y, float maxWidthPoints, float fontSizePoints = 12f,
        PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left,
        float lineSpacing = 1.2f)
    {
        return DrawMultiLineText(text, x, y, maxWidthPoints, fontSizePoints, color, font, alignment, lineSpacing);
    }

    private float DrawTextBoxMultiLineText(string text, float x, float y, float maxWidth, float fontSize = 12f, 
        PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left, 
        float lineSpacing = 1.2f)
    {
        if (string.IsNullOrEmpty(text)) return 0f;

        var lines = WrapText(text, maxWidth, fontSize, font);
        var lineHeight = fontSize * lineSpacing;
        // Adjust Y position to account for font baseline - use smaller adjustment for small content areas
        var baselineAdjustment = fontSize * 0.7f; // Reduced from 0.8f to work better with small boxes
        var currentY = y + baselineAdjustment;
        var actualFont = font ?? PdfFont.Helvetica;

        foreach (var line in lines)
        {
            // Calculate proper positioning for each line based on alignment
            var lineWidth = EstimateTextWidthForFont(line, fontSize, actualFont);
            var lineX = alignment switch
            {
                TextAlignment.Center => x + (maxWidth - lineWidth) / 2,  // Center within available width
                TextAlignment.Right => x + (maxWidth - lineWidth),      // Right-align within available width
                _ => x // Left edge
            };
            
            // Use left alignment since lineX is already positioned correctly
            DrawText(line, lineX, currentY, fontSize, color, font, TextAlignment.Left);
            currentY += lineHeight;
        }

        return (lines.Count - 1) * lineHeight;
    }

    private float DrawTextBoxMultiLineRichText(RichText richText, float x, float y, float maxWidth, float fontSize = 12f,
        TextAlignment alignment = TextAlignment.Left, float lineSpacing = 1.2f)
    {
        if (richText.Segments.Count == 0) return 0f;

        // Convert rich text to wrapped lines preserving formatting
        var wrappedLines = WrapRichText(richText, maxWidth, fontSize);
        var lineHeight = fontSize * lineSpacing;
        var currentY = y;

        foreach (var line in wrappedLines)
        {
            // Calculate line width for proper alignment
            var lineWidth = CalculateRichTextWidth(line, fontSize);
            var lineX = alignment switch
            {
                TextAlignment.Center => x + (maxWidth - lineWidth) / 2,  // Center within available width
                TextAlignment.Right => x + (maxWidth - lineWidth),      // Right-align within available width
                _ => x // Left edge
            };
            
            // Use left alignment for DrawRichText since lineX is already positioned correctly
            DrawTextBoxRichText(line, lineX, currentY, fontSize);
            currentY += lineHeight;
        }

        return (wrappedLines.Count - 1) * lineHeight;
    }

    private void DrawTextBoxRichText(RichText richText, float x, float y, float fontSize = 12f)
    {
        if (richText.Segments.Count == 0) return;

        var currentX = x; // Start at the specified position (already calculated for alignment)

        foreach (var segment in richText.Segments)
        {
            var font = segment.Font ?? PdfFont.Helvetica;
            DrawText(segment.Text, currentX, y, fontSize, segment.Color, font, TextAlignment.Left);
            
            // Move X position for next segment
            var segmentWidth = EstimateTextWidthForFont(segment.Text, fontSize, font);
            currentX += segmentWidth;
        }
    }

    private List<string> WrapText(string text, float maxWidth, float fontSize, PdfFont? font)
    {
        var lines = new List<string>();
        var paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add("");
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add("");
                continue;
            }

            var currentLine = "";
            
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                var testWidth = EstimateTextWidth(testLine, fontSize);

                if (testWidth <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    // If the word itself is too long, break it
                    if (string.IsNullOrEmpty(currentLine))
                    {
                        // Break long word
                        var brokenWords = BreakLongWord(word, maxWidth, fontSize);
                        lines.AddRange(brokenWords.Take(brokenWords.Count - 1));
                        currentLine = brokenWords.Last();
                    }
                    else
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                        
                        // Check if this single word is too long
                        if (EstimateTextWidth(currentLine, fontSize) > maxWidth)
                        {
                            var brokenWords = BreakLongWord(currentLine, maxWidth, fontSize);
                            lines.AddRange(brokenWords.Take(brokenWords.Count - 1));
                            currentLine = brokenWords.Last();
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
        }

        return lines;
    }

    private List<string> BreakLongWord(string word, float maxWidth, float fontSize)
    {
        var result = new List<string>();
        var currentPart = "";

        foreach (var character in word)
        {
            var testPart = currentPart + character;
            if (EstimateTextWidth(testPart, fontSize) <= maxWidth)
            {
                currentPart = testPart;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentPart))
                {
                    result.Add(currentPart);
                }
                currentPart = character.ToString();
            }
        }

        if (!string.IsNullOrEmpty(currentPart))
        {
            result.Add(currentPart);
        }

        return result.Count == 0 ? new List<string> { word } : result;
    }

    public void DrawRichText(RichText richText, float x, float y, float fontSize = 12f, 
        TextAlignment alignment = TextAlignment.Left)
    {
        if (richText.Segments.Count == 0) return;

        // Calculate total width for alignment
        var totalWidth = CalculateRichTextWidth(richText, fontSize);
        var adjustedX = alignment switch
        {
            TextAlignment.Center => x - (totalWidth / 2),
            TextAlignment.Right => x - totalWidth,
            _ => x // Left alignment (default)
        };

        var currentX = adjustedX;
        var pdfY = ConvertY(y);

        foreach (var segment in richText.Segments)
        {
            if (string.IsNullOrEmpty(segment.Text)) continue;

            var segmentFont = segment.Font ?? PdfFont.Helvetica;
            var segmentColor = segment.Color ?? PdfColor.Black;
            
            // Begin text
            _content.AppendLine("BT");
            
            // Set color
            _content.AppendLine($"{segmentColor.R:F3} {segmentColor.G:F3} {segmentColor.B:F3} rg");
            
            // Set font
            var fontIndex = GetOrAddFont(segmentFont);
            _content.AppendLine($"/F{fontIndex} {fontSize:F2} Tf");
            
            // Set absolute position (Tm matrix)
            _content.AppendLine($"1 0 0 1 {currentX:F2} {pdfY:F2} Tm");
            
            // Show text
            _content.AppendLine($"({EscapeText(segment.Text)}) Tj");
            
            // End text
            _content.AppendLine("ET");

            // Calculate segment width for positioning next segment (considering the font)
            var segmentWidth = EstimateTextWidthForFont(segment.Text, fontSize, segmentFont);

            // Draw underline if needed
            if (segment.IsUnderlined)
            {
                var underlineY = y + fontSize * 0.1f; // Slightly below baseline
                var underlineThickness = fontSize * 0.05f;
                DrawLine(currentX, underlineY, currentX + segmentWidth, underlineY, underlineThickness, segmentColor);
            }

            // Draw strikethrough if needed  
            if (segment.IsStrikethrough)
            {
                var strikeY = y - fontSize * 0.3f; // Through middle of text
                var strikeThickness = fontSize * 0.05f;
                DrawLine(currentX, strikeY, currentX + segmentWidth, strikeY, strikeThickness, segmentColor);
            }

            currentX += segmentWidth;
        }
    }

    public void DrawRichTextInches(RichText richText, float x, float y, float fontSizeInches = 0.167f,
        TextAlignment alignment = TextAlignment.Left)
    {
        DrawRichText(richText, InchesToPoints(x), InchesToPoints(y), InchesToPoints(fontSizeInches), alignment);
    }

    public void DrawRichTextPixels(RichText richText, float x, float y, float fontSizePixels = 16f,
        TextAlignment alignment = TextAlignment.Left)
    {
        DrawRichText(richText, PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(fontSizePixels), alignment);
    }

    public void DrawRichTextPoints(RichText richText, float x, float y, float fontSizePoints = 12f,
        TextAlignment alignment = TextAlignment.Left)
    {
        DrawRichText(richText, x, y, fontSizePoints, alignment);
    }

    public float DrawMultiLineRichText(RichText richText, float x, float y, float maxWidth, float fontSize = 12f,
        TextAlignment alignment = TextAlignment.Left, float lineSpacing = 1.2f)
    {
        if (richText.Segments.Count == 0) return 0f;

        // Convert rich text to wrapped lines preserving formatting
        var wrappedLines = WrapRichText(richText, maxWidth, fontSize);
        var lineHeight = fontSize * lineSpacing;
        var currentY = y;

        foreach (var line in wrappedLines)
        {
            DrawRichText(line, x, currentY, fontSize, alignment);
            currentY += lineHeight;
        }

        return (wrappedLines.Count - 1) * lineHeight;
    }

    public float DrawMultiLineRichTextInches(RichText richText, float x, float y, float maxWidthInches, float fontSizeInches = 0.167f,
        TextAlignment alignment = TextAlignment.Left, float lineSpacing = 1.2f)
    {
        var heightPoints = DrawMultiLineRichText(richText, InchesToPoints(x), InchesToPoints(y), InchesToPoints(maxWidthInches),
            InchesToPoints(fontSizeInches), alignment, lineSpacing);
        return PointsToInches(heightPoints);
    }

    public float DrawMultiLineRichTextPixels(RichText richText, float x, float y, float maxWidthPixels, float fontSizePixels = 16f,
        TextAlignment alignment = TextAlignment.Left, float lineSpacing = 1.2f)
    {
        var heightPoints = DrawMultiLineRichText(richText, PixelsToPoints(x), PixelsToPoints(y), PixelsToPoints(maxWidthPixels),
            PixelsToPoints(fontSizePixels), alignment, lineSpacing);
        return PointsToPixels(heightPoints);
    }

    public float DrawMultiLineRichTextPoints(RichText richText, float x, float y, float maxWidthPoints, float fontSizePoints = 12f,
        TextAlignment alignment = TextAlignment.Left, float lineSpacing = 1.2f)
    {
        return DrawMultiLineRichText(richText, x, y, maxWidthPoints, fontSizePoints, alignment, lineSpacing);
    }

    private float CalculateRichTextWidth(RichText richText, float fontSize)
    {
        var totalWidth = 0f;
        foreach (var segment in richText.Segments)
        {
            var segmentFont = segment.Font ?? PdfFont.Helvetica;
            totalWidth += EstimateTextWidthForFont(segment.Text, fontSize, segmentFont);
        }
        return totalWidth;
    }

    private List<RichText> WrapRichText(RichText richText, float maxWidth, float fontSize)
    {
        var lines = new List<RichText>();
        var currentLine = new RichText();
        var currentLineWidth = 0f;

        foreach (var segment in richText.Segments)
        {
            // Handle line breaks in segment text
            var textParts = segment.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            
            for (int i = 0; i < textParts.Length; i++)
            {
                if (i > 0)
                {
                    // New line encountered
                    lines.Add(currentLine);
                    currentLine = new RichText();
                    currentLineWidth = 0f;
                }

                var textPart = textParts[i];
                if (string.IsNullOrEmpty(textPart)) continue;

                var words = textPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var word in words)
                {
                    var wordSegment = new RichTextSegment(word, segment.Font, segment.Color, segment.IsUnderlined, segment.IsStrikethrough);
                    var segmentFont = segment.Font ?? PdfFont.Helvetica;
                    var wordWidth = EstimateTextWidthForFont(word, fontSize, segmentFont);
                    var spaceWidth = EstimateTextWidthForFont(" ", fontSize, segmentFont);
                    
                    // Check if adding this word would exceed the line width
                    var testWidth = currentLineWidth + (currentLineWidth > 0 ? spaceWidth : 0) + wordWidth;
                    
                    if (testWidth <= maxWidth || currentLineWidth == 0)
                    {
                        // Add space if not at beginning of line
                        if (currentLineWidth > 0)
                        {
                            currentLine.AddText(" ", segment.Font, segment.Color, segment.IsUnderlined, segment.IsStrikethrough);
                            currentLineWidth += spaceWidth;
                        }
                        
                        currentLine.Segments.Add(wordSegment);
                        currentLineWidth += wordWidth;
                    }
                    else
                    {
                        // Start new line
                        lines.Add(currentLine);
                        currentLine = new RichText();
                        currentLine.Segments.Add(wordSegment);
                        currentLineWidth = wordWidth;
                    }
                }
            }
        }

        // Add the last line if it has content
        if (currentLine.Segments.Count > 0)
        {
            lines.Add(currentLine);
        }

        return lines.Count == 0 ? new List<RichText> { new RichText() } : lines;
    }

    public TextBox DrawTextBox(TextBox textBox, string text, float fontSize = 12f, PdfColor? textColor = null, PdfFont? font = null)
    {
        // Create a copy to avoid modifying the original
        var box = new TextBox(textBox.X, textBox.Y, textBox.Width, textBox.Height)
        {
            MinWidth = textBox.MinWidth,
            MaxWidth = textBox.MaxWidth,
            MinHeight = textBox.MinHeight,
            MaxHeight = textBox.MaxHeight,
            Padding = textBox.Padding,
            PaddingLeft = textBox.PaddingLeft,
            PaddingRight = textBox.PaddingRight,
            PaddingTop = textBox.PaddingTop,
            PaddingBottom = textBox.PaddingBottom,
            Sizing = textBox.Sizing,
            Overflow = textBox.Overflow,
            Alignment = textBox.Alignment,
            LineSpacing = textBox.LineSpacing,
            BackgroundColor = textBox.BackgroundColor,
            BorderColor = textBox.BorderColor,
            BorderWidth = textBox.BorderWidth,
            BorderStyle = textBox.BorderStyle
        };

        // Calculate required dimensions based on sizing mode
        CalculateTextBoxDimensions(box, text, fontSize, font);

        // Draw background if specified
        if (box.BackgroundColor != null)
        {
            DrawRectangle(box.X, box.Y, box.Width, box.Height, 0f, null, box.BackgroundColor);
        }

        // Draw border if specified
        if (box.BorderColor != null)
        {
            DrawRectangle(box.X, box.Y, box.Width, box.Height, box.BorderWidth, box.BorderColor, null, box.BorderStyle);
        }

        // Draw text content
        var contentX = box.GetContentX();
        var contentY = box.GetContentY();
        var contentWidth = box.GetContentWidth();
        var contentHeight = box.GetContentHeight();

        if (box.Overflow == TextBoxOverflow.Clip)
        {
            // Use clipping to ensure text doesn't exceed bounds
            SetRectangularClip(contentX, contentY, contentWidth, contentHeight);
        }

        if (box.Overflow == TextBoxOverflow.Wrap || box.Sizing == TextBoxSizing.AutoHeight || box.Sizing == TextBoxSizing.AutoBoth)
        {
            
            // Handle multi-line text with proper text box alignment
            DrawTextBoxMultiLineText(text, contentX, contentY, contentWidth, fontSize, textColor, font, box.Alignment, box.LineSpacing);
        }
        else
        {
            
            // Calculate text width and position it correctly within the content area
            var actualFont = font ?? PdfFont.Helvetica;
            var textWidth = EstimateTextWidthForFont(text, fontSize, actualFont);
            var textX = box.Alignment switch
            {
                TextAlignment.Center => contentX + (contentWidth - textWidth) / 2,  // Center the text within content area
                TextAlignment.Right => contentX + (contentWidth - textWidth),      // Right-align within content area  
                _ => contentX // Left edge of content area
            };
            
            // Use left alignment since textX is already positioned correctly
            DrawText(text, textX, contentY + fontSize * 0.7f, fontSize, textColor, font, TextAlignment.Left);
        }

        if (box.Overflow == TextBoxOverflow.Clip)
        {
            RestoreGraphicsState();
        }

        return box;
    }

    public TextBox DrawTextBoxInches(TextBox textBox, string text, float fontSizeInches = 0.167f, PdfColor? textColor = null, PdfFont? font = null)
    {
        // Convert textBox coordinates to points
        var textBoxPoints = new TextBox(
            InchesToPoints(textBox.X), 
            InchesToPoints(textBox.Y), 
            InchesToPoints(textBox.Width), 
            InchesToPoints(textBox.Height))
        {
            MinWidth = InchesToPoints(textBox.MinWidth),
            MaxWidth = textBox.MaxWidth == float.MaxValue ? float.MaxValue : InchesToPoints(textBox.MaxWidth),
            MinHeight = InchesToPoints(textBox.MinHeight),
            MaxHeight = textBox.MaxHeight == float.MaxValue ? float.MaxValue : InchesToPoints(textBox.MaxHeight),
            Padding = InchesToPoints(textBox.Padding),
            PaddingLeft = InchesToPoints(textBox.PaddingLeft),
            PaddingRight = InchesToPoints(textBox.PaddingRight),
            PaddingTop = InchesToPoints(textBox.PaddingTop),
            PaddingBottom = InchesToPoints(textBox.PaddingBottom),
            Sizing = textBox.Sizing,
            Overflow = textBox.Overflow,
            Alignment = textBox.Alignment,
            LineSpacing = textBox.LineSpacing,
            BackgroundColor = textBox.BackgroundColor,
            BorderColor = textBox.BorderColor,
            BorderWidth = textBox.BorderWidth,
            BorderStyle = textBox.BorderStyle
        };

        var result = DrawTextBox(textBoxPoints, text, InchesToPoints(fontSizeInches), textColor, font);
        
        // Convert result back to inches
        result.X = PointsToInches(result.X);
        result.Y = PointsToInches(result.Y);
        result.Width = PointsToInches(result.Width);
        result.Height = PointsToInches(result.Height);
        result.MinWidth = PointsToInches(result.MinWidth);
        result.MaxWidth = result.MaxWidth == float.MaxValue ? float.MaxValue : PointsToInches(result.MaxWidth);
        result.MinHeight = PointsToInches(result.MinHeight);
        result.MaxHeight = result.MaxHeight == float.MaxValue ? float.MaxValue : PointsToInches(result.MaxHeight);
        result.Padding = PointsToInches(result.Padding);
        result.PaddingLeft = PointsToInches(result.PaddingLeft);
        result.PaddingRight = PointsToInches(result.PaddingRight);
        result.PaddingTop = PointsToInches(result.PaddingTop);
        result.PaddingBottom = PointsToInches(result.PaddingBottom);
        
        return result;
    }

    public TextBox DrawTextBoxPixels(TextBox textBox, string text, float fontSizePixels = 16f, PdfColor? textColor = null, PdfFont? font = null)
    {
        // Convert textBox coordinates to points
        var textBoxPoints = new TextBox(
            PixelsToPoints(textBox.X), 
            PixelsToPoints(textBox.Y), 
            PixelsToPoints(textBox.Width), 
            PixelsToPoints(textBox.Height))
        {
            MinWidth = PixelsToPoints(textBox.MinWidth),
            MaxWidth = textBox.MaxWidth == float.MaxValue ? float.MaxValue : PixelsToPoints(textBox.MaxWidth),
            MinHeight = PixelsToPoints(textBox.MinHeight),
            MaxHeight = textBox.MaxHeight == float.MaxValue ? float.MaxValue : PixelsToPoints(textBox.MaxHeight),
            Padding = PixelsToPoints(textBox.Padding),
            PaddingLeft = PixelsToPoints(textBox.PaddingLeft),
            PaddingRight = PixelsToPoints(textBox.PaddingRight),
            PaddingTop = PixelsToPoints(textBox.PaddingTop),
            PaddingBottom = PixelsToPoints(textBox.PaddingBottom),
            Sizing = textBox.Sizing,
            Overflow = textBox.Overflow,
            Alignment = textBox.Alignment,
            LineSpacing = textBox.LineSpacing,
            BackgroundColor = textBox.BackgroundColor,
            BorderColor = textBox.BorderColor,
            BorderWidth = textBox.BorderWidth,
            BorderStyle = textBox.BorderStyle
        };

        var result = DrawTextBox(textBoxPoints, text, PixelsToPoints(fontSizePixels), textColor, font);
        
        // Convert result back to pixels
        result.X = PointsToPixels(result.X);
        result.Y = PointsToPixels(result.Y);
        result.Width = PointsToPixels(result.Width);
        result.Height = PointsToPixels(result.Height);
        result.MinWidth = PointsToPixels(result.MinWidth);
        result.MaxWidth = result.MaxWidth == float.MaxValue ? float.MaxValue : PointsToPixels(result.MaxWidth);
        result.MinHeight = PointsToPixels(result.MinHeight);
        result.MaxHeight = result.MaxHeight == float.MaxValue ? float.MaxValue : PointsToPixels(result.MaxHeight);
        result.Padding = PointsToPixels(result.Padding);
        result.PaddingLeft = PointsToPixels(result.PaddingLeft);
        result.PaddingRight = PointsToPixels(result.PaddingRight);
        result.PaddingTop = PointsToPixels(result.PaddingTop);
        result.PaddingBottom = PointsToPixels(result.PaddingBottom);
        
        return result;
    }

    public TextBox DrawRichTextBox(TextBox textBox, RichText richText, float fontSize = 12f)
    {
        // Create a copy to avoid modifying the original
        var box = new TextBox(textBox.X, textBox.Y, textBox.Width, textBox.Height)
        {
            MinWidth = textBox.MinWidth,
            MaxWidth = textBox.MaxWidth,
            MinHeight = textBox.MinHeight,
            MaxHeight = textBox.MaxHeight,
            Padding = textBox.Padding,
            PaddingLeft = textBox.PaddingLeft,
            PaddingRight = textBox.PaddingRight,
            PaddingTop = textBox.PaddingTop,
            PaddingBottom = textBox.PaddingBottom,
            Sizing = textBox.Sizing,
            Overflow = textBox.Overflow,
            Alignment = textBox.Alignment,
            LineSpacing = textBox.LineSpacing,
            BackgroundColor = textBox.BackgroundColor,
            BorderColor = textBox.BorderColor,
            BorderWidth = textBox.BorderWidth,
            BorderStyle = textBox.BorderStyle
        };

        // Calculate required dimensions for rich text
        CalculateRichTextBoxDimensions(box, richText, fontSize);

        // Draw background if specified
        if (box.BackgroundColor != null)
        {
            DrawRectangle(box.X, box.Y, box.Width, box.Height, 0f, null, box.BackgroundColor);
        }

        // Draw border if specified
        if (box.BorderColor != null)
        {
            DrawRectangle(box.X, box.Y, box.Width, box.Height, box.BorderWidth, box.BorderColor, null, box.BorderStyle);
        }

        // Draw rich text content
        var contentX = box.GetContentX();
        var contentY = box.GetContentY();
        var contentWidth = box.GetContentWidth();
        var contentHeight = box.GetContentHeight();

        if (box.Overflow == TextBoxOverflow.Clip)
        {
            SetRectangularClip(contentX, contentY, contentWidth, contentHeight);
        }

        if (box.Overflow == TextBoxOverflow.Wrap || box.Sizing == TextBoxSizing.AutoHeight || box.Sizing == TextBoxSizing.AutoBoth)
        {
            DrawTextBoxMultiLineRichText(richText, contentX, contentY, contentWidth, fontSize, box.Alignment, box.LineSpacing);
        }
        else
        {
            // Single line rich text - adjust X coordinate for alignment
            var textX = box.Alignment switch
            {
                TextAlignment.Center => contentX + (contentWidth / 2),
                TextAlignment.Right => contentX + contentWidth,
                _ => contentX // Left alignment
            };
            
            DrawRichText(richText, textX, contentY, fontSize, box.Alignment);
        }

        if (box.Overflow == TextBoxOverflow.Clip)
        {
            RestoreGraphicsState();
        }

        return box;
    }

    public TextBox DrawRichTextBoxInches(TextBox textBoxInches, RichText richText, float fontSizeInches = 0.167f)
    {
        // Convert textBox from inches to points
        var textBoxPoints = new TextBox(
            InchesToPoints(textBoxInches.X), 
            InchesToPoints(textBoxInches.Y), 
            InchesToPoints(textBoxInches.Width), 
            InchesToPoints(textBoxInches.Height))
        {
            MinWidth = InchesToPoints(textBoxInches.MinWidth),
            MaxWidth = textBoxInches.MaxWidth == float.MaxValue ? float.MaxValue : InchesToPoints(textBoxInches.MaxWidth),
            MinHeight = InchesToPoints(textBoxInches.MinHeight),
            MaxHeight = textBoxInches.MaxHeight == float.MaxValue ? float.MaxValue : InchesToPoints(textBoxInches.MaxHeight),
            Padding = InchesToPoints(textBoxInches.Padding),
            PaddingLeft = InchesToPoints(textBoxInches.PaddingLeft),
            PaddingRight = InchesToPoints(textBoxInches.PaddingRight),
            PaddingTop = InchesToPoints(textBoxInches.PaddingTop),
            PaddingBottom = InchesToPoints(textBoxInches.PaddingBottom),
            Sizing = textBoxInches.Sizing,
            Overflow = textBoxInches.Overflow,
            Alignment = textBoxInches.Alignment,
            LineSpacing = textBoxInches.LineSpacing,
            BackgroundColor = textBoxInches.BackgroundColor,
            BorderColor = textBoxInches.BorderColor,
            BorderWidth = textBoxInches.BorderWidth,
            BorderStyle = textBoxInches.BorderStyle
        };

        var result = DrawRichTextBox(textBoxPoints, richText, InchesToPoints(fontSizeInches));
        
        // Convert result back to inches
        result.X = PointsToInches(result.X);
        result.Y = PointsToInches(result.Y);
        result.Width = PointsToInches(result.Width);
        result.Height = PointsToInches(result.Height);
        result.MinWidth = PointsToInches(result.MinWidth);
        result.MaxWidth = result.MaxWidth == float.MaxValue ? float.MaxValue : PointsToInches(result.MaxWidth);
        result.MinHeight = PointsToInches(result.MinHeight);
        result.MaxHeight = result.MaxHeight == float.MaxValue ? float.MaxValue : PointsToInches(result.MaxHeight);
        result.Padding = PointsToInches(result.Padding);
        result.PaddingLeft = PointsToInches(result.PaddingLeft);
        result.PaddingRight = PointsToInches(result.PaddingRight);
        result.PaddingTop = PointsToInches(result.PaddingTop);
        result.PaddingBottom = PointsToInches(result.PaddingBottom);
        
        return result;
    }

    private void CalculateTextBoxDimensions(TextBox box, string text, float fontSize, PdfFont? font)
    {
        var actualFont = font ?? PdfFont.Helvetica;
        
        if (box.Sizing == TextBoxSizing.Fixed)
            return;

        var contentWidth = box.GetContentWidth();
        var requiredWidth = EstimateTextWidthForFont(text, fontSize, actualFont);
        var requiredHeight = fontSize * box.LineSpacing;

        if (box.Sizing == TextBoxSizing.AutoWidth || box.Sizing == TextBoxSizing.AutoBoth)
        {
            // For AutoWidth, always expand the width to fit the content (unless constrained by MaxWidth)
            var newWidth = Math.Max(box.MinWidth, Math.Min(box.MaxWidth, 
                requiredWidth + box.GetEffectivePaddingLeft() + box.GetEffectivePaddingRight()));
            box.Width = newWidth;
            requiredHeight = fontSize * box.LineSpacing;
        }

        if (box.Sizing == TextBoxSizing.AutoHeight || box.Sizing == TextBoxSizing.AutoBoth)
        {
            if (box.Overflow == TextBoxOverflow.Wrap)
            {
                var lines = WrapText(text, box.GetContentWidth(), fontSize, actualFont);
                requiredHeight = lines.Count * fontSize * box.LineSpacing;
            }
            
            var newHeight = Math.Max(box.MinHeight, Math.Min(box.MaxHeight, 
                requiredHeight + box.GetEffectivePaddingTop() + box.GetEffectivePaddingBottom()));
            box.Height = newHeight;
        }
    }

    private void CalculateRichTextBoxDimensions(TextBox box, RichText richText, float fontSize)
    {
        if (box.Sizing == TextBoxSizing.Fixed)
            return;

        var contentWidth = box.GetContentWidth();
        var requiredWidth = CalculateRichTextWidth(richText, fontSize);
        var requiredHeight = fontSize * box.LineSpacing;

        if (box.Sizing == TextBoxSizing.AutoWidth || box.Sizing == TextBoxSizing.AutoBoth)
        {
            // For AutoWidth, always expand the width to fit the content (unless constrained by MaxWidth)
            var newWidth = Math.Max(box.MinWidth, Math.Min(box.MaxWidth, 
                requiredWidth + box.GetEffectivePaddingLeft() + box.GetEffectivePaddingRight()));
            box.Width = newWidth;
            requiredHeight = fontSize * box.LineSpacing;
        }

        if (box.Sizing == TextBoxSizing.AutoHeight || box.Sizing == TextBoxSizing.AutoBoth)
        {
            if (box.Overflow == TextBoxOverflow.Wrap)
            {
                var lines = WrapRichText(richText, box.GetContentWidth(), fontSize);
                requiredHeight = lines.Count * fontSize * box.LineSpacing;
            }
            
            var newHeight = Math.Max(box.MinHeight, Math.Min(box.MaxHeight, 
                requiredHeight + box.GetEffectivePaddingTop() + box.GetEffectivePaddingBottom()));
            box.Height = newHeight;
        }
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