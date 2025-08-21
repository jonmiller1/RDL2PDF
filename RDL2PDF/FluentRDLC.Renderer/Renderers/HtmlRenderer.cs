using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ScottPlot;

namespace FluentRDLC.Renderer.Renderers
{
    public partial class HtmlRenderer : IRenderer
    {
        public RenderFormat Format => RenderFormat.HTML;

        public string GetFileExtension() => ".html";
        public string GetMimeType() => "text/html";

        public byte[] Render(RenderContext context)
        {
            var htmlContent = GenerateHtml(context);
            return Encoding.UTF8.GetBytes(htmlContent);
        }

        private string GenerateHtml(RenderContext context)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='en'>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine("<title>RDLC Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; font-size: 12px; margin: 20px; line-height: 1.4; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 15px 0; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
            html.AppendLine("tbody tr:nth-child(even) { background-color: #f9f9f9; }");
            html.AppendLine("tbody tr:hover { background-color: #f5f5f5; }");
            html.AppendLine("h1, h2, h3 { color: #333; margin: 20px 0 10px 0; }");
            html.AppendLine("h1 { font-size: 24px; }");
            html.AppendLine("h2 { font-size: 20px; }");
            html.AppendLine("h3 { font-size: 16px; }");
            html.AppendLine(".header { border-bottom: 2px solid #333; padding-bottom: 15px; margin-bottom: 25px; }");
            html.AppendLine(".footer { border-top: 2px solid #333; padding-top: 15px; margin-top: 25px; }");
            html.AppendLine(".chart-container { border: 1px solid #ddd; padding: 20px; margin: 15px 0; background-color: #fafafa; text-align: center; }");
            html.AppendLine(".chart-title { font-size: 18px; font-weight: bold; margin-bottom: 15px; color: #333; }");
            html.AppendLine(".chart-image { max-width: 100%; height: auto; border: 1px solid #ccc; }");
            html.AppendLine(".placeholder { color: #666; font-style: italic; }");
            html.AppendLine(".textbox { margin: 10px 0; }");
            html.AppendLine("@media print { body { margin: 0; } .header, .footer { page-break-inside: avoid; } }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Header
            var pageHeader = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageHeader");
            if (pageHeader != null)
            {
                html.AppendLine("<div class='header'>");
                RenderReportItemsToHtml(html, pageHeader, context);
                html.AppendLine("</div>");
            }

            // Body content
            var reportElement = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "Body");
            if (reportElement != null)
            {
                html.AppendLine("<main>");
                RenderReportItemsToHtml(html, reportElement, context);
                html.AppendLine("</main>");
            }

            // Footer
            var pageFooter = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageFooter");
            if (pageFooter != null)
            {
                html.AppendLine("<div class='footer'>");
                RenderReportItemsToHtml(html, pageFooter, context);
                html.AppendLine("</div>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private void RenderReportItemsToHtml(StringBuilder html, XElement parent, RenderContext context)
        {
            var reportItemsElement = parent.Element(context.RdlcNamespace + "ReportItems");
            if (reportItemsElement == null) return;

            foreach (var item in reportItemsElement.Elements())
            {
                if (item.Name.LocalName == "Textbox")
                {
                    RenderTextboxToHtml(html, item, context);
                }
                else if (item.Name.LocalName == "Tablix")
                {
                    RenderTablixToHtml(html, item, context);
                }
                else if (item.Name.LocalName == "Chart")
                {
                    RenderChartToHtml(html, item, context);
                }
                else if (item.Name.LocalName == "GaugePanel")
                {
                    RenderGaugePanelToHtml(html, item, context);
                }
                else if (item.Name.LocalName == "Image")
                {
                    RenderImageToHtml(html, item, context);
                }
            }
        }

        private void RenderTextboxToHtml(StringBuilder html, XElement textboxElement, RenderContext context)
        {
            var valueElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");

            if (valueElement != null)
            {
                var text = ProcessTextValue(valueElement.Value, context);
                html.AppendLine($"<div class='textbox'>{System.Web.HttpUtility.HtmlEncode(text)}</div>");
            }
        }

        private void RenderTablixToHtml(StringBuilder html, XElement tablixElement, RenderContext context)
        {
            var dataSetName = tablixElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return;

            var dataTable = context.DataSources[dataSetName];
            var headerElement = tablixElement.Element(context.RdlcNamespace + "TablixBody")?.Element(context.RdlcNamespace + "TablixRows")?.Elements().FirstOrDefault();
            var columnsElement = tablixElement.Element(context.RdlcNamespace + "TablixColumnHierarchy")?.Element(context.RdlcNamespace + "TablixMembers");

            if (headerElement != null && columnsElement != null)
            {
                html.AppendLine("<table>");

                // Header
                html.AppendLine("<thead><tr>");
                var headerCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements();
                if (headerCells != null)
                {
                    foreach (var cell in headerCells)
                    {
                        var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                        
                        var cellText = cellTextElement?.Value ?? "";
                        html.AppendLine($"<th>{System.Web.HttpUtility.HtmlEncode(cellText)}</th>");
                    }
                }
                html.AppendLine("</tr></thead>");

                // Data rows
                html.AppendLine("<tbody>");
                foreach (DataRow row in dataTable.Rows)
                {
                    context.CurrentDataRow = row;
                    html.AppendLine("<tr>");
                    
                    var dataCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements();
                    if (dataCells != null)
                    {
                        foreach (var cell in dataCells)
                        {
                            var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                            
                            var cellText = ProcessTextValue(cellTextElement?.Value ?? "", context);
                            html.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(cellText)}</td>");
                        }
                    }
                    html.AppendLine("</tr>");
                }
                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
            }
        }

        private void RenderChartToHtml(StringBuilder html, XElement chartElement, RenderContext context)
        {
            try
            {
                var chartData = ExtractChartData(chartElement, context);
                if (chartData != null)
                {
                    var chartImage = RenderChartToImage(chartData);
                    if (chartImage != null)
                    {
                        var base64Image = Convert.ToBase64String(chartImage);
                        html.AppendLine("<div class='chart-container'>");
                        if (!string.IsNullOrEmpty(chartData.Title))
                        {
                            html.AppendLine($"<div class='chart-title'>{System.Web.HttpUtility.HtmlEncode(chartData.Title)}</div>");
                        }
                        html.AppendLine($"<img src='data:image/png;base64,{base64Image}' alt='Chart' class='chart-image' />");
                        html.AppendLine("</div>");
                        return;
                    }
                }
            }
            catch
            {
                // Fall through to placeholder
            }

            var title = chartElement.Element(context.RdlcNamespace + "ChartAreas")?.Element(context.RdlcNamespace + "ChartArea")?.Element(context.RdlcNamespace + "ChartTitle")?.Element(context.RdlcNamespace + "Caption")?.Value ?? "Chart";
            
            html.AppendLine("<div class='chart-container'>");
            html.AppendLine($"<div class='chart-title'>{System.Web.HttpUtility.HtmlEncode(title)}</div>");
            html.AppendLine("<div class='placeholder'>Chart rendering failed</div>");
            html.AppendLine("</div>");
        }

        private void RenderGaugePanelToHtml(StringBuilder html, XElement gaugePanelElement, RenderContext context)
        {
            html.AppendLine("<div class='chart-container'>");
            html.AppendLine("<div class='chart-title'>Gauge</div>");
            html.AppendLine("<div class='placeholder'>Gauge rendering not supported</div>");
            html.AppendLine("</div>");
        }

        private void RenderImageToHtml(StringBuilder html, XElement imageElement, RenderContext context)
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
                        var mimeTypeElement = embeddedImage.Element(context.RdlcNamespace + "MIMEType");
                        
                        if (imageDataElement != null && mimeTypeElement != null)
                        {
                            try
                            {
                                var base64Data = imageDataElement.Value;
                                var mimeType = mimeTypeElement.Value;
                                html.AppendLine($"<div class='image-container'>");
                                html.AppendLine($"<img src='data:{mimeType};base64,{base64Data}' alt='{System.Web.HttpUtility.HtmlEncode(imageName)}' style='max-width: 100%; height: auto;' />");
                                html.AppendLine("</div>");
                                return;
                            }
                            catch
                            {
                                // Fall through to error message
                            }
                        }
                    }
                    
                    html.AppendLine($"<div class='placeholder'>Image could not be loaded: {System.Web.HttpUtility.HtmlEncode(imageName)}</div>");
                }
            }
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

        private byte[]? RenderChartToImage(ChartDefinition chartDef)
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