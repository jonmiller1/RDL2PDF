using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace FluentRDLC.Generator
{
    #region Data Models

    /// <summary>
    /// Represents a data source in the RDLC report
    /// </summary>
    public class RdlcDataSource
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string DataSourceReference { get; set; }
    }

    /// <summary>
    /// Represents a dataset in the RDLC report
    /// </summary>
    public class RdlcDataSet
    {
        public string Name { get; set; }
        public string DataSourceName { get; set; }
        public string CommandText { get; set; }
        public List<RdlcField> Fields { get; set; } = new List<RdlcField>();
    }

    /// <summary>
    /// Represents a field in a dataset
    /// </summary>
    public class RdlcField
    {
        public string Name { get; set; }
        public string DataField { get; set; }
        public string DataType { get; set; } = "System.String";
    }

    /// <summary>
    /// Represents a text box control in the report
    /// </summary>
    public class RdlcTextBox
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string FontFamily { get; set; } = "Arial";
        public string FontSize { get; set; } = "10pt";
        public string FontWeight { get; set; } = "Normal";
    }

    /// <summary>
    /// Represents an image control in the report
    /// </summary>
    public class RdlcImage
    {
        public string Name { get; set; }
        public string Source { get; set; } // "External", "Embedded", "Database"
        public string Value { get; set; } // URL, embedded name, or field expression
        public string MimeType { get; set; } = "image/png";
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Sizing { get; set; } = "AutoSize"; // "AutoSize", "Fit", "FitProportional", "Clip"
    }

    /// <summary>
    /// Represents a line control in the report
    /// </summary>
    public class RdlcLine
    {
        public string Name { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string BorderColor { get; set; } = "Black";
        public string BorderStyle { get; set; } = "Solid"; // "Solid", "Dashed", "Dotted", "Double", "None"
        public string BorderWidth { get; set; } = "1pt";
    }

    /// <summary>
    /// Represents a gauge/indicator control in the report
    /// </summary>
    public class RdlcGauge
    {
        public string Name { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Value { get; set; } // Expression or field reference
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 100;
        public string GaugeType { get; set; } = "Linear"; // "Linear", "Radial"
        public string BackgroundColor { get; set; } = "LightGray";
        public string PointerColor { get; set; } = "Blue";
        public string ScaleColor { get; set; } = "Black";
        public bool ShowScale { get; set; } = true;
        public bool ShowPointer { get; set; } = true;
        public List<RdlcGaugeRange> Ranges { get; set; } = new List<RdlcGaugeRange>();
    }

    /// <summary>
    /// Represents a color range in a gauge
    /// </summary>
    public class RdlcGaugeRange
    {
        public string Name { get; set; }
        public double StartValue { get; set; }
        public double EndValue { get; set; }
        public string Color { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// Represents a chart control in the report
    /// </summary>
    public class RdlcChart
    {
        public string Name { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string ChartType { get; set; } = "Column"; // Column, Bar, Line, Pie, Area, etc.
        public string DataSetName { get; set; }
        public string CategoryField { get; set; }
        public string ValueField { get; set; }
        public string SeriesField { get; set; }
        public string Title { get; set; }
        public string CategoryAxisTitle { get; set; }
        public string ValueAxisTitle { get; set; }
        public bool ShowLegend { get; set; } = true;
        public string LegendPosition { get; set; } = "Right"; // Top, Bottom, Left, Right
        public bool Show3D { get; set; } = false;
        public List<RdlcChartSeries> Series { get; set; } = new List<RdlcChartSeries>();
        public string BackgroundColor { get; set; } = "White";
        public string BorderColor { get; set; } = "Black";
        public string BorderStyle { get; set; } = "Solid";
        public string BorderWidth { get; set; } = "1pt";
    }

    /// <summary>
    /// Represents a series in a chart
    /// </summary>
    public class RdlcChartSeries
    {
        public string Name { get; set; }
        public string ValueField { get; set; }
        public string CategoryField { get; set; }
        public string Color { get; set; }
        public string ChartType { get; set; }
        public bool ShowDataLabels { get; set; } = false;
        public string MarkerType { get; set; } = "None"; // None, Square, Circle, Diamond, Triangle
        public string MarkerSize { get; set; } = "5pt";
    }

    /// <summary>
    /// Represents an embedded image in the report
    /// </summary>
    public class RdlcEmbeddedImage
    {
        public string Name { get; set; }
        public string MimeType { get; set; }
        public string ImageData { get; set; } // Base64 encoded image data
    }

    #endregion

    #region Fluent Builders

    /// <summary>
    /// Fluent builder for RDLC data sources
    /// </summary>
    public class DataSourceBuilder
    {
        internal RdlcDataSource DataSource { get; private set; }

        internal DataSourceBuilder(string name)
        {
            DataSource = new RdlcDataSource { Name = name };
        }

        public DataSourceBuilder WithConnectionString(string connectionString)
        {
            DataSource.ConnectionString = connectionString;
            return this;
        }

        public DataSourceBuilder WithDataSourceReference(string reference)
        {
            DataSource.DataSourceReference = reference;
            return this;
        }
    }

    /// <summary>
    /// Fluent builder for RDLC fields
    /// </summary>
    public class FieldBuilder
    {
        internal RdlcField Field { get; private set; }

        internal FieldBuilder(string name, string dataField)
        {
            Field = new RdlcField { Name = name, DataField = dataField };
        }

        public FieldBuilder AsString() => WithDataType("System.String");
        public FieldBuilder AsInt() => WithDataType("System.Int32");
        public FieldBuilder AsDecimal() => WithDataType("System.Decimal");
        public FieldBuilder AsDateTime() => WithDataType("System.DateTime");
        public FieldBuilder AsBoolean() => WithDataType("System.Boolean");

        public FieldBuilder WithDataType(string dataType)
        {
            Field.DataType = dataType;
            return this;
        }
    }

    /// <summary>
    /// Fluent builder for RDLC datasets
    /// </summary>
    public class DataSetBuilder
    {
        internal RdlcDataSet DataSet { get; private set; }

        internal DataSetBuilder(string name)
        {
            DataSet = new RdlcDataSet { Name = name };
        }

        public DataSetBuilder UsingDataSource(string dataSourceName)
        {
            DataSet.DataSourceName = dataSourceName;
            return this;
        }

        public DataSetBuilder WithQuery(string commandText)
        {
            DataSet.CommandText = commandText;
            return this;
        }

        public DataSetBuilder WithField(string name, string dataField, Action<FieldBuilder> configure = null)
        {
            var fieldBuilder = new FieldBuilder(name, dataField);
            configure?.Invoke(fieldBuilder);
            DataSet.Fields.Add(fieldBuilder.Field);
            return this;
        }

        public DataSetBuilder WithStringField(string name, string dataField = null) =>
            WithField(name, dataField ?? name, f => f.AsString());

        public DataSetBuilder WithIntField(string name, string dataField = null) =>
            WithField(name, dataField ?? name, f => f.AsInt());

        public DataSetBuilder WithDecimalField(string name, string dataField = null) =>
            WithField(name, dataField ?? name, f => f.AsDecimal());

        public DataSetBuilder WithDateTimeField(string name, string dataField = null) =>
            WithField(name, dataField ?? name, f => f.AsDateTime());
    }

    /// <summary>
    /// Fluent builder for RDLC text boxes
    /// </summary>
    public class TextBoxBuilder
    {
        internal RdlcTextBox TextBox { get; private set; }

        internal TextBoxBuilder(string name)
        {
            TextBox = new RdlcTextBox { Name = name };
        }

        public TextBoxBuilder WithText(string text)
        {
            TextBox.Value = text;
            return this;
        }

        public TextBoxBuilder WithExpression(string expression)
        {
            TextBox.Value = expression.StartsWith("=") ? expression : $"={expression}";
            return this;
        }

        public TextBoxBuilder WithFieldValue(string fieldName)
        {
            TextBox.Value = $"=Fields!{fieldName}.Value";
            return this;
        }

        public TextBoxBuilder At(double left, double top)
        {
            TextBox.Left = left;
            TextBox.Top = top;
            return this;
        }

        public TextBoxBuilder WithSize(double width, double height)
        {
            TextBox.Width = width;
            TextBox.Height = height;
            return this;
        }

        public TextBoxBuilder WithBounds(double left, double top, double width, double height) =>
            At(left, top).WithSize(width, height);

        public TextBoxBuilder WithFont(string fontFamily)
        {
            TextBox.FontFamily = fontFamily;
            return this;
        }

        public TextBoxBuilder WithFontSize(string fontSize)
        {
            TextBox.FontSize = fontSize;
            return this;
        }

        public TextBoxBuilder WithFontSize(int points) => WithFontSize($"{points}pt");

        public TextBoxBuilder Bold()
        {
            TextBox.FontWeight = "Bold";
            return this;
        }

        public TextBoxBuilder Normal()
        {
            TextBox.FontWeight = "Normal";
            return this;
        }

        public TextBoxBuilder AsTitle() => WithFontSize(16).Bold();
        public TextBoxBuilder AsHeader() => WithFontSize(12).Bold();
        public TextBoxBuilder AsLabel() => WithFontSize(10).Bold();
    }

    /// <summary>
    /// Fluent builder for RDLC images
    /// </summary>
    public class ImageBuilder
    {
        internal RdlcImage Image { get; private set; }

        internal ImageBuilder(string name)
        {
            Image = new RdlcImage { Name = name };
        }

        public ImageBuilder FromUrl(string url)
        {
            Image.Source = "External";
            Image.Value = url;
            return this;
        }

        public ImageBuilder FromEmbedded(string embeddedImageName)
        {
            Image.Source = "Embedded";
            Image.Value = embeddedImageName;
            return this;
        }

        public ImageBuilder FromDatabase(string fieldName)
        {
            Image.Source = "Database";
            Image.Value = $"=Fields!{fieldName}.Value";
            return this;
        }

        public ImageBuilder At(double left, double top)
        {
            Image.Left = left;
            Image.Top = top;
            return this;
        }

        public ImageBuilder WithSize(double width, double height)
        {
            Image.Width = width;
            Image.Height = height;
            return this;
        }

        public ImageBuilder WithBounds(double left, double top, double width, double height) =>
            At(left, top).WithSize(width, height);

        public ImageBuilder AsPng() { Image.MimeType = "image/png"; return this; }
        public ImageBuilder AsJpeg() { Image.MimeType = "image/jpeg"; return this; }
        public ImageBuilder AutoSize() { Image.Sizing = "AutoSize"; return this; }
        public ImageBuilder Fit() { Image.Sizing = "Fit"; return this; }
        public ImageBuilder FitProportional() { Image.Sizing = "FitProportional"; return this; }
        public ImageBuilder AsLogo() => WithSize(2, 1).FitProportional();
    }

    /// <summary>
    /// Fluent builder for RDLC lines
    /// </summary>
    public class LineBuilder
    {
        internal RdlcLine Line { get; private set; }

        internal LineBuilder(string name)
        {
            Line = new RdlcLine { Name = name };
        }

        public LineBuilder At(double left, double top)
        {
            Line.Left = left;
            Line.Top = top;
            return this;
        }

        public LineBuilder WithSize(double width, double height)
        {
            Line.Width = width;
            Line.Height = height;
            return this;
        }

        public LineBuilder Horizontal(double left, double top, double width) =>
            WithBounds(left, top, width, 0.01);

        public LineBuilder Vertical(double left, double top, double height) =>
            WithBounds(left, top, 0.01, height);

        public LineBuilder WithBounds(double left, double top, double width, double height) =>
            At(left, top).WithSize(width, height);

        public LineBuilder WithColor(string color) { Line.BorderColor = color; return this; }
        public LineBuilder WithWidth(double points) { Line.BorderWidth = $"{points}pt"; return this; }
        public LineBuilder Solid() { Line.BorderStyle = "Solid"; return this; }
        public LineBuilder Dashed() { Line.BorderStyle = "Dashed"; return this; }
        public LineBuilder Black() => WithColor("Black");
        public LineBuilder Gray() => WithColor("Gray");
        public LineBuilder AsSeparator() => Gray().WithWidth(0.5);
    }

    /// <summary>
    /// Fluent builder for RDLC charts
    /// </summary>
    public class ChartBuilder
    {
        internal RdlcChart Chart { get; private set; }

        internal ChartBuilder(string name)
        {
            Chart = new RdlcChart { Name = name };
        }

        public ChartBuilder At(double left, double top)
        {
            Chart.Left = left;
            Chart.Top = top;
            return this;
        }

        public ChartBuilder WithSize(double width, double height)
        {
            Chart.Width = width;
            Chart.Height = height;
            return this;
        }

        public ChartBuilder WithBounds(double left, double top, double width, double height) =>
            At(left, top).WithSize(width, height);

        public ChartBuilder UsingDataSet(string dataSetName)
        {
            Chart.DataSetName = dataSetName;
            return this;
        }

        public ChartBuilder WithData(string categoryField, string valueField)
        {
            Chart.CategoryField = categoryField;
            Chart.ValueField = valueField;
            return this;
        }

        public ChartBuilder WithTitle(string title) { Chart.Title = title; return this; }
        public ChartBuilder AsColumn() { Chart.ChartType = "Column"; return this; }
        public ChartBuilder AsBar() { Chart.ChartType = "Bar"; return this; }
        public ChartBuilder AsLine() { Chart.ChartType = "Line"; return this; }
        public ChartBuilder AsPie() { Chart.ChartType = "Pie"; return this; }
        public ChartBuilder ShowLegend(bool show = true) { Chart.ShowLegend = show; return this; }
        public ChartBuilder LegendOnRight() { Chart.LegendPosition = "Right"; return this; }
    }

    /// <summary>
    /// Fluent builder for report layout and properties
    /// </summary>
    public class ReportLayoutBuilder
    {
        internal double? Width { get; private set; }
        internal double? Height { get; private set; }
        internal double? LeftMargin { get; private set; }
        internal double? RightMargin { get; private set; }
        internal double? TopMargin { get; private set; }
        internal double? BottomMargin { get; private set; }

        public ReportLayoutBuilder Letter() => WithPageSize(8.5, 11);
        public ReportLayoutBuilder A4() => WithPageSize(8.27, 11.69);

        public ReportLayoutBuilder WithPageSize(double width, double height)
        {
            Width = width;
            Height = height;
            return this;
        }

        public ReportLayoutBuilder WithMargins(double all) =>
            WithMargins(all, all, all, all);

        public ReportLayoutBuilder WithMargins(double left, double right, double top, double bottom)
        {
            LeftMargin = left;
            RightMargin = right;
            TopMargin = top;
            BottomMargin = bottom;
            return this;
        }

        public ReportLayoutBuilder WithBodyHeight(double height)
        {
            Height = height;
            return this;
        }
    }

    /// <summary>
    /// Fluent builder for page headers and footers
    /// </summary>
    public class PageSectionBuilder
    {
        internal List<TextBoxBuilder> TextBoxes { get; private set; } = new List<TextBoxBuilder>();
        internal List<ImageBuilder> Images { get; private set; } = new List<ImageBuilder>();
        internal List<LineBuilder> Lines { get; private set; } = new List<LineBuilder>();
        internal double Height { get; private set; } = 0.5;
        internal bool ShowOnFirstPage { get; private set; } = true;
        internal bool ShowOnLastPage { get; private set; } = true;

        public PageSectionBuilder WithHeight(double height) { Height = height; return this; }
        public PageSectionBuilder PrintOnFirstPage(bool print = true) { ShowOnFirstPage = print; return this; }
        public PageSectionBuilder PrintOnLastPage(bool print = true) { ShowOnLastPage = print; return this; }

        public PageSectionBuilder WithTextBox(string name, Action<TextBoxBuilder> configure)
        {
            var textBoxBuilder = new TextBoxBuilder(name);
            configure(textBoxBuilder);
            TextBoxes.Add(textBoxBuilder);
            return this;
        }

        public PageSectionBuilder WithTitle(string text, double left = 0, double top = 0, double width = 6)
        {
            return WithTextBox($"Title_{Guid.NewGuid():N}", tb => tb
                .WithText(text)
                .WithBounds(left, top, width, 0.4)
                .AsTitle());
        }

        public PageSectionBuilder WithPageNumber(double left = 6, double top = 0)
        {
            return WithTextBox($"PageNumber_{Guid.NewGuid():N}", tb => tb
                .WithExpression("\"Page \" & Globals!PageNumber")
                .WithBounds(left, top, 2, 0.25));
        }
    }

    #endregion

    #region Main Report Builder

    /// <summary>
    /// Main fluent builder for RDLC reports
    /// </summary>
    public class RdlcReportBuilder
    {
        private readonly RdlcProcessor _processor;
        private readonly List<DataSourceBuilder> _dataSources = new List<DataSourceBuilder>();
        private readonly List<DataSetBuilder> _dataSets = new List<DataSetBuilder>();
        private readonly List<TextBoxBuilder> _textBoxes = new List<TextBoxBuilder>();
        private readonly List<ImageBuilder> _images = new List<ImageBuilder>();
        private readonly List<LineBuilder> _lines = new List<LineBuilder>();
        private readonly List<ChartBuilder> _charts = new List<ChartBuilder>();
        private readonly List<RdlcEmbeddedImage> _embeddedImages = new List<RdlcEmbeddedImage>();
        private ReportLayoutBuilder _layout;
        private PageSectionBuilder _header;
        private PageSectionBuilder _footer;

        private RdlcReportBuilder(string reportName = "Report1")
        {
            _processor = new RdlcProcessor();
            _processor.CreateNew(reportName);
        }

        /// <summary>
        /// Start building a new RDLC report
        /// </summary>
        public static RdlcReportBuilder Create(string reportName = "Report1") =>
            new RdlcReportBuilder(reportName);

        public RdlcReportBuilder WithSqlDataSource(string name, string connectionString)
        {
            return WithDataSource(name, ds => ds.WithConnectionString(connectionString));
        }

        public RdlcReportBuilder WithDataSource(string name, Action<DataSourceBuilder> configure)
        {
            var dataSourceBuilder = new DataSourceBuilder(name);
            configure(dataSourceBuilder);
            _dataSources.Add(dataSourceBuilder);
            return this;
        }

        public RdlcReportBuilder WithDataSet(string name, Action<DataSetBuilder> configure)
        {
            var dataSetBuilder = new DataSetBuilder(name);
            configure(dataSetBuilder);
            _dataSets.Add(dataSetBuilder);
            return this;
        }

        public RdlcReportBuilder WithTextBox(string name, Action<TextBoxBuilder> configure)
        {
            var textBoxBuilder = new TextBoxBuilder(name);
            configure(textBoxBuilder);
            _textBoxes.Add(textBoxBuilder);
            return this;
        }

        public RdlcReportBuilder WithTitle(string text, double left = 0, double top = 0, double width = 6)
        {
            return WithTextBox($"Title_{Guid.NewGuid():N}", tb => tb
                .WithText(text)
                .WithBounds(left, top, width, 0.5)
                .AsTitle());
        }

        public RdlcReportBuilder WithLabel(string text, double left, double top, double width = 2)
        {
            return WithTextBox($"Label_{Guid.NewGuid():N}", tb => tb
                .WithText(text)
                .WithBounds(left, top, width, 0.25)
                .AsLabel());
        }

        public RdlcReportBuilder WithFieldValue(string fieldName, double left, double top, double width = 2, double height = 0.25)
        {
            return WithTextBox($"Field_{fieldName}_{Guid.NewGuid():N}", tb => tb
                .WithFieldValue(fieldName)
                .WithBounds(left, top, width, height));
        }

        public RdlcReportBuilder WithChart(string name, Action<ChartBuilder> configure)
        {
            var chartBuilder = new ChartBuilder(name);
            configure(chartBuilder);
            _charts.Add(chartBuilder);
            return this;
        }

        public RdlcReportBuilder WithSalesChart(string dataSetName, string categoryField, string valueField,
            double left, double top, double width = 4, double height = 3)
        {
            return WithChart($"SalesChart_{Guid.NewGuid():N}", chart => chart
                .UsingDataSet(dataSetName)
                .WithData(categoryField, valueField)
                .WithBounds(left, top, width, height)
                .AsColumn()
                .WithTitle("Sales Performance")
                .ShowLegend()
                .LegendOnRight());
        }

        public RdlcReportBuilder WithLineChart(string dataSetName, string categoryField, string valueField,
            double left, double top, double width = 4, double height = 2.5)
        {
            return WithChart($"LineChart_{Guid.NewGuid():N}", chart => chart
                .UsingDataSet(dataSetName)
                .WithData(categoryField, valueField)
                .WithBounds(left, top, width, height)
                .AsLine()
                .WithTitle("Trends")
                .ShowLegend());
        }

        public RdlcReportBuilder WithPieChart(string dataSetName, string categoryField, string valueField,
            double left, double top, double size = 3)
        {
            return WithChart($"PieChart_{Guid.NewGuid():N}", chart => chart
                .UsingDataSet(dataSetName)
                .WithData(categoryField, valueField)
                .WithBounds(left, top, size, size)
                .AsPie()
                .WithTitle("Distribution")
                .ShowLegend());
        }

        public RdlcReportBuilder WithLine(string name, Action<LineBuilder> configure)
        {
            var lineBuilder = new LineBuilder(name);
            configure(lineBuilder);
            _lines.Add(lineBuilder);
            return this;
        }

        public RdlcReportBuilder WithHorizontalLine(double left = 0, double top = 0, double width = 8)
        {
            return WithLine($"HLine_{Guid.NewGuid():N}", line => line
                .Horizontal(left, top, width)
                .AsSeparator());
        }

        public RdlcReportBuilder WithEmbeddedImageFromFile(string name, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Image file not found: {filePath}");

            var imageBytes = File.ReadAllBytes(filePath);
            var base64Data = Convert.ToBase64String(imageBytes);
            var mimeType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "image/png"
            };

            _embeddedImages.Add(new RdlcEmbeddedImage
            {
                Name = name,
                ImageData = base64Data,
                MimeType = mimeType
            });
            return this;
        }

        public RdlcReportBuilder WithHeader(Action<PageSectionBuilder> configure)
        {
            _header = new PageSectionBuilder();
            configure(_header);
            return this;
        }

        public RdlcReportBuilder WithFooter(Action<PageSectionBuilder> configure)
        {
            _footer = new PageSectionBuilder();
            configure(_footer);
            return this;
        }

        public RdlcReportBuilder WithSimpleHeader(string title)
        {
            return WithHeader(header => header
                .WithTitle(title)
                .WithHeight(0.5));
        }

        public RdlcReportBuilder WithSimpleFooter(string copyrightText)
        {
            return WithFooter(footer => footer
                .WithTextBox("Copyright", tb => tb
                    .WithText(copyrightText)
                    .WithBounds(0, 0, 4, 0.25)
                    .WithFontSize(8))
                .WithPageNumber()
                .WithHeight(0.4));
        }

        public RdlcReportBuilder WithLayout(Action<ReportLayoutBuilder> configure)
        {
            _layout = new ReportLayoutBuilder();
            configure(_layout);
            return this;
        }

        /// <summary>
        /// Build the report and return the processor
        /// </summary>
        public RdlcProcessor Build()
        {
            // Add embedded images first
            foreach (var embeddedImage in _embeddedImages)
            {
                _processor.AddEmbeddedImage(embeddedImage);
            }

            // Add data sources
            foreach (var dataSourceBuilder in _dataSources)
            {
                _processor.AddDataSource(dataSourceBuilder.DataSource);
            }

            // Add datasets
            foreach (var dataSetBuilder in _dataSets)
            {
                _processor.AddDataSet(dataSetBuilder.DataSet);
            }

            // Add text boxes
            foreach (var textBoxBuilder in _textBoxes)
            {
                _processor.AddTextBox(textBoxBuilder.TextBox);
            }

            // Add images
            foreach (var imageBuilder in _images)
            {
                _processor.AddImage(imageBuilder.Image);
            }

            // Add lines
            foreach (var lineBuilder in _lines)
            {
                _processor.AddLine(lineBuilder.Line);
            }

            // Add charts
            foreach (var chartBuilder in _charts)
            {
                _processor.AddChart(chartBuilder.Chart);
            }

            // Apply layout
            if (_layout != null)
            {
                _processor.SetReportProperties(
                    width: _layout.Width,
                    bodyHeight: _layout.Height,
                    leftMargin: _layout.LeftMargin,
                    rightMargin: _layout.RightMargin,
                    topMargin: _layout.TopMargin,
                    bottomMargin: _layout.BottomMargin);
            }

            // Add header/footer
            if (_header != null) _processor.AddPageHeader(_header);
            if (_footer != null) _processor.AddPageFooter(_footer);

            return _processor;
        }

        /// <summary>
        /// Build and save the report to a file
        /// </summary>
        public void SaveTo(string filePath) => Build().SaveToFile(filePath);

        /// <summary>
        /// Build and get the report as XML string
        /// </summary>
        public string ToXml() => Build().GetXml();
    }

    #endregion

    #region Core Processor

    public class RdlcProcessor
    {
        private XDocument _rdlcDocument;
        private XNamespace _reportNamespace;

        public void CreateNew(string reportName = "Report1")
        {
            _reportNamespace = XNamespace.Get("http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition");

            _rdlcDocument = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(_reportNamespace + "Report",
                    new XAttribute("xmlns", _reportNamespace.NamespaceName),
                    new XElement(_reportNamespace + "AutoRefresh", "0"),
                    new XElement(_reportNamespace + "DataSources"),
                    new XElement(_reportNamespace + "DataSets"),
                    new XElement(_reportNamespace + "Body",
                        new XElement(_reportNamespace + "ReportItems"),
                        new XElement(_reportNamespace + "Height", "2in"),
                        new XElement(_reportNamespace + "Style")
                    ),
                    new XElement(_reportNamespace + "Width", "6.5in"),
                    new XElement(_reportNamespace + "Page",
                        new XElement(_reportNamespace + "LeftMargin", "1in"),
                        new XElement(_reportNamespace + "RightMargin", "1in"),
                        new XElement(_reportNamespace + "TopMargin", "1in"),
                        new XElement(_reportNamespace + "BottomMargin", "1in"),
                        new XElement(_reportNamespace + "Style")
                    )
                )
            );
        }

        public void SaveToFile(string filePath)
        {
            if (_rdlcDocument == null)
                throw new InvalidOperationException("No RDLC document loaded.");
            _rdlcDocument.Save(filePath);
        }

        public string GetXml()
        {
            if (_rdlcDocument == null)
                throw new InvalidOperationException("No RDLC document loaded.");
            return _rdlcDocument.ToString();
        }

        public void AddDataSource(RdlcDataSource dataSource)
        {
            var dataSourcesElement = GetOrCreateElement(_rdlcDocument.Root, "DataSources");
            var dataSourceElement = new XElement(_reportNamespace + "DataSource",
                new XAttribute("Name", dataSource.Name),
                new XElement(_reportNamespace + "ConnectionProperties",
                    new XElement(_reportNamespace + "DataProvider", "System.Data.SqlClient"),
                    new XElement(_reportNamespace + "ConnectString", dataSource.ConnectionString ?? "")
                ));
            dataSourcesElement.Add(dataSourceElement);
        }

        public void AddDataSet(RdlcDataSet dataSet)
        {
            var dataSetsElement = GetOrCreateElement(_rdlcDocument.Root, "DataSets");
            var dataSetElement = new XElement(_reportNamespace + "DataSet",
                new XAttribute("Name", dataSet.Name),
                new XElement(_reportNamespace + "Query",
                    new XElement(_reportNamespace + "DataSourceName", dataSet.DataSourceName),
                    new XElement(_reportNamespace + "CommandText", dataSet.CommandText)
                ),
                new XElement(_reportNamespace + "Fields")
            );

            var fieldsElement = dataSetElement.Element(_reportNamespace + "Fields");
            foreach (var field in dataSet.Fields)
            {
                fieldsElement?.Add(new XElement(_reportNamespace + "Field",
                    new XAttribute("Name", field.Name),
                    new XElement(_reportNamespace + "DataField", field.DataField),
                    new XElement(_reportNamespace + "rd:TypeName", field.DataType,
                        new XAttribute(XNamespace.Get("http://schemas.microsoft.com/SQLServer/reporting/reportdesigner") + "TypeName", field.DataType))
                ));
            }
            dataSetsElement.Add(dataSetElement);
        }

        public void AddTextBox(RdlcTextBox textBox)
        {
            var reportItemsElement = GetReportItemsElement();
            var textBoxElement = new XElement(_reportNamespace + "Textbox",
                new XAttribute("Name", textBox.Name),
                new XElement(_reportNamespace + "CanGrow", "true"),
                new XElement(_reportNamespace + "KeepTogether", "true"),
                new XElement(_reportNamespace + "Paragraphs",
                    new XElement(_reportNamespace + "Paragraph",
                        new XElement(_reportNamespace + "TextRuns",
                            new XElement(_reportNamespace + "TextRun",
                                new XElement(_reportNamespace + "Value", textBox.Value),
                                new XElement(_reportNamespace + "Style",
                                    new XElement(_reportNamespace + "FontFamily", textBox.FontFamily),
                                    new XElement(_reportNamespace + "FontSize", textBox.FontSize),
                                    new XElement(_reportNamespace + "FontWeight", textBox.FontWeight)
                                )
                            )
                        ),
                        new XElement(_reportNamespace + "Style")
                    )
                ),
                new XElement(_reportNamespace + "Top", $"{textBox.Top}in"),
                new XElement(_reportNamespace + "Left", $"{textBox.Left}in"),
                new XElement(_reportNamespace + "Height", $"{textBox.Height}in"),
                new XElement(_reportNamespace + "Width", $"{textBox.Width}in"),
                new XElement(_reportNamespace + "Style")
            );
            reportItemsElement.Add(textBoxElement);
        }

        public void AddImage(RdlcImage image)
        {
            var reportItemsElement = GetReportItemsElement();
            var imageElement = new XElement(_reportNamespace + "Image",
                new XAttribute("Name", image.Name),
                new XElement(_reportNamespace + "Source", image.Source),
                new XElement(_reportNamespace + "Value", image.Value),
                new XElement(_reportNamespace + "MIMEType", image.MimeType),
                new XElement(_reportNamespace + "Sizing", image.Sizing),
                new XElement(_reportNamespace + "Top", $"{image.Top}in"),
                new XElement(_reportNamespace + "Left", $"{image.Left}in"),
                new XElement(_reportNamespace + "Height", $"{image.Height}in"),
                new XElement(_reportNamespace + "Width", $"{image.Width}in"),
                new XElement(_reportNamespace + "Style")
            );
            reportItemsElement.Add(imageElement);
        }

        public void AddLine(RdlcLine line)
        {
            var reportItemsElement = GetReportItemsElement();
            var lineElement = new XElement(_reportNamespace + "Line",
                new XAttribute("Name", line.Name),
                new XElement(_reportNamespace + "Top", $"{line.Top}in"),
                new XElement(_reportNamespace + "Left", $"{line.Left}in"),
                new XElement(_reportNamespace + "Height", $"{line.Height}in"),
                new XElement(_reportNamespace + "Width", $"{line.Width}in"),
                new XElement(_reportNamespace + "Style",
                    new XElement(_reportNamespace + "Border",
                        new XElement(_reportNamespace + "Color", line.BorderColor),
                        new XElement(_reportNamespace + "Style", line.BorderStyle),
                        new XElement(_reportNamespace + "Width", line.BorderWidth)
                    )
                )
            );
            reportItemsElement.Add(lineElement);
        }

        public void AddChart(RdlcChart chart)
        {
            var reportItemsElement = GetReportItemsElement();
            var chartElement = new XElement(_reportNamespace + "Chart",
                new XAttribute("Name", chart.Name),
                new XElement(_reportNamespace + "Top", $"{chart.Top}in"),
                new XElement(_reportNamespace + "Left", $"{chart.Left}in"),
                new XElement(_reportNamespace + "Height", $"{chart.Height}in"),
                new XElement(_reportNamespace + "Width", $"{chart.Width}in"),
                new XElement(_reportNamespace + "DataSetName", chart.DataSetName),
                new XElement(_reportNamespace + "ChartAreas",
                    new XElement(_reportNamespace + "ChartArea",
                        new XAttribute("Name", $"{chart.Name}_ChartArea"),
                        new XElement(_reportNamespace + "Style")
                    )
                ),
                new XElement(_reportNamespace + "ChartSeries",
                    new XElement(_reportNamespace + "ChartSeries",
                        new XAttribute("Name", $"{chart.Name}_Series1"),
                        new XElement(_reportNamespace + "ChartDataPoints",
                            new XElement(_reportNamespace + "ChartDataPoint",
                                new XElement(_reportNamespace + "ChartDataPointValues",
                                    new XElement(_reportNamespace + "Y", $"=Fields!{chart.ValueField}.Value")
                                )
                            )
                        ),
                        new XElement(_reportNamespace + "Type", chart.ChartType)
                    )
                ),
                new XElement(_reportNamespace + "CategoryGroups",
                    new XElement(_reportNamespace + "CategoryGroup",
                        new XAttribute("Name", $"{chart.Name}_CategoryGroup"),
                        new XElement(_reportNamespace + "GroupExpressions",
                            new XElement(_reportNamespace + "GroupExpression", $"=Fields!{chart.CategoryField}.Value")
                        )
                    )
                ),
                new XElement(_reportNamespace + "Style")
            );

            // Add title if specified
            if (!string.IsNullOrEmpty(chart.Title))
            {
                chartElement.Add(new XElement(_reportNamespace + "Title",
                    new XElement(_reportNamespace + "Caption", chart.Title),
                    new XElement(_reportNamespace + "Style",
                        new XElement(_reportNamespace + "FontWeight", "Bold"),
                        new XElement(_reportNamespace + "FontSize", "12pt")
                    )
                ));
            }

            // Add legend if enabled
            if (chart.ShowLegend)
            {
                chartElement.Add(new XElement(_reportNamespace + "Legend",
                    new XAttribute("Name", $"{chart.Name}_Legend"),
                    new XElement(_reportNamespace + "Position", chart.LegendPosition),
                    new XElement(_reportNamespace + "Style")
                ));
            }

            reportItemsElement.Add(chartElement);
        }

        public void AddEmbeddedImage(RdlcEmbeddedImage embeddedImage)
        {
            var embeddedImagesElement = GetOrCreateElement(_rdlcDocument.Root, "EmbeddedImages");
            var embeddedImageElement = new XElement(_reportNamespace + "EmbeddedImage",
                new XAttribute("Name", embeddedImage.Name),
                new XElement(_reportNamespace + "MIMEType", embeddedImage.MimeType),
                new XElement(_reportNamespace + "ImageData", embeddedImage.ImageData)
            );
            embeddedImagesElement.Add(embeddedImageElement);
        }

        public void AddPageHeader(PageSectionBuilder headerBuilder)
        {
            var pageHeaderElement = new XElement(_reportNamespace + "PageHeader",
                new XElement(_reportNamespace + "Height", $"{headerBuilder.Height}in"),
                new XElement(_reportNamespace + "PrintOnFirstPage", headerBuilder.ShowOnFirstPage.ToString().ToLower()),
                new XElement(_reportNamespace + "PrintOnLastPage", headerBuilder.ShowOnLastPage.ToString().ToLower()),
                new XElement(_reportNamespace + "ReportItems"),
                new XElement(_reportNamespace + "Style")
            );

            var reportItemsElement = pageHeaderElement.Element(_reportNamespace + "ReportItems");
            AddSectionItems(reportItemsElement, headerBuilder);

            // Insert before Body
            var bodyElement = _rdlcDocument.Root?.Element(_reportNamespace + "Body");
            bodyElement?.AddBeforeSelf(pageHeaderElement);
        }

        public void AddPageFooter(PageSectionBuilder footerBuilder)
        {
            var pageFooterElement = new XElement(_reportNamespace + "PageFooter",
                new XElement(_reportNamespace + "Height", $"{footerBuilder.Height}in"),
                new XElement(_reportNamespace + "PrintOnFirstPage", footerBuilder.ShowOnFirstPage.ToString().ToLower()),
                new XElement(_reportNamespace + "PrintOnLastPage", footerBuilder.ShowOnLastPage.ToString().ToLower()),
                new XElement(_reportNamespace + "ReportItems"),
                new XElement(_reportNamespace + "Style")
            );

            var reportItemsElement = pageFooterElement.Element(_reportNamespace + "ReportItems");
            AddSectionItems(reportItemsElement, footerBuilder);

            // Insert after Body
            var bodyElement = _rdlcDocument.Root?.Element(_reportNamespace + "Body");
            bodyElement?.AddAfterSelf(pageFooterElement);
        }

        public void SetReportProperties(double? width = null, double? bodyHeight = null,
            double? leftMargin = null, double? rightMargin = null,
            double? topMargin = null, double? bottomMargin = null)
        {
            if (width.HasValue)
            {
                var widthElement = _rdlcDocument.Root?.Element(_reportNamespace + "Width");
                if (widthElement != null) widthElement.Value = $"{width.Value}in";
            }

            if (bodyHeight.HasValue)
            {
                var heightElement = _rdlcDocument.Root?.Element(_reportNamespace + "Body")?.Element(_reportNamespace + "Height");
                if (heightElement != null) heightElement.Value = $"{bodyHeight.Value}in";
            }

            var pageElement = _rdlcDocument.Root?.Element(_reportNamespace + "Page");
            if (pageElement != null)
            {
                if (leftMargin.HasValue) UpdateOrCreateElement(pageElement, "LeftMargin", $"{leftMargin.Value}in");
                if (rightMargin.HasValue) UpdateOrCreateElement(pageElement, "RightMargin", $"{rightMargin.Value}in");
                if (topMargin.HasValue) UpdateOrCreateElement(pageElement, "TopMargin", $"{topMargin.Value}in");
                if (bottomMargin.HasValue) UpdateOrCreateElement(pageElement, "BottomMargin", $"{bottomMargin.Value}in");
            }
        }

        private XElement GetOrCreateElement(XElement parent, string elementName)
        {
            var element = parent?.Element(_reportNamespace + elementName);
            if (element == null)
            {
                element = new XElement(_reportNamespace + elementName);
                parent?.Add(element);
            }
            return element;
        }

        private XElement GetReportItemsElement()
        {
            var bodyElement = _rdlcDocument.Root?.Element(_reportNamespace + "Body");
            return GetOrCreateElement(bodyElement, "ReportItems");
        }

        private void AddSectionItems(XElement reportItemsElement, PageSectionBuilder sectionBuilder)
        {
            foreach (var textBoxBuilder in sectionBuilder.TextBoxes)
            {
                AddTextBoxToElement(reportItemsElement, textBoxBuilder.TextBox);
            }

            foreach (var imageBuilder in sectionBuilder.Images)
            {
                AddImageToElement(reportItemsElement, imageBuilder.Image);
            }

            foreach (var lineBuilder in sectionBuilder.Lines)
            {
                AddLineToElement(reportItemsElement, lineBuilder.Line);
            }
        }

        private void AddTextBoxToElement(XElement parentElement, RdlcTextBox textBox)
        {
            var textBoxElement = new XElement(_reportNamespace + "Textbox",
                new XAttribute("Name", textBox.Name),
                new XElement(_reportNamespace + "CanGrow", "true"),
                new XElement(_reportNamespace + "Paragraphs",
                    new XElement(_reportNamespace + "Paragraph",
                        new XElement(_reportNamespace + "TextRuns",
                            new XElement(_reportNamespace + "TextRun",
                                new XElement(_reportNamespace + "Value", textBox.Value),
                                new XElement(_reportNamespace + "Style",
                                    new XElement(_reportNamespace + "FontFamily", textBox.FontFamily),
                                    new XElement(_reportNamespace + "FontSize", textBox.FontSize),
                                    new XElement(_reportNamespace + "FontWeight", textBox.FontWeight)
                                )
                            )
                        )
                    )
                ),
                new XElement(_reportNamespace + "Top", $"{textBox.Top}in"),
                new XElement(_reportNamespace + "Left", $"{textBox.Left}in"),
                new XElement(_reportNamespace + "Height", $"{textBox.Height}in"),
                new XElement(_reportNamespace + "Width", $"{textBox.Width}in"),
                new XElement(_reportNamespace + "Style")
            );
            parentElement?.Add(textBoxElement);
        }

        private void AddImageToElement(XElement parentElement, RdlcImage image)
        {
            var imageElement = new XElement(_reportNamespace + "Image",
                new XAttribute("Name", image.Name),
                new XElement(_reportNamespace + "Source", image.Source),
                new XElement(_reportNamespace + "Value", image.Value),
                new XElement(_reportNamespace + "MIMEType", image.MimeType),
                new XElement(_reportNamespace + "Sizing", image.Sizing),
                new XElement(_reportNamespace + "Top", $"{image.Top}in"),
                new XElement(_reportNamespace + "Left", $"{image.Left}in"),
                new XElement(_reportNamespace + "Height", $"{image.Height}in"),
                new XElement(_reportNamespace + "Width", $"{image.Width}in"),
                new XElement(_reportNamespace + "Style")
            );
            parentElement?.Add(imageElement);
        }

        private void AddLineToElement(XElement parentElement, RdlcLine line)
        {
            var lineElement = new XElement(_reportNamespace + "Line",
                new XAttribute("Name", line.Name),
                new XElement(_reportNamespace + "Top", $"{line.Top}in"),
                new XElement(_reportNamespace + "Left", $"{line.Left}in"),
                new XElement(_reportNamespace + "Height", $"{line.Height}in"),
                new XElement(_reportNamespace + "Width", $"{line.Width}in"),
                new XElement(_reportNamespace + "Style",
                    new XElement(_reportNamespace + "Border",
                        new XElement(_reportNamespace + "Color", line.BorderColor),
                        new XElement(_reportNamespace + "Style", line.BorderStyle),
                        new XElement(_reportNamespace + "Width", line.BorderWidth)
                    )
                )
            );
            parentElement?.Add(lineElement);
        }

        private void UpdateOrCreateElement(XElement parent, string elementName, string value)
        {
            var element = parent.Element(_reportNamespace + elementName);
            if (element != null)
                element.Value = value;
            else
                parent.Add(new XElement(_reportNamespace + elementName, value));
        }
    }

    #endregion

    #region Usage Examples

    /// <summary>
    /// Complete usage examples for the RDLC Fluent Builder
    /// </summary>
    public static class RdlcExamples
    {
        /// <summary>
        /// Simple sales report example
        /// </summary>
        public static void CreateSimpleSalesReport()
        {
            RdlcReportBuilder
                .Create("SalesReport")
                .WithSqlDataSource("Sales", "Server=localhost;Database=Sales;Integrated Security=true")
                .WithDataSet("MonthlySales", ds => ds
                    .UsingDataSource("Sales")
                    .WithQuery("SELECT Month, Revenue, Units FROM MonthlySales ORDER BY Month")
                    .WithStringField("Month")
                    .WithDecimalField("Revenue")
                    .WithIntField("Units"))

                .WithSimpleHeader("Monthly Sales Report")
                .WithTitle("Sales Performance", 0, 1, 8)

                .WithLabel("Month:", 0, 2)
                .WithFieldValue("Month", 1.5, 2)
                .WithLabel("Revenue:", 0, 2.5)
                .WithTextBox("FormattedRevenue", tb => tb
                    .WithExpression("Format(Fields!Revenue.Value, \"C\")")
                    .WithBounds(1.5, 2.5, 2, 0.25)
                    .Bold())

                .WithSalesChart("MonthlySales", "Month", "Revenue", 0, 3.5, 8, 3)

                .WithSimpleFooter("© 2025 Sales Corp")
                .WithLayout(layout => layout.Letter().WithMargins(1))
                .SaveTo("SalesReport.rdlc");
        }

        /// <summary>
        /// Dashboard with multiple charts
        /// </summary>
        public static void CreateDashboard()
        {
            RdlcReportBuilder
                .Create("Dashboard")
                .WithSqlDataSource("Analytics", "connection_string_here")
                .WithDataSet("SalesData", ds => ds
                    .UsingDataSource("Analytics")
                    .WithQuery("SELECT Region, Product, Revenue, Profit FROM Sales")
                    .WithStringField("Region")
                    .WithStringField("Product")
                    .WithDecimalField("Revenue")
                    .WithDecimalField("Profit"))

                .WithTitle("Executive Dashboard", 0, 0.5, 8)

                // Top row charts
                .WithSalesChart("SalesData", "Region", "Revenue", 0, 1.5, 4, 2.5)
                .WithPieChart("SalesData", "Product", "Profit", 4.5, 1.5, 3.5)

                // Separator line
                .WithHorizontalLine(0, 4.2, 8)

                // Bottom chart
                .WithLineChart("SalesData", "Region", "Revenue", 0, 4.7, 8, 3)

                .WithLayout(layout => layout.Letter().WithMargins(0.5))
                .SaveTo("Dashboard.rdlc");
        }

        /// <summary>
        /// Employee directory with headers and footers
        /// </summary>
        public static void CreateEmployeeDirectory()
        {
            RdlcReportBuilder
                .Create("EmployeeDirectory")
                .WithSqlDataSource("HR", "connection_string")
                .WithDataSet("Employees", ds => ds
                    .UsingDataSource("HR")
                    .WithQuery("SELECT EmployeeID, FirstName, LastName, Department, Email FROM Employees")
                    .WithIntField("EmployeeID")
                    .WithStringField("FirstName")
                    .WithStringField("LastName")
                    .WithStringField("Department")
                    .WithStringField("Email"))

                .WithHeader(header => header
                    .WithTitle("Employee Directory", 0, 0, 6)
                    .WithTextBox("Date", tb => tb
                        .WithExpression("Format(Now(), \"MM/dd/yyyy\")")
                        .WithBounds(6, 0, 2, 0.25))
                    .WithHeight(0.6))

                .WithLabel("Employee ID:", 0, 1.5)
                .WithFieldValue("EmployeeID", 2, 1.5)

                .WithLabel("Name:", 0, 1.8)
                .WithTextBox("FullName", tb => tb
                    .WithExpression("Fields!FirstName.Value & \" \" & Fields!LastName.Value")
                    .WithBounds(2, 1.8, 3, 0.25)
                    .Bold())

                .WithLabel("Department:", 0, 2.1)
                .WithFieldValue("Department", 2, 2.1, 2.5)

                .WithLabel("Email:", 0, 2.4)
                .WithFieldValue("Email", 2, 2.4, 4)

                .WithFooter(footer => footer
                    .WithTextBox("Confidential", tb => tb
                        .WithText("HR Department - Confidential")
                        .WithBounds(0, 0, 4, 0.25)
                        .WithFontSize(8))
                    .WithPageNumber()
                    .WithHeight(0.4))

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo("EmployeeDirectory.rdlc");
        }

        /// <summary>
        /// Invoice template with professional styling
        /// </summary>
        public static void CreateInvoiceTemplate()
        {
            RdlcReportBuilder
                .Create("Invoice")
                .WithSqlDataSource("Billing", "connection_string")
                .WithDataSet("InvoiceData", ds => ds
                    .UsingDataSource("Billing")
                    .WithQuery(@"SELECT InvoiceNumber, CustomerName, InvoiceDate, 
                                       ItemDescription, Quantity, UnitPrice, Total FROM Invoice")
                    .WithStringField("InvoiceNumber")
                    .WithStringField("CustomerName")
                    .WithDateTimeField("InvoiceDate")
                    .WithStringField("ItemDescription")
                    .WithIntField("Quantity")
                    .WithDecimalField("UnitPrice")
                    .WithDecimalField("Total"))

                // Company header
                .WithTextBox("CompanyName", tb => tb
                    .WithText("ACME Corporation")
                    .WithBounds(0, 0.5, 4, 0.5)
                    .WithFontSize(18)
                    .Bold())

                // Invoice title
                .WithTextBox("InvoiceTitle", tb => tb
                    .WithText("INVOICE")
                    .WithBounds(5, 0.5, 3, 0.5)
                    .WithFontSize(20)
                    .Bold())

                .WithLabel("Invoice #:", 5, 1.2)
                .WithFieldValue("InvoiceNumber", 6, 1.2, 2)

                .WithLabel("Date:", 5, 1.5)
                .WithTextBox("FormattedDate", tb => tb
                    .WithExpression("Format(Fields!InvoiceDate.Value, \"MM/dd/yyyy\")")
                    .WithBounds(6, 1.5, 2, 0.25))

                // Separator
                .WithHorizontalLine(0, 2, 8)

                // Customer info
                .WithLabel("Bill To:", 0, 2.3)
                .WithFieldValue("CustomerName", 0, 2.6, 4)

                // Line items
                .WithLabel("Description", 0, 3.5)
                .WithLabel("Qty", 4, 3.5, 1)
                .WithLabel("Price", 5, 3.5, 1)
                .WithLabel("Total", 6.5, 3.5, 1.5)

                .WithHorizontalLine(0, 3.8, 8)

                .WithFieldValue("ItemDescription", 0, 4.1, 4)
                .WithFieldValue("Quantity", 4, 4.1, 1)
                .WithTextBox("FormattedPrice", tb => tb
                    .WithExpression("Format(Fields!UnitPrice.Value, \"C\")")
                    .WithBounds(5, 4.1, 1, 0.25))
                .WithTextBox("FormattedTotal", tb => tb
                    .WithExpression("Format(Fields!Total.Value, \"C\")")
                    .WithBounds(6.5, 4.1, 1.5, 0.25)
                    .Bold())

                .WithHorizontalLine(4, 5, 4)

                .WithTextBox("GrandTotal", tb => tb
                    .WithText("TOTAL:")
                    .WithBounds(5.5, 5.3, 1, 0.3)
                    .Bold())
                .WithTextBox("GrandTotalAmount", tb => tb
                    .WithExpression("Format(Fields!Total.Value, \"C\")")
                    .WithBounds(6.5, 5.3, 1.5, 0.3)
                    .Bold()
                    .WithFontSize(12))

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo("Invoice.rdlc");
        }

        /// <summary>
        /// Product catalog with images (if available)
        /// </summary>
        public static void CreateProductCatalog()
        {
            RdlcReportBuilder
                .Create("ProductCatalog")
                .WithSqlDataSource("Products", "connection_string")
                .WithDataSet("ProductData", ds => ds
                    .UsingDataSource("Products")
                    .WithQuery("SELECT ProductName, Description, Price, Category FROM Products")
                    .WithStringField("ProductName")
                    .WithStringField("Description")
                    .WithDecimalField("Price")
                    .WithStringField("Category"))

                .WithTitle("Product Catalog 2025", 0, 0.5, 8)

                .WithTextBox("ProductTitle", tb => tb
                    .WithFieldValue("ProductName")
                    .WithBounds(0, 1.5, 4, 0.4)
                    .WithFontSize(14)
                    .Bold())

                .WithLabel("Category:", 0, 2)
                .WithFieldValue("Category", 1.5, 2, 2.5)

                .WithLabel("Price:", 0, 2.3)
                .WithTextBox("FormattedPrice", tb => tb
                    .WithExpression("Format(Fields!Price.Value, \"C\")")
                    .WithBounds(1.5, 2.3, 2, 0.25)
                    .Bold()
                    .WithFontSize(12))

                .WithLabel("Description:", 0, 2.7)
                .WithFieldValue("Description", 0, 3, 8, 1.5)

                // Decorative border
                .WithLine("ProductBorder", line => line
                    .WithBounds(0, 1.3, 8, 3.5)
                    .WithColor("LightBlue")
                    .AsSeparator())

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo("ProductCatalog.rdlc");
        }
    }

    #endregion
}