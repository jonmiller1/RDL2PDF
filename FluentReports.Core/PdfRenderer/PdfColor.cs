namespace FluentReports.Core.PdfRenderer;

public class PdfColor
{
    public float R { get; }
    public float G { get; }
    public float B { get; }

    public PdfColor(float r, float g, float b)
    {
        R = Math.Clamp(r, 0f, 1f);
        G = Math.Clamp(g, 0f, 1f);
        B = Math.Clamp(b, 0f, 1f);
    }

    public PdfColor(int r, int g, int b) : this(r / 255f, g / 255f, b / 255f)
    {
    }

    public static PdfColor Black => new(0f, 0f, 0f);
    public static PdfColor White => new(1f, 1f, 1f);
    public static PdfColor Red => new(1f, 0f, 0f);
    public static PdfColor Green => new(0f, 1f, 0f);
    public static PdfColor Blue => new(0f, 0f, 1f);
    public static PdfColor Yellow => new(1f, 1f, 0f);
    public static PdfColor Cyan => new(0f, 1f, 1f);
    public static PdfColor Magenta => new(1f, 0f, 1f);
    public static PdfColor Gray => new(0.5f, 0.5f, 0.5f);
    public static PdfColor LightGray => new(0.75f, 0.75f, 0.75f);
    public static PdfColor DarkGray => new(0.25f, 0.25f, 0.25f);
}