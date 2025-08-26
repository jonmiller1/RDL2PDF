using System.Globalization;
using System.Text;

namespace FluentReports.Core.PdfRenderer;

public class MultiPagePdfRenderer : IDisposable
{
    private readonly List<SimplePdfRenderer> _pages = new();
    private readonly float _pageWidth;
    private readonly float _pageHeight;
    private readonly float _dpi;
    private SimplePdfRenderer _currentPage;
    private PageHeaderFooter? _headerFooter;
    private readonly List<Watermark> _watermarks = new();
    private bool _disposed = false;
    
    // Content area boundaries (adjusted for headers/footers)
    private float _contentAreaTop;
    private float _contentAreaBottom;
    private float _contentAreaHeight;
    
    public int CurrentPageNumber => _pages.Count;
    public int TotalPages => _pages.Count;
    public float DPI => _dpi;
    
    public MultiPagePdfRenderer(float widthPoints = 612f, float heightPoints = 792f, float dpi = 72f)
    {
        _pageWidth = widthPoints;
        _pageHeight = heightPoints;
        _dpi = dpi;
        
        // Start with first page
        _currentPage = new SimplePdfRenderer(widthPoints, heightPoints) { DPI = dpi };
        _pages.Add(_currentPage);
        
        // Initialize content area (will be adjusted when headers/footers are set)
        _contentAreaTop = 0;
        _contentAreaBottom = heightPoints;
        _contentAreaHeight = heightPoints;
    }
    
    public static MultiPagePdfRenderer CreateFromInches(float widthInches, float heightInches, float dpi = 72f)
    {
        var widthPoints = widthInches * 72f;
        var heightPoints = heightInches * 72f;
        return new MultiPagePdfRenderer(widthPoints, heightPoints, dpi);
    }
    
    public void SetHeaderFooter(PageHeaderFooter headerFooter)
    {
        _headerFooter = headerFooter;
        UpdateContentAreaBounds();
    }
    
    public void AddWatermark(Watermark watermark)
    {
        _watermarks.Add(watermark);
    }
    
    public void ClearWatermarks()
    {
        _watermarks.Clear();
    }
    
    public void RemoveWatermark(Watermark watermark)
    {
        _watermarks.Remove(watermark);
    }
    
    private void UpdateContentAreaBounds()
    {
        if (_headerFooter == null)
        {
            _contentAreaTop = 0;
            _contentAreaBottom = _pageHeight;
            _contentAreaHeight = _pageHeight;
            return;
        }
        
        // Calculate content area based on header/footer settings
        var headerTotalHeight = 0f;
        var footerTotalHeight = 0f;
        
        if (_headerFooter.Header != null)
        {
            headerTotalHeight = InchesToPoints(_headerFooter.HeaderHeight + _headerFooter.HeaderBottomMargin);
        }
        
        if (_headerFooter.Footer != null)
        {
            footerTotalHeight = InchesToPoints(_headerFooter.FooterHeight + _headerFooter.FooterTopMargin);
        }
        
        _contentAreaTop = headerTotalHeight;
        _contentAreaBottom = _pageHeight - footerTotalHeight;
        _contentAreaHeight = _contentAreaBottom - _contentAreaTop;
    }
    
    public SimplePdfRenderer AddNewPage()
    {
        _currentPage = new SimplePdfRenderer(_pageWidth, _pageHeight) { DPI = _dpi };
        _pages.Add(_currentPage);
        return _currentPage;
    }
    
    public float GetContentAreaTop() => _contentAreaTop;
    public float GetContentAreaBottom() => _contentAreaBottom;
    public float GetContentAreaHeight() => _contentAreaHeight;
    
    // Conversion methods
    public float InchesToPoints(float inches) => inches * 72f;
    public float PointsToInches(float points) => points / 72f;
    public float PixelsToPoints(float pixels) => pixels * 72f / _dpi;
    public float PointsToPixels(float points) => points * _dpi / 72f;
    
