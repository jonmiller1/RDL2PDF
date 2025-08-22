using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using System.Data;
using System.Xml.Linq;
using ScottPlot;

namespace FluentRDLC.Renderer.Renderers
{
    public class PdfRenderer : IRenderer
    {
        public RenderFormat Format => RenderFormat.PDF;

        public string GetFileExtension() => ".pdf";
        public string GetMimeType() => "application/pdf";

        public byte[] Render(RenderContext context)
        {
            using var stream = new MemoryStream();
            
            // Create PDF document
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);
            
            // Extract page settings from RDLC
            var pageWidth = ParseDimension(context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageWidth")?.Value ?? "8.5in");
            var pageHeight = ParseDimension(context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageHeight")?.Value ?? "11in");
            
            // Set page size
            pdf.SetDefaultPageSize(new PageSize(pageWidth, pageHeight));
            
            // Get canvas for low-level drawing
            var page = pdf.AddNewPage();
            var canvas = new PdfCanvas(page);
            
            // Render RDLC elements
            RenderRdlcElements(canvas, document, context, pageWidth, pageHeight);
            
            document.Close();
            return stream.ToArray();
        }

        private void RenderRdlcElements(PdfCanvas canvas, Document document, RenderContext context, float pageWidth, float pageHeight)
        {
            // Find the Body element in the RDLC
            var bodyElement = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "Body");
            if (bodyElement == null)
            {
                Console.WriteLine("DEBUG: No Body element found in RDLC");
                return;
            }

            // Start rendering from the Body's ReportItems
            RenderReportItems(canvas, document, bodyElement, context, pageWidth, pageHeight);
        }

        private void RenderReportItems(PdfCanvas canvas, Document document, XElement parent, RenderContext context, float pageWidth, float pageHeight)
        {
            var reportItemsElement = parent.Element(context.RdlcNamespace + "ReportItems");
            if (reportItemsElement == null)
            {
                Console.WriteLine("DEBUG: No ReportItems found");
                return;
            }

            var items = reportItemsElement.Elements().ToList();
            Console.WriteLine($"DEBUG: Found {items.Count} report items");

            // Render in order: Rectangles first (backgrounds), then other items
            var rectangles = items.Where(i => i.Name.LocalName == "Rectangle").ToList();
            var otherItems = items.Where(i => i.Name.LocalName != "Rectangle").ToList();

            // First render all rectangles as backgrounds
            foreach (var item in rectangles)
            {
                Console.WriteLine($"DEBUG: Processing background: {item.Name.LocalName}");
                RenderSingleItem(canvas, document, item, context, pageWidth, pageHeight);
            }

            // Then render all other items on top
            foreach (var item in otherItems)
            {
                Console.WriteLine($"DEBUG: Processing item: {item.Name.LocalName}");
                RenderSingleItem(canvas, document, item, context, pageWidth, pageHeight);
            }
        }

        private void RenderSingleItem(PdfCanvas canvas, Document document, XElement item, RenderContext context, float pageWidth, float pageHeight)
        {
            var bounds = ExtractBounds(item, context);
            if (bounds == null)
            {
                Console.WriteLine($"DEBUG: No bounds found for item: {item.Name.LocalName}");
                return;
            }

            // Convert coordinates to PDF coordinate system (origin at bottom-left)
            var x = bounds.Left;
            var y = pageHeight - bounds.Top - bounds.Height; // Flip Y coordinate
            var width = bounds.Width;
            var height = bounds.Height;

            Console.WriteLine($"DEBUG: Item bounds: ({x}, {y}, {width}, {height})");

            switch (item.Name.LocalName)
            {
                case "Textbox":
                    RenderTextbox(canvas, document, item, context, x, y, width, height);
                    break;
                case "Tablix":
                    RenderTablix(canvas, document, item, context, x, y, width, height);
                    break;
                case "Rectangle":
                    RenderRectangle(canvas, document, item, context, x, y, width, height);
                    break;
                case "Image":
                    RenderImage(canvas, document, item, context, x, y, width, height);
                    break;
            }
        }

