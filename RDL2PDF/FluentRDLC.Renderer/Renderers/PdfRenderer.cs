using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ScottPlot;
using QPDFColors = QuestPDF.Helpers.Colors;

namespace FluentRDLC.Renderer.Renderers
{
    public partial class PdfRenderer : IRenderer
    {
        public RenderFormat Format => RenderFormat.PDF;

        public string GetFileExtension() => ".pdf";
        public string GetMimeType() => "application/pdf";

        public byte[] Render(RenderContext context)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(QPDFColors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    var pageHeader = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageHeader");
                    if (pageHeader != null)
                    {
                        page.Header().Container().Column(column =>
                        {
                            RenderReportItems(column, pageHeader, context);
                        });
                    }

                    // Footer  
                    var pageFooter = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageFooter");
                    if (pageFooter != null)
                    {
                        page.Footer().Container().Column(column =>
                        {
                            RenderReportItems(column, pageFooter, context);
                        });
                    }

                    page.Content().Column(column =>
                    {
                        var reportElement = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "Body");
                        if (reportElement != null)
                        {
                            RenderReportItems(column, reportElement, context);
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void RenderReportItems(ColumnDescriptor column, XElement parent, RenderContext context)
        {
            var reportItemsElement = parent.Element(context.RdlcNamespace + "ReportItems");
            if (reportItemsElement == null) return;

            foreach (var item in reportItemsElement.Elements())
            {
                if (item.Name.LocalName == "Textbox")
                {
                    RenderTextbox(column, item, context);
                }
                else if (item.Name.LocalName == "Tablix")
                {
                    RenderTablix(column, item, context);
                }
                else if (item.Name.LocalName == "Chart")
                {
                    RenderChart(column, item, context);
                }
                else if (item.Name.LocalName == "GaugePanel")
                {
                    RenderGaugePanel(column, item, context);
                }
                else if (item.Name.LocalName == "Image")
                {
                    RenderImage(column, item, context);
                }
            }
        }

        private void RenderTextbox(ColumnDescriptor column, XElement textboxElement, RenderContext context)
        {
            var valueElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");

            if (valueElement != null)
            {
                var text = ProcessTextValue(valueElement.Value, context);
                column.Item().Text(text);
            }
        }

        private void RenderTablix(ColumnDescriptor column, XElement tablixElement, RenderContext context)
        {
            var dataSetName = tablixElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return;

            var dataTable = context.DataSources[dataSetName];
            var headerElement = tablixElement.Element(context.RdlcNamespace + "TablixBody")?.Element(context.RdlcNamespace + "TablixRows")?.Elements().FirstOrDefault();
            var columnsElement = tablixElement.Element(context.RdlcNamespace + "TablixColumnHierarchy")?.Element(context.RdlcNamespace + "TablixMembers");

            if (headerElement != null && columnsElement != null)
            {
                column.Item().Table(table =>
                {
                    var columnCount = columnsElement.Elements().Count();
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < columnCount; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // Header
                    table.Header(header =>
                    {
                        var headerCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements();
                        if (headerCells != null)
                        {
                            foreach (var cell in headerCells)
                            {
                                var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                                
                                var cellText = cellTextElement?.Value ?? "";
                                header.Cell().Element(container =>
                                {
                                    container.Border(1).Padding(5).Text(cellText).Bold();
                                });
                            }
                        }
                    });

                    // Data rows
                    foreach (DataRow row in dataTable.Rows)
                    {
                        context.CurrentDataRow = row;
                        var dataCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements();
                        if (dataCells != null)
                        {
                            foreach (var cell in dataCells)
                            {
                                var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                                
                                var cellText = ProcessTextValue(cellTextElement?.Value ?? "", context);
                                table.Cell().Element(container =>
                                {
                                    container.Border(1).Padding(5).Text(cellText);
                                });
                            }
                        }
                    }
                });
            }
        }

        private void RenderChart(ColumnDescriptor column, XElement chartElement, RenderContext context)
        {
            try
            {
                var chartData = ExtractChartData(chartElement, context);
                if (chartData == null) return;

                var chartImage = RenderChartToImage(chartData);
                if (chartImage != null)
                {
                    column.Item().Image(chartImage);
                }
            }
            catch
            {
                column.Item().Text("Chart rendering failed").FontColor(QPDFColors.Red.Medium);
            }
        }

        private void RenderGaugePanel(ColumnDescriptor column, XElement gaugePanelElement, RenderContext context)
        {
            // Indicators are no longer supported after removing SkiaSharp
            column.Item().Text("Gauge rendering not supported").FontColor(QPDFColors.Grey.Medium);
        }

        private void RenderImage(ColumnDescriptor column, XElement imageElement, RenderContext context)
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
                                column.Item().Image(imageBytes);
                            }
                            catch
                            {
                                column.Item().Text("Image could not be loaded").FontColor(QPDFColors.Red.Medium);
                            }
                        }
                    }
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
            var dataSetName = GetDataSetName(chartElement, context);
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return null;

