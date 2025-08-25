namespace FluentReports.Core.PdfRenderer;

public class PdfFont
{
    public string Name { get; }
    public string? FilePath { get; }
    public bool IsEmbedded { get; }
    public PdfFontStyle Style { get; }

    private PdfFont(string name, string? filePath, bool isEmbedded, PdfFontStyle style = PdfFontStyle.Regular)
    {
        Name = name;
        FilePath = filePath;
        IsEmbedded = isEmbedded;
        Style = style;
    }

    // Built-in PDF fonts (no embedding needed)
    public static PdfFont Helvetica => new("Helvetica", null, false);
    public static PdfFont HelveticaBold => new("Helvetica-Bold", null, false, PdfFontStyle.Bold);
    public static PdfFont HelveticaOblique => new("Helvetica-Oblique", null, false, PdfFontStyle.Italic);
    public static PdfFont HelveticaBoldOblique => new("Helvetica-BoldOblique", null, false, PdfFontStyle.BoldItalic);
    
    public static PdfFont TimesRoman => new("Times-Roman", null, false);
    public static PdfFont TimesBold => new("Times-Bold", null, false, PdfFontStyle.Bold);
    public static PdfFont TimesItalic => new("Times-Italic", null, false, PdfFontStyle.Italic);
    public static PdfFont TimesBoldItalic => new("Times-BoldItalic", null, false, PdfFontStyle.BoldItalic);
    
    public static PdfFont Courier => new("Courier", null, false);
    public static PdfFont CourierBold => new("Courier-Bold", null, false, PdfFontStyle.Bold);
    public static PdfFont CourierOblique => new("Courier-Oblique", null, false, PdfFontStyle.Italic);
    public static PdfFont CourierBoldOblique => new("Courier-BoldOblique", null, false, PdfFontStyle.BoldItalic);

    // Create embedded font from file
    public static PdfFont FromFile(string fontPath, PdfFontStyle style = PdfFontStyle.Regular)
    {
        if (!File.Exists(fontPath))
            throw new FileNotFoundException($"Font file not found: {fontPath}");

        var fontName = Path.GetFileNameWithoutExtension(fontPath);
        return new PdfFont(fontName, fontPath, true, style);
    }

    // Create embedded font from system font name (searches Windows fonts folder)
    public static PdfFont FromSystemFont(string fontName, PdfFontStyle style = PdfFontStyle.Regular)
    {
        var windowsFontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var possibleFiles = new[]
        {
            $"{fontName.Replace(" ", "")}.ttf",
            $"{fontName.Replace(" ", "")}".ToLowerInvariant() + ".ttf",
            $"{fontName}.ttf",
            $"{fontName.ToLowerInvariant()}.ttf"
        };

        foreach (var fileName in possibleFiles)
        {
            var fullPath = Path.Combine(windowsFontsPath, fileName);
            if (File.Exists(fullPath))
            {
                return new PdfFont(fontName, fullPath, true, style);
            }
        }

        throw new FileNotFoundException($"System font not found: {fontName}");
    }
}

[Flags]
public enum PdfFontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    BoldItalic = Bold | Italic
}