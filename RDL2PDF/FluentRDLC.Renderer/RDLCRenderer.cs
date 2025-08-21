using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ScottPlot;
using QPDFColors = QuestPDF.Helpers.Colors;

namespace FluentRDLC.Renderer
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

        public void AddObjectDataSource<T>(string name, IEnumerable<T> objects) where T : class
        {
            var dataTable = ConvertObjectsToDataTable(objects);
            _dataSources[name] = dataTable;
        }

        public void AddParameter(string name, object value)
        {
            _parameters[name] = value;
        }

        public byte[] RenderToPdf(string rdlcPath)
        {
            return Render(rdlcPath, RenderFormat.PDF);
        }

        public byte[] RenderToPdfFromContent(string rdlcContent)
        {
            // Use original direct PDF generation to avoid circular dependency
            LoadRdlcContent(rdlcContent);
            return GeneratePdf();
        }

        public byte[] Render(string rdlcPath, RenderFormat format)
        {
            LoadRdlcFile(rdlcPath);
            return RenderWithFormat(format);
        }

        public byte[] RenderFromContent(string rdlcContent, RenderFormat format)
        {
            LoadRdlcContent(rdlcContent);
            return RenderWithFormat(format);
        }

        private byte[] RenderWithFormat(RenderFormat format)
        {
            var renderer = RendererFactory.CreateRenderer(format);
            var context = CreateRenderContext();
            return renderer.Render(context);
        }

        private RenderContext CreateRenderContext()
        {
            return new RenderContext
            {
                RdlcDocument = _rdlcDocument ?? throw new InvalidOperationException("RDLC document not loaded"),
                RdlcNamespace = _rdlcNamespace,
                DataSources = _dataSources,
                Parameters = _parameters,
                CurrentPageNumber = _currentPageNumber,
                TotalPages = _totalPages,
                CurrentDataRow = _currentDataRow,
                Width = 600,
                Height = 800
            };
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
                    page.PageColor(QPDFColors.White);
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
                    column.Item().Text("[Chart: No data source found]").FontColor(QPDFColors.Red.Medium);
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
                        column.Item().Text("[Chart: Rendering failed]").FontColor(QPDFColors.Red.Medium);
                    }
                }
                else
                {
                    column.Item().Text("[Chart: Invalid configuration]").FontColor(QPDFColors.Red.Medium);
                }
            }
            catch (Exception ex)
            {
                column.Item().Text($"[Chart Error: {ex.Message}]").FontColor(QPDFColors.Red.Medium);
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
                var xExpression = xValue.Value;
                point.Category = ProcessExpression(xExpression);
            }

            return point;
        }

        private byte[]? RenderChartToImage(ChartDefinition chartDef)
        {
            try
            {
                var plt = new Plot();
                // ScottPlot 5.x doesn't use Layout.Padding like this, margins are handled automatically
                
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

                // Set chart title if available
                if (!string.IsNullOrEmpty(chartDef.Title))
                {
                    plt.Title(chartDef.Title);
                }

                // Render to byte array
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
            if (chartDef.Series.Count == 0 || chartDef.Series[0].DataPoints.Count == 0)
                return;

            var series = chartDef.Series[0];
            var values = series.DataPoints.Select(p => p.Value).ToArray();
            var labels = series.DataPoints.Select(p => string.IsNullOrEmpty(p.Category) ? "Item" : p.Category).ToArray();

            var bar = plt.Add.Bars(values);
            bar.Color = ScottPlot.Color.FromHex("#2E86AB");
            
            // Set category labels on X-axis
            plt.Axes.Bottom.SetTicks(Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(), labels);
            plt.Axes.Bottom.TickLabelStyle.Rotation = -45;
            plt.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
            
            plt.Axes.Left.Label.Text = "Value";
            plt.Axes.Bottom.Label.Text = "Category";
        }

        private void CreateBarChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count == 0 || chartDef.Series[0].DataPoints.Count == 0)
                return;

            var series = chartDef.Series[0];
            var values = series.DataPoints.Select(p => p.Value).ToArray();
            var labels = series.DataPoints.Select(p => string.IsNullOrEmpty(p.Category) ? "Item" : p.Category).ToArray();

            // Create horizontal bars
            var positions = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();
            var bar = plt.Add.Bars(positions, values);
            bar.Color = ScottPlot.Color.FromHex("#A23B72");
            bar.Horizontal = true;
            
            // Set category labels on Y-axis for horizontal bars
            plt.Axes.Left.SetTicks(positions, labels);
            plt.Axes.Bottom.Label.Text = "Value";
            plt.Axes.Left.Label.Text = "Category";
        }

        private void CreateLineChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count == 0 || chartDef.Series[0].DataPoints.Count == 0)
                return;

            var series = chartDef.Series[0];
            var xValues = Enumerable.Range(0, series.DataPoints.Count).Select(i => (double)i).ToArray();
            var yValues = series.DataPoints.Select(p => p.Value).ToArray();
            var labels = series.DataPoints.Select(p => string.IsNullOrEmpty(p.Category) ? "Point" : p.Category).ToArray();

            var line = plt.Add.ScatterLine(xValues, yValues);
            line.Color = ScottPlot.Color.FromHex("#F18F01");
            line.LineWidth = 3;
            line.MarkerSize = 8;
            
            // Set category labels on X-axis
            plt.Axes.Bottom.SetTicks(xValues, labels);
            plt.Axes.Bottom.TickLabelStyle.Rotation = -45;
            plt.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
            
            plt.Axes.Left.Label.Text = "Value";
            plt.Axes.Bottom.Label.Text = "Category";
        }

        private void CreatePieChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count == 0 || chartDef.Series[0].DataPoints.Count == 0)
                return;

            var series = chartDef.Series[0];
            var values = series.DataPoints.Select(p => p.Value).ToArray();
            var labels = series.DataPoints.Select(p => string.IsNullOrEmpty(p.Category) ? "Slice" : p.Category).ToArray();

            var pie = plt.Add.Pie(values);
            // Set labels for pie slices
            for (int i = 0; i < labels.Length && i < pie.Slices.Count; i++)
            {
                pie.Slices[i].LegendText = labels[i];
            }
            plt.ShowLegend(Alignment.UpperRight);
            
            // Use a nice color palette
            var colors = new ScottPlot.Color[]
            {
                ScottPlot.Color.FromHex("#2E86AB"),
                ScottPlot.Color.FromHex("#A23B72"),
                ScottPlot.Color.FromHex("#F18F01"),
                ScottPlot.Color.FromHex("#C73E1D"),
                ScottPlot.Color.FromHex("#8B5A2B"),
                ScottPlot.Color.FromHex("#5D737E"),
                ScottPlot.Color.FromHex("#7B2D26"),
                ScottPlot.Color.FromHex("#4A4A4A")
            };
            
            for (int i = 0; i < pie.Slices.Count && i < colors.Length; i++)
            {
                pie.Slices[i].FillColor = colors[i % colors.Length];
            }
        }

        private void CreateAreaChart(Plot plt, ChartDefinition chartDef)
        {
            if (chartDef.Series.Count == 0 || chartDef.Series[0].DataPoints.Count == 0)
                return;

            var series = chartDef.Series[0];
            var xValues = Enumerable.Range(0, series.DataPoints.Count).Select(i => (double)i).ToArray();
            var yValues = series.DataPoints.Select(p => p.Value).ToArray();
            var labels = series.DataPoints.Select(p => string.IsNullOrEmpty(p.Category) ? "Point" : p.Category).ToArray();

            var scatter = plt.Add.Scatter(xValues, yValues);
            scatter.FillY = true;
            scatter.FillYColor = ScottPlot.Color.FromHex("#2E86AB").WithAlpha(100);
            scatter.Color = ScottPlot.Color.FromHex("#2E86AB");
            scatter.LineWidth = 2;
            
            // Set category labels on X-axis
            plt.Axes.Bottom.SetTicks(xValues, labels);
            plt.Axes.Bottom.TickLabelStyle.Rotation = -45;
            plt.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
            
            plt.Axes.Left.Label.Text = "Value";
            plt.Axes.Bottom.Label.Text = "Category";
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
                column.Item().BorderColor(QPDFColors.Grey.Medium).Border(1).Padding(5).Column(subColumn =>
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
                        column.Item().Text($"[Image not found: {imagePath}]").FontColor(QPDFColors.Red.Medium);
                    }
                }
                else if (source?.Value == "Embedded" && value != null)
                {
                    var embeddedImageName = value.Value;
                    var imageData = GetEmbeddedImageData(embeddedImageName);
                    
                    if (imageData != null)
                    {
                        column.Item().Image(imageData).FitWidth();
                    }
                    else
                    {
                        column.Item().Text($"[Embedded image not found: {embeddedImageName}]").FontColor(QPDFColors.Red.Medium);
                    }
                }
                else
                {
                    column.Item().Text($"[Image: {source?.Value ?? "Unknown"}]").FontColor(QPDFColors.Grey.Medium);
                }
            }
            catch (Exception ex)
            {
                column.Item().Text($"[Image Error: {ex.Message}]").FontColor(QPDFColors.Red.Medium);
            }
        }

        private byte[]? GetEmbeddedImageData(string imageName)
        {
            try
            {
                if (_rdlcDocument?.Root == null)
                    return null;

                var embeddedImagesElement = _rdlcDocument.Root.Element(_rdlcNamespace + "EmbeddedImages");
                if (embeddedImagesElement == null)
                    return null;

                var embeddedImage = embeddedImagesElement.Elements(_rdlcNamespace + "EmbeddedImage")
                    .FirstOrDefault(img => img.Attribute("Name")?.Value == imageName);

                if (embeddedImage == null)
                    return null;

                var imageDataElement = embeddedImage.Element(_rdlcNamespace + "ImageData");
                if (imageDataElement?.Value == null)
                    return null;

                // Decode base64 image data
                return Convert.FromBase64String(imageDataElement.Value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void ProcessLine(ColumnDescriptor column, XElement lineElement)
        {
            column.Item().LineHorizontal(1).LineColor(QPDFColors.Black);
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
                "red" => QPDFColors.Red.Medium,
                "blue" => QPDFColors.Blue.Medium,
                "green" => QPDFColors.Green.Medium,
                "black" => QPDFColors.Black,
                "white" => QPDFColors.White,
                "gray" or "grey" => QPDFColors.Grey.Medium,
                _ => QPDFColors.Black
            };
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(QPDFColors.Grey.Medium)
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
                        column.Item().Text("[Indicator: Rendering failed]").FontColor(QPDFColors.Red.Medium);
                    }
                }
                else
                {
                    column.Item().Text("[Indicator: Invalid configuration]").FontColor(QPDFColors.Red.Medium);
                }
            }
            catch (Exception ex)
            {
                column.Item().Text($"[Indicator Error: {ex.Message}]").FontColor(QPDFColors.Red.Medium);
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
            // Indicators are no longer supported after removing SkiaSharp dependency
            // Return null to skip indicator rendering
            return null;
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

        private static DataTable ConvertObjectsToDataTable<T>(IEnumerable<T> objects) where T : class
        {
            var dataTable = new DataTable();
            var type = typeof(T);
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Create columns from object properties
            foreach (var prop in properties)
            {
                var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                dataTable.Columns.Add(prop.Name, columnType);
            }

            // Add rows from objects
            foreach (var obj in objects)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(obj);
                    row[prop.Name] = value ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}