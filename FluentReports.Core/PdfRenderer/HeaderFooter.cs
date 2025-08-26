namespace FluentReports.Core.PdfRenderer;

public class HeaderFooterContent
{
    public string? Text { get; set; }
    public RichText? RichText { get; set; }
    public float FontSize { get; set; } = 12f;
    public PdfColor? Color { get; set; }
    public PdfFont? Font { get; set; }
    public TextAlignment Alignment { get; set; } = TextAlignment.Center;
    public float LeftMargin { get; set; } = 0.5f; // inches from page edge
    public float RightMargin { get; set; } = 0.5f;
    public float TopMargin { get; set; } = 0.5f;
    public float BottomMargin { get; set; } = 0.5f;
    
    // Page numbering support
    public bool ShowPageNumbers { get; set; } = false;
    public string PageNumberFormat { get; set; } = "Page {0}"; // {0} will be replaced with page number
    public string PageNumberOfTotalFormat { get; set; } = "Page {0} of {1}"; // {0} = current, {1} = total
    public bool UsePageNumberOfTotal { get; set; } = false;
    
    // Background and border
    public PdfColor? BackgroundColor { get; set; }
    public PdfColor? BorderColor { get; set; }
    public float BorderWidth { get; set; } = 0f;
    public float Padding { get; set; } = 0.1f; // inches
}

public class PageHeaderFooter
{
    public HeaderFooterContent? Header { get; set; }
    public HeaderFooterContent? Footer { get; set; }
    public float HeaderHeight { get; set; } = 0.75f; // inches
    public float FooterHeight { get; set; } = 0.75f; // inches
    
    // Content area margins (space between header/footer and main content)
    public float HeaderBottomMargin { get; set; } = 0.25f; // inches
    public float FooterTopMargin { get; set; } = 0.25f; // inches
    
    public bool ShowOnFirstPage { get; set; } = true;
    public bool ShowOnLastPage { get; set; } = true;
    
    // Different headers/footers for first/last page
    public HeaderFooterContent? FirstPageHeader { get; set; }
    public HeaderFooterContent? FirstPageFooter { get; set; }
    public HeaderFooterContent? LastPageHeader { get; set; }
    public HeaderFooterContent? LastPageFooter { get; set; }
}