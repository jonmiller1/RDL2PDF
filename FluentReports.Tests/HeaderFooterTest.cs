using FluentReports.Core.PdfRenderer;

namespace FluentReports.Tests;

public class HeaderFooterTest
{
    [Fact]
    public void CanCreateHeadersAndFooters()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        // Set up headers and footers
        var headerFooter = new PageHeaderFooter
        {
            Header = new HeaderFooterContent
            {
                Text = "Sample Document Header",
                FontSize = 14f,
                Color = PdfColor.DarkBlue,
                Font = PdfFont.HelveticaBold,
                Alignment = TextAlignment.Center,
                BackgroundColor = new PdfColor(0.95f, 0.95f, 1f), // Light blue background
                BorderColor = PdfColor.Blue,
                BorderWidth = 1f,
                Padding = 0.1f
            },
            Footer = new HeaderFooterContent
            {
                ShowPageNumbers = true,
                PageNumberFormat = "Page {0}",
                FontSize = 10f,
                Color = PdfColor.Gray,
                Alignment = TextAlignment.Center,
                BorderColor = PdfColor.Gray,
                BorderWidth = 0.5f,
                Padding = 0.05f
            },
            HeaderHeight = 1f,
            FooterHeight = 0.5f,
            HeaderBottomMargin = 0.25f,
            FooterTopMargin = 0.25f
        };
        
        renderer.SetHeaderFooter(headerFooter);
        
        // Add content to first page
        renderer.DrawTextInches("This is page 1 content", 1f, 1f, 0.2f, PdfColor.Black);
        renderer.DrawTextInches("The header and footer should appear on this page.", 1f, 1.5f, 0.12f, PdfColor.Black);
        
        // Draw some content to show content area boundaries
        renderer.DrawRectangleInches(0.5f, 0.5f, 7.5f, renderer.PointsToInches(renderer.GetContentAreaHeight()) - 1f, 1f, PdfColor.LightGray, null);
        
        // Add a second page
        renderer.AddNewPage();
        renderer.DrawTextInches("This is page 2 content", 1f, 1f, 0.2f, PdfColor.Black);
        renderer.DrawTextInches("Headers and footers should appear on all pages.", 1f, 1.5f, 0.12f, PdfColor.Black);
        
        // Add a third page to test page numbering
        renderer.AddNewPage();
        renderer.DrawTextInches("This is page 3 content", 1f, 1f, 0.2f, PdfColor.Black);
        renderer.DrawTextInches("Page numbers should increment correctly.", 1f, 1.5f, 0.12f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\HeaderFooterTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal(3, renderer.TotalPages);
    }
    
    [Fact]
    public void CanCreatePageOfTotalNumbering()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        // Set up footer with "page X of Y" format
        var headerFooter = new PageHeaderFooter
        {
            Header = new HeaderFooterContent
            {
                Text = "Document with Page X of Y Format",
                FontSize = 16f,
                Color = PdfColor.Black,
                Font = PdfFont.HelveticaBold,
                Alignment = TextAlignment.Center
            },
            Footer = new HeaderFooterContent
            {
                ShowPageNumbers = true,
                UsePageNumberOfTotal = true,
                PageNumberOfTotalFormat = "Page {0} of {1}",
                FontSize = 10f,
                Color = PdfColor.DarkGray,
                Alignment = TextAlignment.Right,
                LeftMargin = 0.5f,
                RightMargin = 1f
            }
        };
        
        renderer.SetHeaderFooter(headerFooter);
        
        // Create 5 pages to test the numbering
        for (int i = 1; i <= 5; i++)
        {
            if (i > 1) renderer.AddNewPage();
            
            renderer.DrawTextInches($"Content for page {i}", 1f, 1f, 0.15f, PdfColor.Black);
            renderer.DrawTextInches($"This page should show 'Page {i} of 5' in the footer.", 1f, 1.5f, 0.11f, PdfColor.Gray);
        }
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\PageOfTotalTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal(5, renderer.TotalPages);
    }
    
    [Fact]
    public void CanCreateDifferentFirstLastPageHeaders()
    {
        using var renderer = MultiPagePdfRenderer.CreateFromInches(8.5f, 11f, 96f);
        
        // Set up different headers for first, middle, and last pages
        var headerFooter = new PageHeaderFooter
        {
            Header = new HeaderFooterContent
            {
                Text = "Regular Page Header",
                FontSize = 12f,
                Color = PdfColor.Black,
                Alignment = TextAlignment.Center
            },
            FirstPageHeader = new HeaderFooterContent
            {
                Text = "TITLE PAGE - Special First Page Header",
                FontSize = 16f,
                Color = PdfColor.DarkRed,
                Font = PdfFont.HelveticaBold,
                Alignment = TextAlignment.Center,
                BackgroundColor = new PdfColor(1f, 0.9f, 0.9f), // Light red
                BorderColor = PdfColor.Red,
                BorderWidth = 2f
            },
            LastPageHeader = new HeaderFooterContent
            {
                Text = "FINAL PAGE - Special Last Page Header",
                FontSize = 14f,
                Color = PdfColor.DarkGreen,
                Font = PdfFont.HelveticaBold,
                Alignment = TextAlignment.Center,
                BackgroundColor = new PdfColor(0.9f, 1f, 0.9f), // Light green
                BorderColor = PdfColor.Green,
                BorderWidth = 2f
            },
            Footer = new HeaderFooterContent
            {
                ShowPageNumbers = true,
                UsePageNumberOfTotal = true,
                PageNumberOfTotalFormat = "Page {0} of {1}",
                FontSize = 10f,
                Color = PdfColor.Gray,
                Alignment = TextAlignment.Center
            }
        };
        
        renderer.SetHeaderFooter(headerFooter);
        
        // Create 4 pages to test first/middle/last page headers
        var pageNames = new[] { "First Page (Title)", "Middle Page 1", "Middle Page 2", "Last Page (Final)" };
        
        for (int i = 0; i < 4; i++)
        {
            if (i > 0) renderer.AddNewPage();
            
            renderer.DrawTextInches(pageNames[i], 1f, 1f, 0.18f, PdfColor.Black, PdfFont.HelveticaBold);
            
            if (i == 0)
            {
                renderer.DrawTextInches("This first page should have a red header saying 'TITLE PAGE'", 1f, 1.5f, 0.11f, PdfColor.Gray);
            }
            else if (i == 3)
            {
                renderer.DrawTextInches("This last page should have a green header saying 'FINAL PAGE'", 1f, 1.5f, 0.11f, PdfColor.Gray);
            }
            else
            {
                renderer.DrawTextInches("This middle page should have the regular header", 1f, 1.5f, 0.11f, PdfColor.Gray);
            }
        }
        
        var pdfBytes = renderer.ToByteArray();
        File.WriteAllBytes(@"c:\output\DifferentHeadersTest.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        Assert.Equal(4, renderer.TotalPages);
    }
}