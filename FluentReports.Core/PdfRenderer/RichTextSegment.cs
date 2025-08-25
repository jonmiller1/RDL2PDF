namespace FluentReports.Core.PdfRenderer;

public class RichTextSegment
{
    public string Text { get; set; } = "";
    public PdfFont? Font { get; set; }
    public PdfColor? Color { get; set; }
    public bool IsUnderlined { get; set; } = false;
    public bool IsStrikethrough { get; set; } = false;

    public RichTextSegment(string text, PdfFont? font = null, PdfColor? color = null, bool isUnderlined = false, bool isStrikethrough = false)
    {
        Text = text;
        Font = font;
        Color = color;
        IsUnderlined = isUnderlined;
        IsStrikethrough = isStrikethrough;
    }
}

public class RichText
{
    public List<RichTextSegment> Segments { get; } = new();

    public RichText AddText(string text, PdfFont? font = null, PdfColor? color = null, bool isUnderlined = false, bool isStrikethrough = false)
    {
        Segments.Add(new RichTextSegment(text, font, color, isUnderlined, isStrikethrough));
        return this;
    }

    public RichText AddBold(string text, PdfColor? color = null, bool isUnderlined = false, bool isStrikethrough = false)
    {
        Segments.Add(new RichTextSegment(text, PdfFont.HelveticaBold, color, isUnderlined, isStrikethrough));
        return this;
    }

    public RichText AddItalic(string text, PdfColor? color = null, bool isUnderlined = false, bool isStrikethrough = false)
    {
        Segments.Add(new RichTextSegment(text, PdfFont.HelveticaOblique, color, isUnderlined, isStrikethrough));
        return this;
    }

    public RichText AddBoldItalic(string text, PdfColor? color = null, bool isUnderlined = false, bool isStrikethrough = false)
    {
        Segments.Add(new RichTextSegment(text, PdfFont.HelveticaBoldOblique, color, isUnderlined, isStrikethrough));
        return this;
    }

    public RichText AddUnderlined(string text, PdfFont? font = null, PdfColor? color = null)
    {
        Segments.Add(new RichTextSegment(text, font, color, isUnderlined: true));
        return this;
    }

    public RichText AddStrikethrough(string text, PdfFont? font = null, PdfColor? color = null)
    {
        Segments.Add(new RichTextSegment(text, font, color, isStrikethrough: true));
        return this;
    }

    public string GetPlainText()
    {
        return string.Join("", Segments.Select(s => s.Text));
    }
}