    // Delegate methods to current page
    public void DrawText(string text, float x, float y, float fontSize = 12f, PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left)
    {
        // Adjust Y coordinate to content area
        var adjustedY = _contentAreaTop + y;
        _currentPage.DrawText(text, x, adjustedY, fontSize, color, font, alignment);
    }
    
    public void DrawTextInches(string text, float x, float y, float fontSize = 12f, PdfColor? color = null, PdfFont? font = null, TextAlignment alignment = TextAlignment.Left)
    {
        DrawText(text, InchesToPoints(x), InchesToPoints(y), fontSize * 72f, color, font, alignment);
    }
    
    public void DrawLine(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        var adjustedY1 = _contentAreaTop + y1;
        var adjustedY2 = _contentAreaTop + y2;
        _currentPage.DrawLine(x1, adjustedY1, x2, adjustedY2, lineWidth, color);
    }
    
    public void DrawLineInches(float x1, float y1, float x2, float y2, float lineWidth = 1f, PdfColor? color = null)
    {
        DrawLine(InchesToPoints(x1), InchesToPoints(y1), InchesToPoints(x2), InchesToPoints(y2), lineWidth, color);
    }
    
    public void DrawRectangle(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? lineStyle = null)
    {
        var adjustedY = _contentAreaTop + y;
        _currentPage.DrawRectangle(x, adjustedY, width, height, lineWidth, strokeColor, fillColor, lineStyle);
    }
    
    public void DrawRectangleInches(float x, float y, float width, float height, float lineWidth = 1f, PdfColor? strokeColor = null, PdfColor? fillColor = null, LineStyle? lineStyle = null)
    {
        DrawRectangle(InchesToPoints(x), InchesToPoints(y), InchesToPoints(width), InchesToPoints(height), lineWidth, strokeColor, fillColor, lineStyle);
    }
    
    private void RenderHeadersFooters()
    {
        if (_headerFooter == null) return;
        
        for (int pageIndex = 0; pageIndex < _pages.Count; pageIndex++)
        {
            var page = _pages[pageIndex];
            var pageNumber = pageIndex + 1;
            var isFirstPage = pageNumber == 1;
            var isLastPage = pageNumber == _pages.Count;
            
            // Determine which header/footer to use
            var header = GetEffectiveHeader(isFirstPage, isLastPage);
            var footer = GetEffectiveFooter(isFirstPage, isLastPage);
            
            // Render header
            if (header != null && ShouldShowOnPage(header, isFirstPage, isLastPage))
            {
                RenderHeaderFooterContent(page, header, pageNumber, _pages.Count, true);
            }
            
            // Render footer
            if (footer != null && ShouldShowOnPage(footer, isFirstPage, isLastPage))
            {
                RenderHeaderFooterContent(page, footer, pageNumber, _pages.Count, false);
            }
        }
    }
    
    private HeaderFooterContent? GetEffectiveHeader(bool isFirstPage, bool isLastPage)
    {
        if (isFirstPage && _headerFooter?.FirstPageHeader != null)
            return _headerFooter.FirstPageHeader;
        if (isLastPage && _headerFooter?.LastPageHeader != null)
            return _headerFooter.LastPageHeader;
        return _headerFooter?.Header;
    }
    
    private HeaderFooterContent? GetEffectiveFooter(bool isFirstPage, bool isLastPage)
    {
        if (isFirstPage && _headerFooter?.FirstPageFooter != null)
            return _headerFooter.FirstPageFooter;
        if (isLastPage && _headerFooter?.LastPageFooter != null)
            return _headerFooter.LastPageFooter;
        return _headerFooter?.Footer;
    }
    
    private bool ShouldShowOnPage(HeaderFooterContent content, bool isFirstPage, bool isLastPage)
    {
        if (_headerFooter == null) return true;
        
        if (isFirstPage && !_headerFooter.ShowOnFirstPage) return false;
        if (isLastPage && !_headerFooter.ShowOnLastPage) return false;
        
        return true;
    }
    