        private void RenderTextbox(PdfCanvas canvas, Document document, XElement textboxElement, RenderContext context, float x, float y, float width, float height)
        {
            var valueElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")
                ?.Element(context.RdlcNamespace + "Paragraph")
                ?.Element(context.RdlcNamespace + "TextRuns")
                ?.Element(context.RdlcNamespace + "TextRun")
                ?.Element(context.RdlcNamespace + "Value");

            if (valueElement != null)
            {
                var text = ProcessTextValue(valueElement.Value, context);
                var style = ExtractTextStyle(textboxElement, context);
                
                // Set font
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                if (style.Bold)
                    font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                
                // Set color
                var color = ColorConstants.BLACK;
                if (!string.IsNullOrEmpty(style.Color))
                {
                    color = ParseColor(style.Color);
                }
                
                canvas.BeginText()
                      .SetFont(font, style.FontSize)
                      .SetColor(color, true)
                      .MoveText(x, y + height - style.FontSize) // Adjust for text baseline
                      .ShowText(text)
                      .EndText();
            }
        }

        private void RenderTablix(PdfCanvas canvas, Document document, XElement tablixElement, RenderContext context, float x, float y, float width, float height)
        {
            var dataSetName = tablixElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
            {
                // Render "NO DATA" message
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                canvas.BeginText()
                      .SetFont(font, 12)
                      .SetColor(ColorConstants.RED, true)
                      .MoveText(x, y + height/2)
                      .ShowText("NO DATA")
                      .EndText();
                return;
            }

            var dataTable = context.DataSources[dataSetName];
            
            // Draw table header
            DrawRectangle(canvas, x, y + height - 20, width, 20, ColorConstants.LIGHT_GRAY, ColorConstants.BLACK);
            
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            canvas.BeginText()
                  .SetFont(font, 10)
                  .SetColor(ColorConstants.BLACK, true)
                  .MoveText(x + 5, y + height - 15)
                  .ShowText("SKU")
                  .MoveText(60, 0)
                  .ShowText("DESCRIPTION")
                  .MoveText(200, 0)
                  .ShowText("QTY")
                  .MoveText(40, 0)
                  .ShowText("PRICE")
                  .MoveText(60, 0)
                  .ShowText("SUBTOTAL")
                  .EndText();
            
            // Draw table rows
            var rowHeight = 18f;
            var currentY = y + height - 20 - rowHeight;
            var rowFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            
            foreach (DataRow row in dataTable.Rows)
            {
                context.CurrentDataRow = row;
                
                // Draw row background
                DrawRectangle(canvas, x, currentY, width, rowHeight, ColorConstants.WHITE, ColorConstants.BLACK);
                
                // Draw row data
                canvas.BeginText()
                      .SetFont(rowFont, 9)
                      .SetColor(ColorConstants.BLACK, true)
                      .MoveText(x + 5, currentY + 6)
                      .ShowText(row["SKU"]?.ToString() ?? "")
                      .MoveText(60, 0)
                      .ShowText(TruncateString(row["Description"]?.ToString() ?? "", 25))
                      .MoveText(200, 0)
                      .ShowText(row["Quantity"]?.ToString() ?? "")
                      .MoveText(40, 0)
                      .ShowText(string.Format("€{0:N2}", row["UnitPrice"]))
                      .MoveText(60, 0)
                      .ShowText(string.Format("€{0:N2}", row["Subtotal"]))
                      .EndText();
                
                currentY -= rowHeight;
                if (currentY < y) break; // Don't draw outside bounds
            }
        }

        private void RenderRectangle(PdfCanvas canvas, Document document, XElement rectangleElement, RenderContext context, float x, float y, float width, float height)
        {
            var backgroundColor = ExtractBackgroundColor(rectangleElement, context);
            var fillColor = ColorConstants.WHITE;
            
            if (!string.IsNullOrEmpty(backgroundColor))
            {
                fillColor = ParseColor(backgroundColor);
            }
            
            DrawRectangle(canvas, x, y, width, height, fillColor, ColorConstants.BLACK);
            
            // Handle nested ReportItems
            var reportItems = rectangleElement.Element(context.RdlcNamespace + "ReportItems");
            if (reportItems != null)
            {
                RenderReportItems(canvas, document, rectangleElement, context, x + width, y + height);
            }
        }

