using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ScottPlot;
using DrawingColor = System.Drawing.Color;
using DrawingImage = System.Drawing.Image;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace FluentRDLC.Renderer.Renderers
{
    public partial class PngRenderer : IRenderer
    {
        public RenderFormat Format => RenderFormat.PNG;

        public string GetFileExtension() => ".png";
        public string GetMimeType() => "image/png";

        public byte[] Render(RenderContext context)
        {
            // Create a bitmap to render the report
            var width = Math.Max(context.Width, 800);
            var height = Math.Max(context.Height, 1000);
            
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(DrawingColor.White);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var font = new Font(new FontFamily("Arial"), 10);
            var titleFont = new Font(new FontFamily("Arial"), 14, DrawingFontStyle.Bold);
            var headerFont = new Font(new FontFamily("Arial"), 11, DrawingFontStyle.Bold);
            var brush = new SolidBrush(DrawingColor.Black);
            var headerBrush = new SolidBrush(DrawingColor.DarkBlue);
            var pen = new Pen(DrawingColor.Gray, 1);

            var currentY = 20f;
            var margin = 20f;

            try
            {
                // Header
                var pageHeader = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageHeader");
                if (pageHeader != null)
                {
                    currentY = RenderReportItemsToImage(graphics, pageHeader, context, margin, currentY, width - 2 * margin, headerFont, titleFont, brush, headerBrush, pen);
                    currentY += 20; // Add spacing
                }

                // Body content
                var reportElement = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "Body");
                if (reportElement != null)
                {
                    currentY = RenderReportItemsToImage(graphics, reportElement, context, margin, currentY, width - 2 * margin, font, titleFont, brush, headerBrush, pen);
                }

                // Footer
                var pageFooter = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageFooter");
                if (pageFooter != null)
                {
                    currentY += 20; // Add spacing
                    RenderReportItemsToImage(graphics, pageFooter, context, margin, currentY, width - 2 * margin, font, titleFont, brush, headerBrush, pen);
                }

                // Convert bitmap to PNG bytes
                using var stream = new MemoryStream();
                bitmap.Save(stream, DrawingImageFormat.Png);
                return stream.ToArray();
            }
            finally
            {
                font.Dispose();
                titleFont.Dispose();
                headerFont.Dispose();
                brush.Dispose();
                headerBrush.Dispose();
                pen.Dispose();
            }
        }

        private float RenderReportItemsToImage(Graphics graphics, XElement parent, RenderContext context, float x, float y, float maxWidth, Font font, Font titleFont, Brush brush, Brush headerBrush, Pen pen)
        {
            var reportItemsElement = parent.Element(context.RdlcNamespace + "ReportItems");
            if (reportItemsElement == null) return y;

            var currentY = y;

            foreach (var item in reportItemsElement.Elements())
            {
                if (item.Name.LocalName == "Textbox")
                {
                    currentY = RenderTextboxToImage(graphics, item, context, x, currentY, maxWidth, font, brush);
                }
                else if (item.Name.LocalName == "Tablix")
                {
                    currentY = RenderTablixToImage(graphics, item, context, x, currentY, maxWidth, font, brush, headerBrush, pen);
                }
                else if (item.Name.LocalName == "Chart")
                {
                    currentY = RenderChartToImage(graphics, item, context, x, currentY, maxWidth, titleFont, brush);
                }
                else if (item.Name.LocalName == "GaugePanel")
                {
                    currentY = RenderGaugePanelToImage(graphics, item, context, x, currentY, maxWidth, font, brush);
                }
                else if (item.Name.LocalName == "Image")
                {
                    currentY = RenderImageToImage(graphics, item, context, x, currentY, maxWidth, font, brush);
                }

                currentY += 10; // Add spacing between items
            }

            return currentY;
        }

        private float RenderTextboxToImage(Graphics graphics, XElement textboxElement, RenderContext context, float x, float y, float maxWidth, Font font, Brush brush)
        {
            var valueElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");

            if (valueElement != null)
            {
                var text = ProcessTextValue(valueElement.Value, context);
                var textRect = new RectangleF(x, y, maxWidth, 0);
                var size = graphics.MeasureString(text, font, (int)maxWidth);
                
                graphics.DrawString(text, font, brush, textRect);
                return y + size.Height + 5;
            }

            return y;
        }

        private float RenderTablixToImage(Graphics graphics, XElement tablixElement, RenderContext context, float x, float y, float maxWidth, Font font, Brush brush, Brush headerBrush, Pen pen)
        {
            var dataSetName = tablixElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return y;

            var dataTable = context.DataSources[dataSetName];
            var headerElement = tablixElement.Element(context.RdlcNamespace + "TablixBody")?.Element(context.RdlcNamespace + "TablixRows")?.Elements().FirstOrDefault();
            var columnsElement = tablixElement.Element(context.RdlcNamespace + "TablixColumnHierarchy")?.Element(context.RdlcNamespace + "TablixMembers");

            if (headerElement == null || columnsElement == null)
                return y;

            var columnCount = columnsElement.Elements().Count();
            if (columnCount == 0) return y;

            var columnWidth = maxWidth / columnCount;
            var rowHeight = 25f;
            var currentY = y;

            // Header row
            var headerCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements().ToArray();
            if (headerCells != null)
            {
                var headerRect = new RectangleF(x, currentY, maxWidth, rowHeight);
                graphics.FillRectangle(new SolidBrush(DrawingColor.LightGray), headerRect);
                graphics.DrawRectangle(pen, Rectangle.Round(headerRect));

                for (int i = 0; i < Math.Min(headerCells.Length, columnCount); i++)
                {
                    var cell = headerCells[i];
                    var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                    
                    var cellText = cellTextElement?.Value ?? "";
                    var cellRect = new RectangleF(x + i * columnWidth, currentY, columnWidth, rowHeight);
                    
                    graphics.DrawRectangle(pen, Rectangle.Round(cellRect));
                    
                    var textRect = new RectangleF(cellRect.X + 5, cellRect.Y + 5, cellRect.Width - 10, cellRect.Height - 10);
                    graphics.DrawString(cellText, font, headerBrush, textRect);
                }
                currentY += rowHeight;
            }

            // Data rows
            foreach (DataRow row in dataTable.Rows)
            {
                context.CurrentDataRow = row;
                
                for (int i = 0; i < Math.Min(headerCells?.Length ?? 0, columnCount); i++)
                {
                    var cell = headerCells![i];
                    var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                    
                    var cellText = ProcessTextValue(cellTextElement?.Value ?? "", context);
                    var cellRect = new RectangleF(x + i * columnWidth, currentY, columnWidth, rowHeight);
                    
                    graphics.DrawRectangle(pen, Rectangle.Round(cellRect));
                    
                    var textRect = new RectangleF(cellRect.X + 5, cellRect.Y + 5, cellRect.Width - 10, cellRect.Height - 10);
                    graphics.DrawString(cellText, font, brush, textRect);
                }
                currentY += rowHeight;
            }

            return currentY + 10;
        }

        private float RenderChartToImage(Graphics graphics, XElement chartElement, RenderContext context, float x, float y, float maxWidth, Font titleFont, Brush brush)
        {
            try
            {
                var chartData = ExtractChartData(chartElement, context);
                if (chartData != null)
                {
                    var chartImage = RenderChartToImageBytes(chartData);
                    if (chartImage != null)
                    {
                        using var stream = new MemoryStream(chartImage);
                        using var image = DrawingImage.FromStream(stream);
                        
                        var chartHeight = (int)(maxWidth * 0.6f); // Maintain aspect ratio
                        var chartWidth = (int)maxWidth;
                        
                        graphics.DrawImage(image, x, y, chartWidth, chartHeight);
                        return y + chartHeight + 10;
                    }
                }
            }
            catch
            {
                // Fall through to placeholder
            }

            var title = chartElement.Element(context.RdlcNamespace + "ChartAreas")?.Element(context.RdlcNamespace + "ChartArea")?.Element(context.RdlcNamespace + "ChartTitle")?.Element(context.RdlcNamespace + "Caption")?.Value ?? "Chart";
            
            var placeholderRect = new RectangleF(x, y, maxWidth, 100);
            graphics.FillRectangle(new SolidBrush(DrawingColor.LightGray), placeholderRect);
            graphics.DrawRectangle(new Pen(DrawingColor.Gray, 2), Rectangle.Round(placeholderRect));
            
            var titleSize = graphics.MeasureString(title, titleFont);
            var titleX = x + (maxWidth - titleSize.Width) / 2;
            var titleY = y + 20;
            graphics.DrawString(title, titleFont, brush, titleX, titleY);
            
            var placeholderText = "Chart rendering failed";
            var placeholderSize = graphics.MeasureString(placeholderText, titleFont);
            var placeholderX = x + (maxWidth - placeholderSize.Width) / 2;
            var placeholderY = titleY + titleSize.Height + 10;
            graphics.DrawString(placeholderText, titleFont, new SolidBrush(DrawingColor.Red), placeholderX, placeholderY);
            
            return y + 100 + 10;
        }

        private float RenderGaugePanelToImage(Graphics graphics, XElement gaugePanelElement, RenderContext context, float x, float y, float maxWidth, Font font, Brush brush)
        {
            var placeholderRect = new RectangleF(x, y, maxWidth, 60);
            graphics.FillRectangle(new SolidBrush(DrawingColor.LightBlue), placeholderRect);
            graphics.DrawRectangle(new Pen(DrawingColor.Blue, 1), Rectangle.Round(placeholderRect));
            
            var text = "Gauge rendering not supported";
            var textSize = graphics.MeasureString(text, font);
            var textX = x + (maxWidth - textSize.Width) / 2;
            var textY = y + (60 - textSize.Height) / 2;
            graphics.DrawString(text, font, brush, textX, textY);
            
            return y + 60 + 10;
        }

        private float RenderImageToImage(Graphics graphics, XElement imageElement, RenderContext context, float x, float y, float maxWidth, Font font, Brush brush)
        {
            var sourceElement = imageElement.Element(context.RdlcNamespace + "Source");
            if (sourceElement?.Value == "Embedded")
            {
                var valueElement = imageElement.Element(context.RdlcNamespace + "Value");
                if (valueElement != null)
                {
                    var imageName = valueElement.Value;
                    var embeddedImage = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "EmbeddedImages")?.Elements(context.RdlcNamespace + "EmbeddedImage")?.FirstOrDefault(x => x.Attribute("Name")?.Value == imageName);
                    
                    if (embeddedImage != null)
                    {
                        var imageDataElement = embeddedImage.Element(context.RdlcNamespace + "ImageData");
                        
                        if (imageDataElement != null)
                        {
                            try
                            {
                                var imageBytes = Convert.FromBase64String(imageDataElement.Value);
                                using var stream = new MemoryStream(imageBytes);
                                using var image = DrawingImage.FromStream(stream);
                                
                                var aspectRatio = (float)image.Height / image.Width;
                                var imageWidth = Math.Min(maxWidth, image.Width);
                                var imageHeight = imageWidth * aspectRatio;
                                
                                graphics.DrawImage(image, x, y, imageWidth, imageHeight);
                                return y + imageHeight + 10;
                            }
                            catch
                            {
                                // Fall through to error message
                            }
                        }
                    }
                    
                    var errorText = $"Image could not be loaded: {imageName}";
                    var errorSize = graphics.MeasureString(errorText, font);
                    graphics.DrawString(errorText, font, new SolidBrush(DrawingColor.Red), x, y);
                    return y + errorSize.Height + 10;
                }
            }

            return y;
        }

        private string ProcessTextValue(string value, RenderContext context)
        {
            if (string.IsNullOrEmpty(value)) return "";

            // Process field references
            value = FieldRegex().Replace(value, match =>
            {
                var fieldName = match.Groups[1].Value;
                if (context.CurrentDataRow != null && context.CurrentDataRow.Table.Columns.Contains(fieldName))
                {
                    return context.CurrentDataRow[fieldName]?.ToString() ?? "";
                }
                return match.Value;
            });

            // Process parameters
            value = ParameterRegex().Replace(value, match =>
            {
                var paramName = match.Groups[1].Value;
                if (context.Parameters.ContainsKey(paramName))
                {
                    return context.Parameters[paramName]?.ToString() ?? "";
                }
                return match.Value;
            });

            // Process page numbers
            value = PageNumberRegex().Replace(value, context.CurrentPageNumber.ToString());

            return value;
        }

        private ChartDefinition? ExtractChartData(XElement chartElement, RenderContext context)
        {
            var chartDataElement = chartElement.Element(context.RdlcNamespace + "ChartData");
            if (chartDataElement == null) return null;

            var dataSetName = chartElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return null;

            var dataTable = context.DataSources[dataSetName];
            var chartDef = new ChartDefinition
            {
                Title = chartElement.Element(context.RdlcNamespace + "ChartAreas")?.Element(context.RdlcNamespace + "ChartArea")?.Element(context.RdlcNamespace + "ChartTitle")?.Element(context.RdlcNamespace + "Caption")?.Value ?? "",
                ChartType = GetChartType(chartElement, context),
                Width = context.Width,
                Height = context.Height
            };

            var seriesData = new ChartSeriesData { Name = "Data" };
            
            foreach (DataRow row in dataTable.Rows)
            {
                if (dataTable.Columns.Count >= 2)
                {
                    var category = row[0]?.ToString() ?? "";
                    if (double.TryParse(row[1]?.ToString(), out double value))
                    {
                        seriesData.DataPoints.Add(new ChartDataPoint
                        {
                            Category = category,
                            Value = value,
                            Label = category
                        });
                        
                        if (!chartDef.Categories.Contains(category))
                        {
                            chartDef.Categories.Add(category);
                        }
                    }
                }
            }

            chartDef.Series.Add(seriesData);
            return chartDef;
        }

        private ChartType GetChartType(XElement chartElement, RenderContext context)
        {
            var chartTypeElement = chartElement.Element(context.RdlcNamespace + "ChartSeriesCollection")?.Element(context.RdlcNamespace + "ChartSeries")?.Element(context.RdlcNamespace + "Type");
            
            return chartTypeElement?.Value?.ToLower() switch
            {
                "column" => ChartType.Column,
                "bar" => ChartType.Bar,
                "line" => ChartType.Line,
                "pie" => ChartType.Pie,
                "area" => ChartType.Area,
                _ => ChartType.Column
            };
        }

        private byte[]? RenderChartToImageBytes(ChartDefinition chartDef)
        {
            try
            {
                var plt = new Plot();
                
                switch (chartDef.ChartType)
                {
                    case ChartType.Column:
                        CreateColumnChart(plt, chartDef);
                        break;
                    case ChartType.Bar:
                        CreateBarChart(plt, chartDef);
                        break;
                    case ChartType.Line:
                        CreateLineChart(plt, chartDef);
                        break;
                    case ChartType.Pie:
                        CreatePieChart(plt, chartDef);
                        break;
                    case ChartType.Area:
                        CreateAreaChart(plt, chartDef);
                        break;
                    default:
                        CreateColumnChart(plt, chartDef);
                        break;
                }
                
                var image = plt.GetImage(chartDef.Width, chartDef.Height);
                return image.GetImageBytes();
            }
            catch
            {
                return null;
            }
        }

        private void CreateColumnChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count > 0)
            {
                var series = chartDef.Series[0];
                var positions = Enumerable.Range(0, series.DataPoints.Count).Select(x => (double)x).ToArray();
                var values = series.DataPoints.Select(p => p.Value).ToArray();
                var labels = series.DataPoints.Select(p => p.Category).ToArray();

                plt.Add.Bars(positions, values);
                plt.Axes.Bottom.SetTicks(positions, labels);
                plt.Title(chartDef.Title);
            }
        }

        private void CreateBarChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count > 0)
            {
                var series = chartDef.Series[0];
                var positions = Enumerable.Range(0, series.DataPoints.Count).Select(x => (double)x).ToArray();
                var values = series.DataPoints.Select(p => p.Value).ToArray();
                var labels = series.DataPoints.Select(p => p.Category).ToArray();

                var bars = plt.Add.Bars(positions, values);
                bars.Horizontal = true;
                plt.Axes.Left.SetTicks(positions, labels);
                plt.Title(chartDef.Title);
            }
        }

        private void CreateLineChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count > 0)
            {
                var series = chartDef.Series[0];
                var positions = Enumerable.Range(0, series.DataPoints.Count).Select(x => (double)x).ToArray();
                var values = series.DataPoints.Select(p => p.Value).ToArray();
                var labels = series.DataPoints.Select(p => p.Category).ToArray();

                plt.Add.ScatterLine(positions, values);
                plt.Axes.Bottom.SetTicks(positions, labels);
                plt.Title(chartDef.Title);
            }
        }

        private void CreatePieChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count > 0)
            {
                var series = chartDef.Series[0];
                var values = series.DataPoints.Select(p => p.Value).ToArray();
                var labels = series.DataPoints.Select(p => p.Category).ToArray();

                var pie = plt.Add.Pie(values);
                plt.Title(chartDef.Title);
                plt.Axes.Frameless();
            }
        }

        private void CreateAreaChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count > 0)
            {
                var series = chartDef.Series[0];
                var positions = Enumerable.Range(0, series.DataPoints.Count).Select(x => (double)x).ToArray();
                var values = series.DataPoints.Select(p => p.Value).ToArray();
                var labels = series.DataPoints.Select(p => p.Category).ToArray();

                plt.Add.Polygon(positions.Zip(values, (x, y) => new ScottPlot.Coordinates(x, y)).ToArray());
                plt.Axes.Bottom.SetTicks(positions, labels);
                plt.Title(chartDef.Title);
            }
        }

        [GeneratedRegex(@"=Fields!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex FieldRegex();

        [GeneratedRegex(@"=Parameters!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex ParameterRegex();

        [GeneratedRegex(@"=PageNumber", RegexOptions.IgnoreCase)]
        private static partial Regex PageNumberRegex();
    }
}