    private void RenderHeaderFooterContent(SimplePdfRenderer page, HeaderFooterContent content, int pageNumber, int totalPages, bool isHeader)
    {
        // Calculate position
        var leftMargin = InchesToPoints(content.LeftMargin);
        var rightMargin = InchesToPoints(content.RightMargin);
        var width = _pageWidth - leftMargin - rightMargin;
        
        float y;
        if (isHeader)
        {
            y = InchesToPoints(content.TopMargin);
        }
        else
        {
            y = _pageHeight - InchesToPoints(content.BottomMargin + (_headerFooter?.FooterHeight ?? 0.75f));
        }
        
        var height = InchesToPoints(isHeader ? _headerFooter?.HeaderHeight ?? 0.75f : _headerFooter?.FooterHeight ?? 0.75f);
        
        // Draw background if specified
        if (content.BackgroundColor != null)
        {
            page.DrawRectangle(leftMargin, y, width, height, 0f, null, content.BackgroundColor);
        }
        
        // Draw border if specified
        if (content.BorderColor != null && content.BorderWidth > 0)
        {
            page.DrawRectangle(leftMargin, y, width, height, content.BorderWidth, content.BorderColor, null);
        }
        
        // Calculate text area
        var padding = InchesToPoints(content.Padding);
        var textX = leftMargin + padding;
        var textWidth = width - (padding * 2);
        var textHeight = height - (padding * 2);
        
        // Center text vertically within the available height
        // PDF text baseline positioning: center the text considering font size and baseline
        var textY = y + padding + (textHeight / 2) - (content.FontSize * 0.2f);
        
        // Determine text to display
        string? displayText = null;
        if (content.ShowPageNumbers)
        {
            if (content.UsePageNumberOfTotal)
            {
                displayText = string.Format(content.PageNumberOfTotalFormat, pageNumber, totalPages);
            }
            else
            {
                displayText = string.Format(content.PageNumberFormat, pageNumber);
            }
        }
        else if (!string.IsNullOrEmpty(content.Text))
        {
            displayText = content.Text;
        }
        
        // Render text
        if (!string.IsNullOrEmpty(displayText))
        {
            // Calculate text position based on alignment
            var finalTextX = content.Alignment switch
            {
                TextAlignment.Center => textX + (textWidth / 2),
                TextAlignment.Right => textX + textWidth,
                _ => textX
            };
            
            page.DrawText(displayText, finalTextX, textY, content.FontSize, content.Color, content.Font, content.Alignment);
        }
        else if (content.RichText != null)
        {
            // TODO: Implement rich text rendering for headers/footers
            // For now, render as plain text
            var plainText = content.RichText.GetPlainText();
            if (!string.IsNullOrEmpty(plainText))
            {
                var finalTextX = content.Alignment switch
                {
                    TextAlignment.Center => textX + (textWidth / 2),
                    TextAlignment.Right => textX + textWidth,
                    _ => textX
                };
                
                page.DrawText(plainText, finalTextX, textY, content.FontSize, content.Color, content.Font, content.Alignment);
            }
        }
    }
    
    private void RenderWatermarks(WatermarkLayer layer)
    {
        for (int pageIndex = 0; pageIndex < _pages.Count; pageIndex++)
        {
            var page = _pages[pageIndex];
            var pageNumber = pageIndex + 1;
            
            foreach (var watermark in _watermarks)
            {
                if (watermark.Layer == layer)
                {
                    watermark.Render(page, _pageWidth, _pageHeight, pageNumber, _pages.Count);
                }
            }
        }
    }
    
    public byte[] ToByteArray()
    {
        // Render watermarks first (background layer)
        RenderWatermarks(WatermarkLayer.Background);
        
        // Then render headers and footers
        RenderHeadersFooters();
        
        // Finally render foreground watermarks
        RenderWatermarks(WatermarkLayer.Foreground);
        
        // For now, we'll combine all pages into a single PDF by merging their content
        // This is a simplified approach - a full implementation would create a proper multi-page PDF structure
        
        if (_pages.Count == 1)
        {
            return _pages[0].ToByteArray();
        }
        
        // For multiple pages, we need to create a proper multi-page PDF
        // This requires a more complex PDF structure with multiple page objects
        return CreateMultiPagePdf();
    }
    
