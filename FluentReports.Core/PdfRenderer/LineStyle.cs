namespace FluentReports.Core.PdfRenderer;

public enum LinePattern
{
    Solid,
    Dashed,
    Dotted,
    DashDot,
    DashDotDot
}

public enum LineCap
{
    Butt = 0,      // Square end, flush with endpoint
    Round = 1,     // Rounded end
    Square = 2     // Square end, extends beyond endpoint
}

public enum LineJoin
{
    Miter = 0,     // Sharp corners
    Round = 1,     // Rounded corners  
    Bevel = 2      // Beveled corners
}

public class LineStyle
{
    public LinePattern Pattern { get; }
    public LineCap Cap { get; }
    public LineJoin Join { get; }
    public float[]? DashArray { get; }
    
    public LineStyle(LinePattern pattern = LinePattern.Solid, LineCap cap = LineCap.Butt, LineJoin join = LineJoin.Miter)
    {
        Pattern = pattern;
        Cap = cap;
        Join = join;
        DashArray = pattern switch
        {
            LinePattern.Dashed => new[] { 6f, 3f },
            LinePattern.Dotted => new[] { 1f, 2f },
            LinePattern.DashDot => new[] { 6f, 3f, 1f, 3f },
            LinePattern.DashDotDot => new[] { 6f, 3f, 1f, 3f, 1f, 3f },
            _ => null
        };
    }
    
    public LineStyle(float[] customDashArray, LineCap cap = LineCap.Butt, LineJoin join = LineJoin.Miter)
    {
        Pattern = LinePattern.Dashed;
        Cap = cap;
        Join = join;
        DashArray = customDashArray;
    }
    
    public LineStyle(LineCap cap, LineJoin join = LineJoin.Miter)
    {
        Pattern = LinePattern.Solid;
        Cap = cap;
        Join = join;
        DashArray = null;
    }
    
    public static LineStyle Solid => new();
    public static LineStyle Dashed => new(LinePattern.Dashed);
    public static LineStyle Dotted => new(LinePattern.Dotted);
    public static LineStyle DashDot => new(LinePattern.DashDot);
    public static LineStyle DashDotDot => new(LinePattern.DashDotDot);
}