using FluentReports.Core.PdfRenderer;

namespace FluentReports.Tests;

public class PdfDebugTest
{
    [Fact]
    public void CreateMinimalPdf()
    {
        using var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f, 300f);
        
        renderer.DrawText("Hello World - Fixed Debug Test", 50, 50, 12f, PdfColor.Black);
        
        var pdfBytes = renderer.ToByteArray();
        var pdfText = System.Text.Encoding.ASCII.GetString(pdfBytes);
        
        // Output the raw PDF content for debugging
        Console.WriteLine("PDF Content:");
        Console.WriteLine(pdfText);
        
        File.WriteAllBytes(@"c:\output\DebugMinimal_Fixed.pdf", pdfBytes);
        
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }
}