    private byte[] CreateMultiPagePdf()
    {
        using var pdfStream = new MemoryStream();
        using var writer = new BinaryWriter(pdfStream);
        
        WriteText(writer, "%PDF-1.4\n");
        
        var positions = new List<long>();
        var pageObjectIds = new List<int>();
        
        // Reserve object IDs
        var catalogId = 1;
        var pagesId = 2;
        var startPageId = 3;
        
        // Calculate object IDs for all pages and their content
        var totalObjects = 2; // catalog + pages object
        var pageContentIds = new List<int>();
        
        for (int i = 0; i < _pages.Count; i++)
        {
            var pageId = startPageId + (i * 2);
            var contentId = pageId + 1;
            pageObjectIds.Add(pageId);
            pageContentIds.Add(contentId);
            totalObjects += 2; // page + content
        }
        
        // Create catalog
        positions.Add(pdfStream.Length);
        WriteText(writer, $"{catalogId} 0 obj\n<<\n/Type /Catalog\n/Pages {pagesId} 0 R\n>>\nendobj\n");
        
        // Create pages object
        positions.Add(pdfStream.Length);
        var pageReferences = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        WriteText(writer, $"{pagesId} 0 obj\n<<\n/Type /Pages\n/Count {_pages.Count}\n/Kids [{pageReferences}]\n>>\nendobj\n");
        
        // Create page objects and content streams
        for (int i = 0; i < _pages.Count; i++)
        {
            var pageId = pageObjectIds[i];
            var contentId = pageContentIds[i];
            var page = _pages[i];
            
            // Get page content
            var pageBytes = page.ToByteArray();
            // Extract content stream from the single-page PDF (this is a simplified approach)
            var contentStream = ExtractContentStreamFromPdf(pageBytes);
            var contentBytes = Encoding.ASCII.GetBytes(contentStream);
            
            // Page object
            positions.Add(pdfStream.Length);
            WriteText(writer, $"{pageId} 0 obj\n<<\n/Type /Page\n/Parent {pagesId} 0 R\n/MediaBox [0 0 {_pageWidth.ToString(CultureInfo.InvariantCulture)} {_pageHeight.ToString(CultureInfo.InvariantCulture)}]\n/Resources <<\n  /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >>\n>>\n/Contents {contentId} 0 R\n>>\nendobj\n");
            
            // Content stream
            positions.Add(pdfStream.Length);
            WriteText(writer, $"{contentId} 0 obj\n<<\n/Length {contentBytes.Length}\n>>\nstream\n");
            writer.Write(contentBytes);
            WriteText(writer, "\nendstream\nendobj\n");
        }
        
        // Cross-reference table
        var xrefPos = pdfStream.Length;
        WriteText(writer, $"xref\n0 {totalObjects + 1}\n0000000000 65535 f \n");
        
        foreach (var pos in positions)
        {
            WriteText(writer, $"{pos:D10} 00000 n \n");
        }
        
        // Trailer
        WriteText(writer, $"trailer\n<<\n/Size {totalObjects + 1}\n/Root {catalogId} 0 R\n>>\n");
        WriteText(writer, $"startxref\n{xrefPos}\n%%EOF");
        
        return pdfStream.ToArray();
    }
    
    private string ExtractContentStreamFromPdf(byte[] pdfBytes)
    {
        // This is a simplified extraction - in a real implementation, you'd parse the PDF structure
        var pdfContent = Encoding.ASCII.GetString(pdfBytes);
        var streamStart = pdfContent.IndexOf("stream\n") + 7;
        var streamEnd = pdfContent.IndexOf("\nendstream");
        
        if (streamStart > 6 && streamEnd > streamStart)
        {
            return pdfContent.Substring(streamStart, streamEnd - streamStart);
        }
        
        return "";
    }
    
    private void WriteText(BinaryWriter writer, string text)
    {
        writer.Write(Encoding.ASCII.GetBytes(text));
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var page in _pages)
            {
                page.Dispose();
            }
            _pages.Clear();
            _disposed = true;
        }
    }
}