        private void RenderImage(PdfCanvas canvas, Document document, XElement imageElement, RenderContext context, float x, float y, float width, float height)
        {
            var sourceElement = imageElement.Element(context.RdlcNamespace + "Source");
            if (sourceElement?.Value == "Embedded")
            {
                var valueElement = imageElement.Element(context.RdlcNamespace + "Value");
                if (valueElement != null)
                {
                    var imageName = valueElement.Value;
                    var embeddedImage = context.RdlcDocument.Root?
                        .Element(context.RdlcNamespace + "EmbeddedImages")?
                        .Elements(context.RdlcNamespace + "EmbeddedImage")?
                        .FirstOrDefault(x => x.Attribute("Name")?.Value == imageName);
                    
                    if (embeddedImage != null)
                    {
                        var imageDataElement = embeddedImage.Element(context.RdlcNamespace + "ImageData");
                        if (imageDataElement != null)
                        {
                            try
                            {
                                var imageBytes = Convert.FromBase64String(imageDataElement.Value);
                                var imageData = iText.IO.Image.ImageDataFactory.Create(imageBytes);
                                var image = new iText.Layout.Element.Image(imageData);
                                
                                // Scale image to fit bounds
                                image.ScaleToFit(width, height);
                                image.SetFixedPosition(x, y);
                                
                                document.Add(image);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Image rendering error: {ex.Message}");
                                // Draw placeholder rectangle
                                DrawRectangle(canvas, x, y, width, height, ColorConstants.LIGHT_GRAY, ColorConstants.BLACK);
                            }
                        }
                    }
                }
            }
        }

        private void DrawRectangle(PdfCanvas canvas, float x, float y, float width, float height, iText.Kernel.Colors.Color fillColor, iText.Kernel.Colors.Color strokeColor)
        {
            canvas.SaveState()
                  .SetFillColor(fillColor)
                  .SetStrokeColor(strokeColor)
                  .SetLineWidth(0.5f)
                  .Rectangle(x, y, width, height)
                  .FillStroke()
                  .RestoreState();
        }

        private iText.Kernel.Colors.Color ParseColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return ColorConstants.BLACK;
                
            return colorString.ToLower() switch
            {
                "white" => ColorConstants.WHITE,
                "black" => ColorConstants.BLACK,
                "red" => ColorConstants.RED,
                "green" => ColorConstants.GREEN,
                "blue" => ColorConstants.BLUE,
                "yellow" => ColorConstants.YELLOW,
                "gray" or "grey" => ColorConstants.GRAY,
                "lightgray" or "lightgrey" => ColorConstants.LIGHT_GRAY,
                "#e8e8e8" => new DeviceRgb(232, 232, 232),
                "#f4d03f" => new DeviceRgb(244, 208, 63),
                "#4a9b8e" => new DeviceRgb(74, 155, 142),
                _ when colorString.StartsWith("#") => ParseHexColor(colorString),
                _ => ColorConstants.BLACK
            };
        }

