namespace FluentReports.Core.PdfRenderer;

public enum TextBoxSizing
{
    Fixed,              // Fixed width and height
    AutoWidth,          // Auto width, fixed height
    AutoHeight,         // Fixed width, auto height  
    AutoBoth            // Auto width and height
}

public enum TextBoxOverflow
{
    Clip,               // Clip text that doesn't fit
    Expand,             // Expand box to fit all text
    Wrap                // Wrap text and expand height as needed
}

public class TextBox
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float MinWidth { get; set; } = 0f;
    public float MaxWidth { get; set; } = float.MaxValue;
    public float MinHeight { get; set; } = 0f;
    public float MaxHeight { get; set; } = float.MaxValue;
    public float Padding { get; set; } = 0f;
    public float PaddingLeft { get; set; } = 0f;
    public float PaddingRight { get; set; } = 0f;
    public float PaddingTop { get; set; } = 0f;
    public float PaddingBottom { get; set; } = 0f;
    
    public TextBoxSizing Sizing { get; set; } = TextBoxSizing.Fixed;
    public TextBoxOverflow Overflow { get; set; } = TextBoxOverflow.Wrap;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public float LineSpacing { get; set; } = 1.2f;
    
    public PdfColor? BackgroundColor { get; set; }
    public PdfColor? BorderColor { get; set; }
    public float BorderWidth { get; set; } = 1f;
    public LineStyle? BorderStyle { get; set; }

    public TextBox(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float GetEffectivePaddingLeft() => PaddingLeft > 0 ? PaddingLeft : Padding;
    public float GetEffectivePaddingRight() => PaddingRight > 0 ? PaddingRight : Padding;
    public float GetEffectivePaddingTop() => PaddingTop > 0 ? PaddingTop : Padding;
    public float GetEffectivePaddingBottom() => PaddingBottom > 0 ? PaddingBottom : Padding;

    public float GetContentWidth() => Math.Max(0, Width - GetEffectivePaddingLeft() - GetEffectivePaddingRight());
    public float GetContentHeight() => Math.Max(0, Height - GetEffectivePaddingTop() - GetEffectivePaddingBottom());

    public float GetContentX() => X + GetEffectivePaddingLeft();
    public float GetContentY() => Y + GetEffectivePaddingTop();
}