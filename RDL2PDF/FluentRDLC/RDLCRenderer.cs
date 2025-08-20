using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FluentRDLC
{
    public enum ChartType
    {
        Column,
        Bar,
        Line,
        Pie,
        Area
    }

    public enum IndicatorType
    {
        Gauge,
        LinearGauge,
        DataBar,
        Sparkline
    }

    public class IndicatorDefinition
    {
        public IndicatorType Type { get; set; }
        public double Value { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string Title { get; set; } = "";
        public int Width { get; set; } = 200;
        public int Height { get; set; } = 100;
        public string Color { get; set; } = "Blue";
        public List<IndicatorRange> Ranges { get; set; } = [];
    }

    public class IndicatorRange
    {
        public double StartValue { get; set; }
        public double EndValue { get; set; }
        public string Color { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class ChartDefinition
    {
        public string Title { get; set; } = "";
        public ChartType ChartType { get; set; }
        public int Width { get; set; } = 600;
        public int Height { get; set; } = 400;
        public List<ChartSeriesData> Series { get; set; } = [];
        public List<string> Categories { get; set; } = [];
    }

    public class ChartSeriesData
    {
        public string Name { get; set; } = "";
        public List<ChartDataPoint> DataPoints { get; set; } = [];
    }

    public class ChartDataPoint
    {
        public double Value { get; set; }
        public string Category { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public partial class RDLCRenderer
    {
        private readonly Dictionary<string, DataTable> _dataSources;
        private readonly Dictionary<string, object> _parameters;
        private XDocument? _rdlcDocument;
        private XNamespace? _rdlcNamespace;
        private DataRow? _currentDataRow;
        private int _currentPageNumber = 1;
        private int _totalPages = 1;

        public RDLCRenderer()
        {
            _dataSources = [];
            _parameters = [];
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void AddDataSource(string name, DataTable dataTable)
        {
            _dataSources[name] = dataTable;
        }

        public void AddParameter(string name, object value)
        {
            _parameters[name] = value;
        }

        public byte[] RenderToPdf(string rdlcPath)
        {
            LoadRdlcFile(rdlcPath);
            return GeneratePdf();
        }

        public byte[] RenderToPdfFromContent(string rdlcContent)
        {
            LoadRdlcContent(rdlcContent);
            return GeneratePdf();
        }

        private void LoadRdlcFile(string rdlcPath)
        {
            var content = File.ReadAllText(rdlcPath);
            LoadRdlcContent(content);
        }

        private void LoadRdlcContent(string rdlcContent)
        {
            _rdlcDocument = XDocument.Parse(rdlcContent);
            _rdlcNamespace = _rdlcDocument.Root?.GetDefaultNamespace();
        }

        private byte[] GeneratePdf()
        {
            if (_rdlcDocument?.Root == null || _rdlcNamespace == null)
                throw new InvalidOperationException("RDLC document not loaded");

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // Header
                    var pageHeader = _rdlcDocument.Root.Element(_rdlcNamespace + "PageHeader");
                    if (pageHeader != null)
                    {
                        page.Header().Column(column =>
                        {
                            ProcessPageHeader(column, pageHeader);
                        });
                    }

                    // Footer  
                    var pageFooter = _rdlcDocument.Root.Element(_rdlcNamespace + "PageFooter");
                    if (pageFooter != null)
                    {
                        page.Footer().Column(column =>
                        {
                            ProcessPageFooter(column, pageFooter);
                        });
                    }

                    // Content
                    page.Content().Column(column =>
                    {
                        var reportElement = _rdlcDocument.Root.Element(_rdlcNamespace + "Body");
                        if (reportElement != null)
                        {
                            ProcessReportBody(column, reportElement);
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ProcessReportBody(ColumnDescriptor column, XElement bodyElement)
        {
            var reportItems = bodyElement.Element(_rdlcNamespace + "ReportItems");
            if (reportItems == null) return;

            foreach (var item in reportItems.Elements())
            {
                ProcessReportItem(column, item);
            }
        }

        private void ProcessReportItem(ColumnDescriptor column, XElement item)
        {
            switch (item.Name.LocalName)
            {
                case "Textbox":
                    ProcessTextbox(column, item);
                    break;
                case "Tablix":
                    ProcessTablix(column, item);
                    break;
                case "Table":
                    ProcessTable(column, item);
                    break;
                case "List":
                    ProcessList(column, item);
                    break;
                case "Rectangle":
                    ProcessRectangle(column, item);
                    break;
                case "Image":
                    ProcessImage(column, item);
                    break;
                case "Chart":
                    ProcessChart(column, item);
                    break;
                case "Line":
                    ProcessLine(column, item);
                    break;
                case "Gauge":
                case "LinearGauge":
                case "DataBar":
                case "Sparkline":
                    ProcessIndicator(column, item);
                    break;
                default:
                    Console.WriteLine($"Unsupported report item: {item.Name.LocalName}");
                    break;
            }
        }

        private void ProcessTextbox(ColumnDescriptor column, XElement textboxElement)
        {
            var paragraphs = textboxElement.Element(_rdlcNamespace + "Paragraphs");
            if (paragraphs == null) return;

            foreach (var paragraphElement in paragraphs.Elements(_rdlcNamespace + "Paragraph"))
            {
                var textRuns = paragraphElement.Element(_rdlcNamespace + "TextRuns");
                if (textRuns == null) continue;

                var combinedText = "";
                TextStyle? textStyle = null;

                foreach (var textRun in textRuns.Elements(_rdlcNamespace + "TextRun"))
                {
                    var valueElement = textRun.Element(_rdlcNamespace + "Value");
                    if (valueElement == null) continue;

                    var text = ProcessExpression(valueElement.Value);
                    textStyle = GetTextStyleFromElement(textRun.Element(_rdlcNamespace + "Style"));
                    combinedText += text;
                }

                if (!string.IsNullOrEmpty(combinedText))
                {
                    column.Item().Text(combinedText).Style(textStyle ?? DefaultTextStyle());
                    column.Item().PaddingBottom(5);
                }
            }
        }

        private void ProcessChart(ColumnDescriptor column, XElement chartElement)
        {
            try
            {
                var dataSetName = GetDataSetName(chartElement);
                if (string.IsNullOrEmpty(dataSetName) || !_dataSources.ContainsKey(dataSetName))
                {
                    column.Item().Text("[Chart: No data source found]").FontColor(Colors.Red.Medium);
                    return;
                }

                var dataTable = _dataSources[dataSetName];
                var chartData = ParseChartDefinition(chartElement, dataTable);

                if (chartData != null)
                {
                    var chartImage = RenderChartToImage(chartData);
                    if (chartImage != null)
                    {
                        column.Item().Image(chartImage).FitWidth();
                    }
                    else
                    {
                        column.Item().Text("[Chart: Rendering failed]").FontColor(Colors.Red.Medium);
                    }
                }
                else
                {
                    column.Item().Text("[Chart: Invalid configuration]").FontColor(Colors.Red.Medium);
                }
            }
            catch (Exception ex)
            {
                column.Item().Text($"[Chart Error: {ex.Message}]").FontColor(Colors.Red.Medium);
            }
        }

        private ChartDefinition? ParseChartDefinition(XElement chartElement, DataTable dataTable)
        {
            var chartSeries = chartElement.Element(_rdlcNamespace + "ChartSeries");
            if (chartSeries == null) return null;

            var chartDef = new ChartDefinition
            {
                Title = GetChartTitle(chartElement),
                ChartType = GetChartType(chartSeries),
                Width = 600,
                Height = 400
            };

            foreach (DataRow row in dataTable.Rows)
            {
                SetCurrentDataRow(row);
                var seriesData = ParseChartSeriesFromData(chartSeries, row);
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

        private string GetChartTitle(XElement chartElement)
        {
            var title = chartElement.Descendants(_rdlcNamespace + "Title").FirstOrDefault();
            var caption = title?.Element(_rdlcNamespace + "Caption");
            if (caption != null)
            {
                return ProcessExpression(caption.Value);
            }
            return "Chart";
        }

        private ChartType GetChartType(XElement chartSeries)
        {
            var firstSeries = chartSeries.Elements(_rdlcNamespace + "ChartSeries").FirstOrDefault();
            var type = firstSeries?.Element(_rdlcNamespace + "Type");

            return type?.Value?.ToLower() switch
            {
                "column" => ChartType.Column,
                "bar" => ChartType.Bar,
                "line" => ChartType.Line,
                "pie" => ChartType.Pie,
                "area" => ChartType.Area,
                _ => ChartType.Column
            };
        }

        private ChartSeriesData? ParseChartSeriesFromData(XElement seriesElement, DataRow dataRow)
        {
            var seriesData = new ChartSeriesData();

            var nameElement = seriesElement.Element(_rdlcNamespace + "ChartSeries")?.Element(_rdlcNamespace + "Name");
            seriesData.Name = nameElement != null ? ProcessExpression(nameElement.Value) : "Series";

            var chartDataPoints = seriesElement.Element(_rdlcNamespace + "ChartSeries")?.Element(_rdlcNamespace + "ChartDataPoints");
            if (chartDataPoints != null)
            {
                var dataPoint = chartDataPoints.Element(_rdlcNamespace + "ChartDataPoint");
                if (dataPoint != null)
                {
                    var point = ParseDataPoint(dataPoint, dataRow);
                    if (point != null)
                    {
                        seriesData.DataPoints.Add(point);
                    }
                }
            }

            return seriesData.DataPoints.Count > 0 ? seriesData : null;
        }

        private ChartDataPoint? ParseDataPoint(XElement dataPointElement, DataRow dataRow)
        {
            var dataValues = dataPointElement.Element(_rdlcNamespace + "ChartDataPointValues");
            if (dataValues == null) return null;

            var yValue = dataValues.Element(_rdlcNamespace + "Y");
            if (yValue == null) return null;

            var point = new ChartDataPoint();

            var yExpression = yValue.Value;
            var yValueStr = ProcessExpression(yExpression);
            if (double.TryParse(yValueStr, out var yVal))
            {
                point.Value = yVal;
            }
            else
            {
                return null;
            }

            var xValue = dataValues.Element(_rdlcNamespace + "X");
            if (xValue != null)
            {
                point.Category = ProcessExpression(xValue.Value);
            }

            return point;
        }

        private byte[]? RenderChartToImage(ChartDefinition chartDef)
        {
            var imageInfo = new SKImageInfo(chartDef.Width, chartDef.Height);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            switch (chartDef.ChartType)
            {
                case ChartType.Column:
                    RenderColumnChart(canvas, chartDef);
                    break;
                case ChartType.Bar:
                    RenderBarChart(canvas, chartDef);
                    break;
                case ChartType.Line:
                    RenderLineChart(canvas, chartDef);
                    break;
                case ChartType.Pie:
                    RenderPieChart(canvas, chartDef);
                    break;
                case ChartType.Area:
                    RenderAreaChart(canvas, chartDef);
                    break;
                default:
                    RenderColumnChart(canvas, chartDef);
                    break;
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private void RenderColumnChart(SKCanvas canvas, ChartDefinition chartDef)
        {
            var margin = 50f;
            var chartArea = new SKRect(margin, margin, chartDef.Width - margin, chartDef.Height - margin);

            DrawChartTitle(canvas, chartDef.Title, chartDef.Width);
            DrawAxes(canvas, chartArea);

            if (chartDef.Series.Count == 0) return;
            var series = chartDef.Series[0];
            if (series.DataPoints.Count == 0) return;

            var columnWidth = (chartArea.Width - 20) / series.DataPoints.Count * 0.8f;
            var maxValue = series.DataPoints.Max(p => p.Value);
            var valueScale = maxValue > 0 ? (chartArea.Height - 20) / maxValue : 1;

            using var paint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Fill };

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var point = series.DataPoints[i];
                var x = chartArea.Left + 10 + (i * (chartArea.Width - 20) / series.DataPoints.Count);
                var height = (float)(point.Value * valueScale);
                var y = chartArea.Bottom - height;

                var columnRect = new SKRect(x, y, x + columnWidth, chartArea.Bottom);
                canvas.DrawRect(columnRect, paint);

                using var textPaint = new SKPaint { Color = SKColors.Black };
                using var font = new SKFont { Size = 12 };
                canvas.DrawText(point.Value.ToString("F1"), x + columnWidth / 2, y - 5, SKTextAlign.Center, font, textPaint);

                if (!string.IsNullOrEmpty(point.Category))
                {
                    canvas.DrawText(point.Category, x + columnWidth / 2, chartArea.Bottom + 15, SKTextAlign.Center, font, textPaint);
                }
            }
        }

        private void RenderBarChart(SKCanvas canvas, ChartDefinition chartDef)
        {
            var margin = 50f;
            var chartArea = new SKRect(margin, margin, chartDef.Width - margin, chartDef.Height - margin);

            DrawChartTitle(canvas, chartDef.Title, chartDef.Width);

            if (chartDef.Series.Count == 0) return;
            var series = chartDef.Series[0];
            if (series.DataPoints.Count == 0) return;

            var barHeight = (chartArea.Height - 20) / series.DataPoints.Count * 0.8f;
            var maxValue = series.DataPoints.Max(p => p.Value);
            var valueScale = maxValue > 0 ? (chartArea.Width - 20) / maxValue : 1;

            using var paint = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill };

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var point = series.DataPoints[i];
                var y = chartArea.Top + 10 + (i * (chartArea.Height - 20) / series.DataPoints.Count);
                var width = (float)(point.Value * valueScale);

                var barRect = new SKRect(chartArea.Left, y, chartArea.Left + width, y + barHeight);
                canvas.DrawRect(barRect, paint);

                using var textPaint = new SKPaint { Color = SKColors.Black };
                using var font = new SKFont { Size = 12 };

                if (!string.IsNullOrEmpty(point.Category))
                {
                    canvas.DrawText(point.Category, chartArea.Left - 40, y + barHeight / 2, SKTextAlign.Left, font, textPaint);
                }
            }
        }

        private void RenderLineChart(SKCanvas canvas, ChartDefinition chartDef)
        {
            var margin = 50f;
            var chartArea = new SKRect(margin, margin, chartDef.Width - margin, chartDef.Height - margin);

            DrawChartTitle(canvas, chartDef.Title, chartDef.Width);
            DrawAxes(canvas, chartArea);

            if (chartDef.Series.Count == 0) return;
            var series = chartDef.Series[0];
            if (series.DataPoints.Count < 2) return;

            var maxValue = series.DataPoints.Max(p => p.Value);
            var valueScale = maxValue > 0 ? (chartArea.Height - 20) / maxValue : 1;

            using var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var path = new SKPath();

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var point = series.DataPoints[i];
                var x = chartArea.Left + 10 + (i * (chartArea.Width - 20) / (series.DataPoints.Count - 1));
                var y = chartArea.Bottom - (float)(point.Value * valueScale);

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);

                using var pointPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };
                canvas.DrawCircle(x, y, 3, pointPaint);
            }

            canvas.DrawPath(path, paint);
        }

        private void RenderPieChart(SKCanvas canvas, ChartDefinition chartDef)
        {
            var center = new SKPoint(chartDef.Width / 2f, chartDef.Height / 2f);
            var radius = Math.Min(chartDef.Width, chartDef.Height) / 3f;

            DrawChartTitle(canvas, chartDef.Title, chartDef.Width);

            if (chartDef.Series.Count == 0) return;
            var series = chartDef.Series[0];
            if (series.DataPoints.Count == 0) return;

            var total = series.DataPoints.Sum(p => p.Value);
            var startAngle = 0f;
            var colors = new[] { SKColors.Blue, SKColors.Red, SKColors.Green, SKColors.Orange, SKColors.Purple };

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var point = series.DataPoints[i];
                var sweepAngle = (float)(360 * point.Value / total);

                using var paint = new SKPaint
                {
                    Color = colors[i % colors.Length],
                    Style = SKPaintStyle.Fill
                };

                var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
                canvas.DrawArc(rect, startAngle, sweepAngle, true, paint);
                startAngle += sweepAngle;
            }
        }

        private void RenderAreaChart(SKCanvas canvas, ChartDefinition chartDef)
        {
            var margin = 50f;
            var chartArea = new SKRect(margin, margin, chartDef.Width - margin, chartDef.Height - margin);

            DrawChartTitle(canvas, chartDef.Title, chartDef.Width);
            DrawAxes(canvas, chartArea);

            if (chartDef.Series.Count == 0) return;
            var series = chartDef.Series[0];
            if (series.DataPoints.Count < 2) return;

            var maxValue = series.DataPoints.Max(p => p.Value);
            var valueScale = maxValue > 0 ? (chartArea.Height - 20) / maxValue : 1;

            using var path = new SKPath();
            path.MoveTo(chartArea.Left + 10, chartArea.Bottom);

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var point = series.DataPoints[i];
                var x = chartArea.Left + 10 + (i * (chartArea.Width - 20) / (series.DataPoints.Count - 1));
                var y = chartArea.Bottom - (float)(point.Value * valueScale);
                path.LineTo(x, y);
            }

            path.LineTo(chartArea.Right - 10, chartArea.Bottom);
            path.Close();

            using var paint = new SKPaint { Color = SKColors.LightBlue, Style = SKPaintStyle.Fill };
            canvas.DrawPath(path, paint);
        }

        private void DrawChartTitle(SKCanvas canvas, string title, int width)
        {
            using var paint = new SKPaint { Color = SKColors.Black };
            using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 16);
            canvas.DrawText(title, width / 2f, 30, SKTextAlign.Center, font, paint);
        }

        private void DrawAxes(SKCanvas canvas, SKRect chartArea)
        {
            using var paint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            canvas.DrawLine(chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom, paint);
            canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, paint);
        }

        private void ProcessTablix(ColumnDescriptor column, XElement tablixElement)
        {
            var dataSetName = GetDataSetName(tablixElement);
            if (string.IsNullOrEmpty(dataSetName) || !_dataSources.TryGetValue(dataSetName, out var dataTable))
                return;

            var tablixBody = tablixElement.Element(_rdlcNamespace + "TablixBody");
            var tablixColumnHierarchy = tablixElement.Element(_rdlcNamespace + "TablixColumnHierarchy");

            if (tablixBody == null) return;

            var columnCount = GetColumnCount(tablixColumnHierarchy);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        columns.RelativeColumn();
                    }
                });

                ProcessTablixHeader(table, tablixColumnHierarchy, tablixBody);
                ProcessTablixData(table, dataTable, tablixBody);
            });
        }

        private void ProcessTable(ColumnDescriptor column, XElement tableElement)
        {
            var dataSetName = GetDataSetName(tableElement);
            if (string.IsNullOrEmpty(dataSetName) || !_dataSources.TryGetValue(dataSetName, out var dataTable))
                return;

            var tableColumns = tableElement.Element(_rdlcNamespace + "TableColumns");
            var header = tableElement.Element(_rdlcNamespace + "Header");
            var details = tableElement.Element(_rdlcNamespace + "Details");

            if (tableColumns == null) return;

            var columnCount = tableColumns.Elements(_rdlcNamespace + "TableColumn").Count();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        columns.RelativeColumn();
                    }
                });

                if (header != null)
                {
                    ProcessTableHeader(table, header);
                }

                if (details != null)
                {
                    ProcessTableDetails(table, details, dataTable);
                }
            });
        }

        private void ProcessList(ColumnDescriptor column, XElement listElement)
        {
            var dataSetName = GetDataSetName(listElement);
            if (string.IsNullOrEmpty(dataSetName) || !_dataSources.TryGetValue(dataSetName, out var dataTable))
                return;

            var contents = listElement.Element(_rdlcNamespace + "Contents");
            if (contents == null) return;

            foreach (DataRow row in dataTable.Rows)
            {
                SetCurrentDataRow(row);
                column.Item().Column(subColumn =>
                {
                    ProcessReportItems(subColumn, contents);
                });
                column.Item().PaddingBottom(10);
            }
        }

        private void ProcessRectangle(ColumnDescriptor column, XElement rectangleElement)
        {
            var reportItems = rectangleElement.Element(_rdlcNamespace + "ReportItems");
            if (reportItems != null)
            {
                column.Item().BorderColor(Colors.Grey.Medium).Border(1).Padding(5).Column(subColumn =>
                {
                    foreach (var item in reportItems.Elements())
                    {
                        ProcessReportItem(subColumn, item);
                    }
                });
            }
        }

        private void ProcessImage(ColumnDescriptor column, XElement imageElement)
        {
            try
            {
                var source = imageElement.Element(_rdlcNamespace + "Source");
                var value = imageElement.Element(_rdlcNamespace + "Value");

                if (source?.Value == "External" && value != null)
                {
                    var imagePath = ProcessExpression(value.Value);
                    if (File.Exists(imagePath))
                    {
                        column.Item().Image(imagePath).FitWidth();
                    }
                    else
                    {
                        column.Item().Text($"[Image not found: {imagePath}]").FontColor(Colors.Red.Medium);
                    }
                }
                else
                {
                    column.Item().Text($"[Image: {source?.Value ?? "Unknown"}]").FontColor(Colors.Grey.Medium);
                }
            }
            catch (Exception ex)
            {
                column.Item().Text($"[Image Error: {ex.Message}]").FontColor(Colors.Red.Medium);
            }
        }

        private void ProcessLine(ColumnDescriptor column, XElement lineElement)
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Black);
        }

        private void ProcessReportItems(ColumnDescriptor column, XElement container)
        {
            var reportItems = container.Element(_rdlcNamespace + "ReportItems");
            if (reportItems == null) return;

            foreach (var item in reportItems.Elements())
            {
                ProcessReportItem(column, item);
            }
        }

        private void ProcessTablixHeader(TableDescriptor table, XElement? columnHierarchy, XElement tablixBody)
        {
            var rows = tablixBody.Element(_rdlcNamespace + "TablixRows");
            if (rows == null) return;

            var firstRow = rows.Elements(_rdlcNamespace + "TablixRow").FirstOrDefault();
            if (firstRow == null) return;

            var cells = firstRow.Element(_rdlcNamespace + "TablixCells");
            if (cells == null) return;

            table.Header(header =>
            {
                foreach (var cell in cells.Elements(_rdlcNamespace + "TablixCell"))
                {
                    var cellContents = cell.Element(_rdlcNamespace + "CellContents");
                    var textbox = cellContents?.Element(_rdlcNamespace + "Textbox");

                    if (textbox != null)
                    {
                        var text = GetTextboxValue(textbox);
                        header.Cell().Element(container => CellStyle(container)).Text(text).Style(HeaderTextStyle());
                    }
                    else
                    {
                        header.Cell().Element(container => CellStyle(container)).Text("");
                    }
                }
            });
        }

        private void ProcessTablixData(TableDescriptor table, DataTable dataTable, XElement tablixBody)
        {
            var rows = tablixBody.Element(_rdlcNamespace + "TablixRows");
            var dataRowElement = rows?.Elements(_rdlcNamespace + "TablixRow").Skip(1).FirstOrDefault();

            if (dataRowElement == null) return;

            foreach (DataRow row in dataTable.Rows)
            {
                SetCurrentDataRow(row);

                var cells = dataRowElement.Element(_rdlcNamespace + "TablixCells");
                if (cells != null)
                {
                    foreach (var cell in cells.Elements(_rdlcNamespace + "TablixCell"))
                    {
                        var cellContents = cell.Element(_rdlcNamespace + "CellContents");
                        var textbox = cellContents?.Element(_rdlcNamespace + "Textbox");

                        if (textbox != null)
                        {
                            var text = GetTextboxValue(textbox);
                            var processedText = ProcessExpression(text);
                            table.Cell().Element(container => CellStyle(container)).Text(processedText);
                        }
                        else
                        {
                            table.Cell().Element(container => CellStyle(container)).Text("");
                        }
                    }
                }
            }
        }

        private void ProcessTableHeader(TableDescriptor table, XElement header)
        {
            var tableRows = header.Element(_rdlcNamespace + "TableRows");
            if (tableRows == null) return;

            table.Header(headerSection =>
            {
                foreach (var row in tableRows.Elements(_rdlcNamespace + "TableRow"))
                {
                    var tableCells = row.Element(_rdlcNamespace + "TableCells");
                    if (tableCells == null) continue;

                    foreach (var cell in tableCells.Elements(_rdlcNamespace + "TableCell"))
                    {
                        var reportItems = cell.Element(_rdlcNamespace + "ReportItems");
                        var textbox = reportItems?.Element(_rdlcNamespace + "Textbox");

                        if (textbox != null)
                        {
                            var text = GetTextboxValue(textbox);
                            headerSection.Cell().Element(container => CellStyle(container)).Text(text).Style(HeaderTextStyle());
                        }
                        else
                        {
                            headerSection.Cell().Element(container => CellStyle(container)).Text("");
                        }
                    }
                }
            });
        }

        private void ProcessTableDetails(TableDescriptor table, XElement details, DataTable dataTable)
        {
            var tableRows = details.Element(_rdlcNamespace + "TableRows");
            if (tableRows == null) return;

            foreach (DataRow dataRow in dataTable.Rows)
            {
                SetCurrentDataRow(dataRow);

                foreach (var row in tableRows.Elements(_rdlcNamespace + "TableRow"))
                {
                    var tableCells = row.Element(_rdlcNamespace + "TableCells");
                    if (tableCells == null) continue;

                    foreach (var cell in tableCells.Elements(_rdlcNamespace + "TableCell"))
                    {
                        var reportItems = cell.Element(_rdlcNamespace + "ReportItems");
                        var textbox = reportItems?.Element(_rdlcNamespace + "Textbox");

                        if (textbox != null)
                        {
                            var text = GetTextboxValue(textbox);
                            var processedText = ProcessExpression(text);
                            table.Cell().Element(container => CellStyle(container)).Text(processedText);
                        }
                        else
                        {
                            table.Cell().Element(container => CellStyle(container)).Text("");
                        }
                    }
                }
            }
        }

        private string GetDataSetName(XElement element)
        {
            var dataSetName = element.Element(_rdlcNamespace + "DataSetName");
            return dataSetName?.Value ?? "";
        }

        private int GetColumnCount(XElement? columnHierarchy)
        {
            if (columnHierarchy == null) return 1;

            var members = columnHierarchy.Descendants(_rdlcNamespace + "TablixMember");
            return Math.Max(1, members.Count());
        }

        private string GetTextboxValue(XElement textbox)
        {
            var paragraphs = textbox.Element(_rdlcNamespace + "Paragraphs");
            if (paragraphs == null) return "";

            var paragraph = paragraphs.Element(_rdlcNamespace + "Paragraph");
            if (paragraph == null) return "";

            var textRuns = paragraph.Element(_rdlcNamespace + "TextRuns");
            if (textRuns == null) return "";

            var textRun = textRuns.Element(_rdlcNamespace + "TextRun");
            if (textRun == null) return "";

            var value = textRun.Element(_rdlcNamespace + "Value");
            return value?.Value ?? "";
        }

        private TextStyle GetTextStyleFromElement(XElement? styleElement)
        {
            var style = TextStyle.Default;

            if (styleElement != null)
            {
                var fontSizeElement = styleElement.Element(_rdlcNamespace + "FontSize");
                if (fontSizeElement != null && double.TryParse(fontSizeElement.Value.Replace("pt", ""), out var size))
                {
                    style = style.FontSize((float)size);
                }

                var fontWeightElement = styleElement.Element(_rdlcNamespace + "FontWeight");
                if (fontWeightElement?.Value == "Bold")
                {
                    style = style.Bold();
                }

                var fontStyleElement = styleElement.Element(_rdlcNamespace + "FontStyle");
                if (fontStyleElement?.Value == "Italic")
                {
                    style = style.Italic();
                }

                var fontFamilyElement = styleElement.Element(_rdlcNamespace + "FontFamily");
                if (fontFamilyElement != null)
                {
                    style = style.FontFamily(fontFamilyElement.Value);
                }

                var colorElement = styleElement.Element(_rdlcNamespace + "Color");
                if (colorElement != null)
                {
                    style = style.FontColor(ParseColor(colorElement.Value));
                }
            }

            return style;
        }

        private static TextStyle DefaultTextStyle()
        {
            return TextStyle.Default.FontSize(12).FontFamily("Arial");
        }

        private static TextStyle HeaderTextStyle()
        {
            return TextStyle.Default.FontSize(12).FontFamily("Arial").Bold();
        }

        private static string ParseColor(string colorValue)
        {
            if (colorValue.StartsWith('#'))
                return colorValue;

            return colorValue.ToLower() switch
            {
                "red" => Colors.Red.Medium,
                "blue" => Colors.Blue.Medium,
                "green" => Colors.Green.Medium,
                "black" => Colors.Black,
                "white" => Colors.White,
                "gray" or "grey" => Colors.Grey.Medium,
                _ => Colors.Black
            };
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(5)
                .AlignMiddle();
        }

        private void SetCurrentDataRow(DataRow row)
        {
            _currentDataRow = row;
        }

        [GeneratedRegex(@"=Fields!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex FieldRegex();

        [GeneratedRegex(@"=Parameters!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex ParameterRegex();

        private void ProcessPageHeader(ColumnDescriptor column, XElement pageHeader)
        {
            var reportItems = pageHeader.Element(_rdlcNamespace + "ReportItems");
            if (reportItems == null) return;

            foreach (var item in reportItems.Elements())
            {
                ProcessReportItem(column, item);
            }
        }

        private void ProcessPageFooter(ColumnDescriptor column, XElement pageFooter)
        {
            var reportItems = pageFooter.Element(_rdlcNamespace + "ReportItems");
            if (reportItems == null) return;

            foreach (var item in reportItems.Elements())
            {
                ProcessReportItem(column, item);
            }
        }

        private void ProcessIndicator(ColumnDescriptor column, XElement indicatorElement)
        {
            try
            {
                var dataSetName = GetDataSetName(indicatorElement);
                var indicatorData = ParseIndicatorDefinition(indicatorElement, dataSetName);

                if (indicatorData != null)
                {
                    var indicatorImage = RenderIndicatorToImage(indicatorData);
                    if (indicatorImage != null)
                    {
                        column.Item().Image(indicatorImage).FitWidth();
                    }
                    else
                    {
                        column.Item().Text("[Indicator: Rendering failed]").FontColor(Colors.Red.Medium);
                    }
                }
                else
                {
                    column.Item().Text("[Indicator: Invalid configuration]").FontColor(Colors.Red.Medium);
                }
            }
            catch (Exception ex)
            {
                column.Item().Text($"[Indicator Error: {ex.Message}]").FontColor(Colors.Red.Medium);
            }
        }

        private IndicatorDefinition? ParseIndicatorDefinition(XElement indicatorElement, string dataSetName)
        {
            var indicator = new IndicatorDefinition
            {
                Type = GetIndicatorType(indicatorElement),
                Width = 200,
                Height = 100
            };

            // Get value from data or expression
            var valueElement = indicatorElement.Element(_rdlcNamespace + "Value");
            if (valueElement != null)
            {
                var valueStr = ProcessExpression(valueElement.Value);
                if (double.TryParse(valueStr, out var value))
                {
                    indicator.Value = value;
                }
            }

            // Get min/max values
            var minElement = indicatorElement.Element(_rdlcNamespace + "MinimumValue");
            if (minElement != null && double.TryParse(ProcessExpression(minElement.Value), out var min))
            {
                indicator.MinValue = min;
            }

            var maxElement = indicatorElement.Element(_rdlcNamespace + "MaximumValue");
            if (maxElement != null && double.TryParse(ProcessExpression(maxElement.Value), out var max))
            {
                indicator.MaxValue = max;
            }

            // Parse indicator ranges for color coding
            var statesElement = indicatorElement.Element(_rdlcNamespace + "IndicatorStates");
            if (statesElement != null)
            {
                foreach (var state in statesElement.Elements(_rdlcNamespace + "IndicatorState"))
                {
                    var startValue = state.Element(_rdlcNamespace + "StartValue");
                    var endValue = state.Element(_rdlcNamespace + "EndValue");
                    var color = state.Element(_rdlcNamespace + "Color");

                    if (startValue != null && endValue != null)
                    {
                        var range = new IndicatorRange
                        {
                            StartValue = double.TryParse(ProcessExpression(startValue.Value), out var start) ? start : 0,
                            EndValue = double.TryParse(ProcessExpression(endValue.Value), out var end) ? end : 100,
                            Color = color != null ? ProcessExpression(color.Value) : "Blue"
                        };
                        indicator.Ranges.Add(range);
                    }
                }
            }

            return indicator;
        }

        private IndicatorType GetIndicatorType(XElement indicatorElement)
        {
            return indicatorElement.Name.LocalName.ToLower() switch
            {
                "gauge" => IndicatorType.Gauge,
                "lineargauge" => IndicatorType.LinearGauge,
                "databar" => IndicatorType.DataBar,
                "sparkline" => IndicatorType.Sparkline,
                _ => IndicatorType.Gauge
            };
        }

        private byte[]? RenderIndicatorToImage(IndicatorDefinition indicator)
        {
            var imageInfo = new SKImageInfo(indicator.Width, indicator.Height);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            switch (indicator.Type)
            {
                case IndicatorType.Gauge:
                    RenderGauge(canvas, indicator);
                    break;
                case IndicatorType.LinearGauge:
                    RenderLinearGauge(canvas, indicator);
                    break;
                case IndicatorType.DataBar:
                    RenderDataBar(canvas, indicator);
                    break;
                case IndicatorType.Sparkline:
                    RenderSparkline(canvas, indicator);
                    break;
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private void RenderGauge(SKCanvas canvas, IndicatorDefinition indicator)
        {
            var center = new SKPoint(indicator.Width / 2f, indicator.Height * 0.8f);
            var radius = Math.Min(indicator.Width, indicator.Height) / 3f;
            var startAngle = 180f;
            var sweepAngle = 180f;
            
            // Background arc
            using (var backgroundPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 20 })
            {
                var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
                canvas.DrawArc(rect, startAngle, sweepAngle, false, backgroundPaint);
            }

            // Value arc
            var valueRange = indicator.MaxValue - indicator.MinValue;
            var valuePercent = valueRange > 0 ? (indicator.Value - indicator.MinValue) / valueRange : 0;
            var valueSweep = (float)(sweepAngle * valuePercent);
            
            var color = GetIndicatorColor(indicator.Value, indicator.Ranges);
            using (var valuePaint = new SKPaint { Color = ParseSKColor(color), Style = SKPaintStyle.Stroke, StrokeWidth = 15 })
            {
                var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
                canvas.DrawArc(rect, startAngle, valueSweep, false, valuePaint);
            }

            // Needle
            var needleAngle = startAngle + valueSweep;
            var needleEndX = center.X + (radius * 0.8f) * (float)Math.Cos(Math.PI * needleAngle / 180);
            var needleEndY = center.Y + (radius * 0.8f) * (float)Math.Sin(Math.PI * needleAngle / 180);
            
            using (var needlePaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 3 })
            {
                canvas.DrawLine(center, new SKPoint(needleEndX, needleEndY), needlePaint);
            }

            // Center dot
            using (var centerPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill })
            {
                canvas.DrawCircle(center, 5, centerPaint);
            }

            // Value text
            using (var textPaint = new SKPaint { Color = SKColors.Black })
            using (var font = new SKFont { Size = 14 })
            {
                canvas.DrawText(indicator.Value.ToString("F1"), center.X, center.Y + 30, SKTextAlign.Center, font, textPaint);
            }
        }

        private void RenderLinearGauge(SKCanvas canvas, IndicatorDefinition indicator)
        {
            var margin = 20f;
            var gaugeRect = new SKRect(margin, indicator.Height / 2f - 15, indicator.Width - margin, indicator.Height / 2f + 15);
            
            // Background
            using (var backgroundPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(gaugeRect, backgroundPaint);
            }

            // Value fill
            var valueRange = indicator.MaxValue - indicator.MinValue;
            var valuePercent = valueRange > 0 ? (indicator.Value - indicator.MinValue) / valueRange : 0;
            var valueWidth = (float)(gaugeRect.Width * valuePercent);
            
            var color = GetIndicatorColor(indicator.Value, indicator.Ranges);
            using (var valuePaint = new SKPaint { Color = ParseSKColor(color), Style = SKPaintStyle.Fill })
            {
                var valueRect = new SKRect(gaugeRect.Left, gaugeRect.Top, gaugeRect.Left + valueWidth, gaugeRect.Bottom);
                canvas.DrawRect(valueRect, valuePaint);
            }

            // Border
            using (var borderPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
            {
                canvas.DrawRect(gaugeRect, borderPaint);
            }

            // Value text
            using (var textPaint = new SKPaint { Color = SKColors.Black })
            using (var font = new SKFont { Size = 12 })
            {
                canvas.DrawText(indicator.Value.ToString("F1"), indicator.Width / 2f, gaugeRect.Bottom + 20, SKTextAlign.Center, font, textPaint);
            }
        }

        private void RenderDataBar(SKCanvas canvas, IndicatorDefinition indicator)
        {
            var margin = 10f;
            var barHeight = indicator.Height - (2 * margin);
            var maxBarWidth = indicator.Width - (2 * margin);
            
            var valueRange = indicator.MaxValue - indicator.MinValue;
            var valuePercent = valueRange > 0 ? Math.Max(0, Math.Min(1, (indicator.Value - indicator.MinValue) / valueRange)) : 0;
            var barWidth = (float)(maxBarWidth * valuePercent);
            
            var color = GetIndicatorColor(indicator.Value, indicator.Ranges);
            using (var barPaint = new SKPaint { Color = ParseSKColor(color), Style = SKPaintStyle.Fill })
            {
                var barRect = new SKRect(margin, margin, margin + barWidth, margin + barHeight);
                canvas.DrawRect(barRect, barPaint);
            }
        }

        private void RenderSparkline(SKCanvas canvas, IndicatorDefinition indicator)
        {
            // For sparkline, we'd typically need historical data points
            // This is a simplified version showing a trend line
            var points = new List<SKPoint>();
            var dataCount = 10; // Simulate 10 data points
            var random = new Random(42); // Fixed seed for consistent output
            
            for (int i = 0; i < dataCount; i++)
            {
                var x = (float)(indicator.Width * i / (dataCount - 1));
                var randomValue = indicator.MinValue + (random.NextDouble() * (indicator.MaxValue - indicator.MinValue));
                var y = (float)(indicator.Height - (indicator.Height * (randomValue - indicator.MinValue) / (indicator.MaxValue - indicator.MinValue)));
                points.Add(new SKPoint(x, y));
            }

            if (points.Count > 1)
            {
                using var path = new SKPath();
                path.MoveTo(points[0]);
                for (int i = 1; i < points.Count; i++)
                {
                    path.LineTo(points[i]);
                }

                using var paint = new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                canvas.DrawPath(path, paint);
                
                // Highlight current value point
                var currentX = points.Last().X;
                var currentY = (float)(indicator.Height - (indicator.Height * (indicator.Value - indicator.MinValue) / (indicator.MaxValue - indicator.MinValue)));
                using var pointPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill };
                canvas.DrawCircle(currentX, currentY, 3, pointPaint);
            }
        }

        private string GetIndicatorColor(double value, List<IndicatorRange> ranges)
        {
            foreach (var range in ranges)
            {
                if (value >= range.StartValue && value <= range.EndValue)
                {
                    return range.Color;
                }
            }
            return "Blue"; // Default color
        }

        private SKColor ParseSKColor(string colorValue)
        {
            return colorValue.ToLower() switch
            {
                "red" => SKColors.Red,
                "green" => SKColors.Green,
                "blue" => SKColors.Blue,
                "yellow" => SKColors.Yellow,
                "orange" => SKColors.Orange,
                "purple" => SKColors.Purple,
                "black" => SKColors.Black,
                "white" => SKColors.White,
                "gray" or "grey" => SKColors.Gray,
                _ => SKColors.Blue
            };
        }

        [GeneratedRegex(@"=PageNumber", RegexOptions.IgnoreCase)]
        private static partial Regex PageNumberRegex();

        [GeneratedRegex(@"=TotalPages", RegexOptions.IgnoreCase)]
        private static partial Regex TotalPagesRegex();

        private string ProcessExpression(string? expression)
        {
            if (string.IsNullOrEmpty(expression))
                return "";

            var fieldMatch = FieldRegex().Match(expression);
            if (fieldMatch.Success && _currentDataRow != null)
            {
                var fieldName = fieldMatch.Groups[1].Value;
                if (_currentDataRow.Table.Columns.Contains(fieldName))
                {
                    return _currentDataRow[fieldName]?.ToString() ?? "";
                }
            }

            var paramMatch = ParameterRegex().Match(expression);
            if (paramMatch.Success)
            {
                var paramName = paramMatch.Groups[1].Value;
                if (_parameters.TryGetValue(paramName, out var value))
                {
                    return value?.ToString() ?? "";
                }
            }

            // Handle page number expressions
            if (PageNumberRegex().IsMatch(expression))
            {
                return _currentPageNumber.ToString();
            }

            if (TotalPagesRegex().IsMatch(expression))
            {
                return _totalPages.ToString();
            }

            if (expression.StartsWith('='))
            {
                var cleanExpression = expression[1..].Trim();

                if (cleanExpression.StartsWith('"') && cleanExpression.EndsWith('"'))
                {
                    return cleanExpression[1..^1];
                }

                if (cleanExpression.StartsWith("Now()", StringComparison.OrdinalIgnoreCase))
                {
                    return DateTime.Now.ToString();
                }

                if (cleanExpression.StartsWith("Today()", StringComparison.OrdinalIgnoreCase))
                {
                    return DateTime.Today.ToShortDateString();
                }

                return cleanExpression;
            }

            return expression;
        }
    }
}