        private iText.Kernel.Colors.Color ParseHexColor(string hex)
        {
            if (hex.Length == 7 && hex.StartsWith("#"))
            {
                try
                {
                    var r = Convert.ToInt32(hex.Substring(1, 2), 16);
                    var g = Convert.ToInt32(hex.Substring(3, 2), 16);
                    var b = Convert.ToInt32(hex.Substring(5, 2), 16);
                    return new DeviceRgb(r, g, b);
                }
                catch
                {
                    return ColorConstants.BLACK;
                }
            }
            return ColorConstants.BLACK;
        }

        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;
            return input.Substring(0, maxLength - 3) + "...";
        }

        private ItemBounds? ExtractBounds(XElement element, RenderContext context)
        {
            var topElement = element.Element(context.RdlcNamespace + "Top");
            var leftElement = element.Element(context.RdlcNamespace + "Left");
            var heightElement = element.Element(context.RdlcNamespace + "Height");
            var widthElement = element.Element(context.RdlcNamespace + "Width");

            if (topElement != null && leftElement != null && heightElement != null && widthElement != null)
            {
                return new ItemBounds
                {
                    Top = ParseDimension(topElement.Value),
                    Left = ParseDimension(leftElement.Value),
                    Height = ParseDimension(heightElement.Value),
                    Width = ParseDimension(widthElement.Value)
                };
            }

            return null;
        }

        private float ParseDimension(string dimension)
        {
            if (string.IsNullOrEmpty(dimension)) return 0;

            if (dimension.EndsWith("in"))
            {
                if (float.TryParse(dimension[..^2], out var inches))
                    return inches * 72; // Convert inches to points
            }
            else if (dimension.EndsWith("pt"))
            {
                if (float.TryParse(dimension[..^2], out var points))
                    return points;
            }
            else if (dimension.EndsWith("cm"))
            {
                if (float.TryParse(dimension[..^2], out var cm))
                    return cm * 28.35f; // Convert cm to points
            }

            // Try to parse as plain number (assume inches)
            if (float.TryParse(dimension, out var value))
                return value * 72;

            return 0;
        }

        private TextStyle ExtractTextStyle(XElement textboxElement, RenderContext context)
        {
            var style = new TextStyle();
            
            var styleElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")?
                .Element(context.RdlcNamespace + "Paragraph")?
                .Element(context.RdlcNamespace + "TextRuns")?
                .Element(context.RdlcNamespace + "TextRun")?
                .Element(context.RdlcNamespace + "Style");

            if (styleElement != null)
            {
                var fontSizeElement = styleElement.Element(context.RdlcNamespace + "FontSize");
                if (fontSizeElement != null && fontSizeElement.Value.EndsWith("pt"))
                {
                    if (float.TryParse(fontSizeElement.Value[..^2], out var fontSize))
                        style.FontSize = fontSize;
                }

                var fontWeightElement = styleElement.Element(context.RdlcNamespace + "FontWeight");
                if (fontWeightElement?.Value == "Bold")
                    style.Bold = true;

                var colorElement = styleElement.Element(context.RdlcNamespace + "Color");
                if (colorElement != null)
                    style.Color = colorElement.Value;
            }

            return style;
        }

        private string ExtractBackgroundColor(XElement element, RenderContext context)
        {
            var styleElement = element.Element(context.RdlcNamespace + "Style");
            if (styleElement == null) return "";
            
            var backgroundColorElement = styleElement.Element(context.RdlcNamespace + "BackgroundColor");
            if (backgroundColorElement == null) return "";
            
            return backgroundColorElement.Value;
        }

        private string ProcessTextValue(string value, RenderContext context)
        {
            if (string.IsNullOrEmpty(value)) return "";

            // Use the proper RdlcExpressionEvaluator
            var evaluator = new RdlcExpressionEvaluator();
            evaluator.SetDataSources(context.DataSources);
            evaluator.SetParameters(context.Parameters);
            
            // Set current row context for field evaluation
            if (context.CurrentDataRow != null)
            {
                var dataSetName = GetCurrentDataSetName(context);
                evaluator.SetCurrentRow(context.CurrentDataRow, dataSetName);
            }
            
            return evaluator.EvaluateExpression(value);
        }

        private string GetCurrentDataSetName(RenderContext context)
        {
            // Try to determine the current dataset name from the data sources
            // For now, use the first available dataset name
            return context.DataSources.Keys.FirstOrDefault() ?? "";
        }

        private class ItemBounds
        {
            public float Top { get; set; }
            public float Left { get; set; }
            public float Height { get; set; }
            public float Width { get; set; }
        }

        private class TextStyle
        {
            public float FontSize { get; set; } = 12;
            public bool Bold { get; set; }
            public string Color { get; set; } = "";
        }
    }
}