            var dataTable = context.DataSources[dataSetName];
            var chartDef = new ChartDefinition
            {
                Title = GetChartTitle(chartElement, context),
                ChartType = GetChartType(chartElement, context),
                Width = 600,
                Height = 400
            };

            foreach (DataRow row in dataTable.Rows)
            {
                context.CurrentDataRow = row;
                var seriesData = ParseChartSeriesFromData(chartElement, row, context);
                if (seriesData != null)
                {
                    var existingSeries = chartDef.Series.FirstOrDefault(s => s.Name == seriesData.Name);
                    if (existingSeries == null)
                    {
                        chartDef.Series.Add(seriesData);
                    }
                    else
                    {
                        existingSeries.DataPoints.AddRange(seriesData.DataPoints);
                    }
                }
            }

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

        private string GetDataSetName(XElement element, RenderContext context)
        {
            var dataSetName = element.Element(context.RdlcNamespace + "DataSetName");
            return dataSetName?.Value ?? "";
        }

        private string GetChartTitle(XElement chartElement, RenderContext context)
        {
            var title = chartElement.Descendants(context.RdlcNamespace + "Title").FirstOrDefault();
            var caption = title?.Element(context.RdlcNamespace + "Caption");
            if (caption != null)
            {
                return ProcessTextValue(caption.Value, context);
            }
            return "Chart";
        }

        private ChartSeriesData? ParseChartSeriesFromData(XElement chartElement, DataRow dataRow, RenderContext context)
        {
            var seriesData = new ChartSeriesData();
            seriesData.Name = "Series";

            var chartSeries = chartElement.Element(context.RdlcNamespace + "ChartSeriesCollection")?.Element(context.RdlcNamespace + "ChartSeries");
            if (chartSeries != null)
            {
                var nameElement = chartSeries.Element(context.RdlcNamespace + "Name");
                if (nameElement != null)
                {
                    seriesData.Name = ProcessTextValue(nameElement.Value, context);
                }

                var chartDataPoints = chartSeries.Element(context.RdlcNamespace + "ChartDataPoints");
                if (chartDataPoints != null)
                {
                    var dataPoint = chartDataPoints.Element(context.RdlcNamespace + "ChartDataPoint");
                    if (dataPoint != null)
                    {
                        var point = ParseDataPoint(dataPoint, dataRow, context);
                        if (point != null)
                        {
                            seriesData.DataPoints.Add(point);
                        }
                    }
                }
            }

            return seriesData.DataPoints.Count > 0 ? seriesData : null;
        }

        private ChartDataPoint? ParseDataPoint(XElement dataPointElement, DataRow dataRow, RenderContext context)
        {
            var dataValues = dataPointElement.Element(context.RdlcNamespace + "ChartDataPointValues");
            if (dataValues == null) return null;

            var yValue = dataValues.Element(context.RdlcNamespace + "Y");
            if (yValue == null) return null;

            var point = new ChartDataPoint();

            var yExpression = yValue.Value;
            var yValueStr = ProcessTextValue(yExpression, context);
            
            if (double.TryParse(yValueStr, out var yVal))
            {
                point.Value = yVal;
            }
            else
            {
                return null;
            }

            var xValue = dataValues.Element(context.RdlcNamespace + "X");
            if (xValue != null)
            {
                var xExpression = xValue.Value;
                point.Category = ProcessTextValue(xExpression, context);
            }

            return point;
        }
    }
}