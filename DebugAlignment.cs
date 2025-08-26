using FluentReports.Core.PdfRenderer;

var renderer = SimplePdfRenderer.CreateFromInches(8.5f, 11f);

// Create three simple text boxes to test alignment
var leftBox = new TextBox(1f, 1f, 3f, 1f)
{
    BackgroundColor = new PdfColor(0.9f, 0.9f, 1f),
    BorderColor = PdfColor.Blue,
    BorderWidth = 2f,
    Padding = 0.1f,
    Alignment = TextAlignment.Left
};

var centerBox = new TextBox(1f, 3f, 3f, 1f)
{
    BackgroundColor = new PdfColor(0.9f, 1f, 0.9f),
    BorderColor = PdfColor.Green,
    BorderWidth = 2f,
    Padding = 0.1f,
    Alignment = TextAlignment.Center
};

var rightBox = new TextBox(1f, 5f, 3f, 1f)
{
    BackgroundColor = new PdfColor(1f, 0.9f, 0.9f),
    BorderColor = PdfColor.Red,
    BorderWidth = 2f,
    Padding = 0.1f,
    Alignment = TextAlignment.Right
};

// Draw the boxes with text
renderer.DrawTextBoxInches(leftBox, "Left Aligned Text", 0.15f, PdfColor.Blue);
renderer.DrawTextBoxInches(centerBox, "Center Aligned Text", 0.15f, PdfColor.Green);
renderer.DrawTextBoxInches(rightBox, "Right Aligned Text", 0.15f, PdfColor.Red);

// Save the PDF
var pdfBytes = renderer.ToByteArray();
File.WriteAllBytes(@"c:\output\DebugAlignment.pdf", pdfBytes);

Console.WriteLine("Debug alignment PDF created at c:\\output\\DebugAlignment.pdf");