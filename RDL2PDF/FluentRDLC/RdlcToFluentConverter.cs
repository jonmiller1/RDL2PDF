using System.Text;
using System.Xml.Linq;

namespace FluentRDLC;

public class RdlcToFluentConverter
{
    private readonly Dictionary<string, string> _dataSets = new();
    private readonly Dictionary<string, string> _parameters = new();
    private readonly List<string> _embeddedImages = new();
    private int _indentLevel = 0;

    public string ConvertToFluentCode(string rdlcXml, string reportName = "ConvertedReport")
    {
        var doc = XDocument.Parse(rdlcXml);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        
        var sb = new StringBuilder();
        sb.AppendLine("// Generated fluent code from RDLC");
        sb.AppendLine("using FluentRDLC;");
        sb.AppendLine();
        sb.AppendLine($"var reportBuilder = RdlcReportBuilder.Create(\"{reportName}\")");
        
        _indentLevel = 1;
        
        // Parse and add layout information
        AddLayoutCode(doc, ns, sb);
        
        // Parse and add data sources
        AddDataSourcesCode(doc, ns, sb);
        
        // Parse and add data sets
        AddDataSetsCode(doc, ns, sb);
        
        // Parse and add embedded images
        AddEmbeddedImagesCode(doc, ns, sb);
        
        // Parse and add report items
        AddReportItemsCode(doc, ns, sb);
        
        sb.AppendLine(";");
        sb.AppendLine();
        sb.AppendLine("return reportBuilder.ToXml();");
        
        return sb.ToString();
    }
    
    private void AddLayoutCode(XDocument doc, XNamespace ns, StringBuilder sb)
    {
        var reportElement = doc.Root;
        if (reportElement?.Name.LocalName != "Report") 
        {
            // Try to find Report element if root is not Report
            reportElement = doc.Root?.Element(ns + "Report");
        }
        if (reportElement == null) return;
        
        var pageHeight = reportElement.Element(ns + "PageHeight")?.Value ?? "11in";
        var pageWidth = reportElement.Element(ns + "PageWidth")?.Value ?? "8.5in";
        var leftMargin = reportElement.Element(ns + "LeftMargin")?.Value ?? "0.5in";
        var rightMargin = reportElement.Element(ns + "RightMargin")?.Value ?? "0.5in";
        var topMargin = reportElement.Element(ns + "TopMargin")?.Value ?? "0.5in";
        var bottomMargin = reportElement.Element(ns + "BottomMargin")?.Value ?? "0.5in";
        
        var bodyElement = reportElement.Element(ns + "Body");
        var bodyHeight = bodyElement?.Element(ns + "Height")?.Value ?? pageHeight;
        
        sb.AppendLine($"{GetIndent()}.WithLayout(layout => layout");
        _indentLevel++;
        sb.AppendLine($"{GetIndent()}.WithPageSize({ParseSize(pageWidth)}, {ParseSize(pageHeight)})");
        sb.AppendLine($"{GetIndent()}.WithMargins({ParseSize(leftMargin)}, {ParseSize(rightMargin)}, {ParseSize(topMargin)}, {ParseSize(bottomMargin)})");
        sb.AppendLine($"{GetIndent()}.WithBodyHeight({ParseSize(bodyHeight)}))");
        _indentLevel--;
    }
    
    private void AddDataSourcesCode(XDocument doc, XNamespace ns, StringBuilder sb)
    {
        var reportElement = doc.Root?.Name.LocalName == "Report" ? doc.Root : doc.Root?.Element(ns + "Report");
        var dataSources = reportElement?.Element(ns + "DataSources")?.Elements(ns + "DataSource");
        if (dataSources == null) return;
        
        foreach (var ds in dataSources)
        {
            var name = ds.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;
            
            var connectionString = ds.Element(ns + "ConnectionProperties")?.Element(ns + "ConnectString")?.Value;
            var dataProvider = ds.Element(ns + "ConnectionProperties")?.Element(ns + "DataProvider")?.Value;
            
            if (dataProvider == "System.Data.DataSet")
            {
                sb.AppendLine($"{GetIndent()}.WithObjectDataSource<object>(\"{name}\")");
            }
            else
            {
                sb.AppendLine($"{GetIndent()}.WithDataSource(\"{name}\", \"{connectionString ?? ""}\")");
            }
        }
    }
    
    private void AddDataSetsCode(XDocument doc, XNamespace ns, StringBuilder sb)
    {
        var reportElement = doc.Root?.Name.LocalName == "Report" ? doc.Root : doc.Root?.Element(ns + "Report");
        var dataSets = reportElement?.Element(ns + "DataSets")?.Elements(ns + "DataSet");
        if (dataSets == null) return;
        
        foreach (var ds in dataSets)
        {
            var name = ds.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;
            
            var dataSourceName = ds.Element(ns + "Query")?.Element(ns + "DataSourceName")?.Value;
            var commandText = ds.Element(ns + "Query")?.Element(ns + "CommandText")?.Value;
            
            _dataSets[name] = dataSourceName ?? "";
            
            sb.AppendLine($"{GetIndent()}.WithDataSet(\"{name}\", ds => ds");
            _indentLevel++;
            if (!string.IsNullOrEmpty(dataSourceName))
            {
                sb.AppendLine($"{GetIndent()}.UsingDataSource(\"{dataSourceName}\")");
            }
            if (!string.IsNullOrEmpty(commandText))
            {
                sb.AppendLine($"{GetIndent()}.WithQuery(\"{EscapeString(commandText)}\")");
            }
            
            // Add fields if present
            var fields = ds.Element(ns + "Fields")?.Elements(ns + "Field");
            if (fields?.Any() == true)
            {
                foreach (var field in fields)
                {
                    var fieldName = field.Attribute("Name")?.Value;
                    var dataField = field.Element(ns + "DataField")?.Value;
                    if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(dataField))
                    {
                        sb.AppendLine($"{GetIndent()}.WithField(\"{fieldName}\", \"{dataField}\")");
                    }
                }
            }
            _indentLevel--;
            sb.AppendLine($"{GetIndent()})");
        }
    }
    
    private void AddEmbeddedImagesCode(XDocument doc, XNamespace ns, StringBuilder sb)
    {
        var reportElement = doc.Root?.Name.LocalName == "Report" ? doc.Root : doc.Root?.Element(ns + "Report");
        var embeddedImages = reportElement?.Element(ns + "EmbeddedImages")?.Elements(ns + "EmbeddedImage");
        if (embeddedImages == null) return;
        
        foreach (var img in embeddedImages)
        {
            var name = img.Attribute("Name")?.Value;
            var mimeType = img.Attribute("MIMEType")?.Value;
            var imageData = img.Element(ns + "ImageData")?.Value;
            
            if (!string.IsNullOrEmpty(name))
            {
                _embeddedImages.Add(name);
                sb.AppendLine($"{GetIndent()}.WithEmbeddedImageFromBytes(\"{name}\", Convert.FromBase64String(\"{imageData}\"), \"{mimeType}\")");;
            }
        }
    }
    
    private void AddReportItemsCode(XDocument doc, XNamespace ns, StringBuilder sb)
    {
        var reportElement = doc.Root?.Name.LocalName == "Report" ? doc.Root : doc.Root?.Element(ns + "Report");
        var reportItems = reportElement?.Element(ns + "Body")?.Element(ns + "ReportItems");
        if (reportItems == null) return;
        
        // Process Textboxes
        var textboxes = reportItems.Elements(ns + "Textbox");
        foreach (var textbox in textboxes)
        {
            AddTextboxCode(textbox, ns, sb);
        }
        
        // Process Images
        var images = reportItems.Elements(ns + "Image");
        foreach (var image in images)
        {
            AddImageCode(image, ns, sb);
        }
        
        // Process Tablix
        var tablixes = reportItems.Elements(ns + "Tablix");
        foreach (var tablix in tablixes)
        {
            AddTablixCode(tablix, ns, sb);
        }
    }
    
    private void AddTextboxCode(XElement textbox, XNamespace ns, StringBuilder sb)
    {
        var name = textbox.Attribute("Name")?.Value;
        if (string.IsNullOrEmpty(name)) return;
        
        var textRun = textbox.Element(ns + "Paragraphs")?.Element(ns + "Paragraph")?.Element(ns + "TextRuns")?.Element(ns + "TextRun");
        var value = textRun?.Element(ns + "Value")?.Value;
        var style = textbox.Element(ns + "Style") ?? textRun?.Element(ns + "Style");
        var bounds = GetElementBounds(textbox, ns);
        
        sb.AppendLine($"{GetIndent()}.WithTextBox(\"{name}\", tb => tb");
        _indentLevel++;
        
        if (!string.IsNullOrEmpty(value))
        {
            if (value.StartsWith("="))
            {
                sb.AppendLine($"{GetIndent()}.WithExpression(\"{EscapeString(value)}\")");
            }
            else
            {
                sb.AppendLine($"{GetIndent()}.WithText(\"{EscapeString(value)}\")");
            }
        }
        
        if (bounds != null)
        {
            sb.AppendLine($"{GetIndent()}.WithBounds({bounds.Left}, {bounds.Top}, {bounds.Width}, {bounds.Height})");
        }
        
        AddStyleCode(style, ns, sb);
        
        _indentLevel--;
        sb.AppendLine($"{GetIndent()})");
    }
    
    private void AddImageCode(XElement image, XNamespace ns, StringBuilder sb)
    {
        var name = image.Attribute("Name")?.Value;
        if (string.IsNullOrEmpty(name)) return;
        
        var source = image.Element(ns + "Source")?.Value;
        var value = image.Element(ns + "Value")?.Value;
        var bounds = GetElementBounds(image, ns);
        
        sb.AppendLine($"{GetIndent()}.WithImage(\"{name}\", img => img");
        _indentLevel++;
        
        if (source == "Embedded" && !string.IsNullOrEmpty(value))
        {
            sb.AppendLine($"{GetIndent()}.WithEmbeddedImageSource(\"{value}\")");
        }
        else if (!string.IsNullOrEmpty(value))
        {
            sb.AppendLine($"{GetIndent()}.WithImageSource(\"{EscapeString(value)}\")");
        }
        
        if (bounds != null)
        {
            sb.AppendLine($"{GetIndent()}.WithBounds({bounds.Left}, {bounds.Top}, {bounds.Width}, {bounds.Height})");
        }
        
        _indentLevel--;
        sb.AppendLine($"{GetIndent()})");
    }
    
    private void AddTablixCode(XElement tablix, XNamespace ns, StringBuilder sb)
    {
        var name = tablix.Attribute("Name")?.Value;
        if (string.IsNullOrEmpty(name)) return;
        
        var dataSetName = tablix.Element(ns + "DataSetName")?.Value;
        var bounds = GetElementBounds(tablix, ns);
        
        sb.AppendLine($"{GetIndent()}.WithTablix(\"{name}\", tbl => tbl");
        _indentLevel++;
        
        if (!string.IsNullOrEmpty(dataSetName))
        {
            sb.AppendLine($"{GetIndent()}.WithDataSet(\"{dataSetName}\")");
        }
        
        if (bounds != null)
        {
            sb.AppendLine($"{GetIndent()}.WithBounds({bounds.Left}, {bounds.Top}, {bounds.Width}, {bounds.Height})");
        }
        
        // Add columns
        var columns = tablix.Element(ns + "TablixBody")?.Element(ns + "TablixColumns")?.Elements(ns + "TablixColumn");
        if (columns?.Any() == true)
        {
            foreach (var column in columns)
            {
                var width = column.Element(ns + "Width")?.Value;
                if (!string.IsNullOrEmpty(width))
                {
                    sb.AppendLine($"{GetIndent()}.WithColumn({ParseSize(width)})");
                }
            }
        }
        
        // Add header row if present
        var headerRow = tablix.Element(ns + "TablixBody")?.Element(ns + "TablixRows")?.Elements(ns + "TablixRow")?.FirstOrDefault();
        if (headerRow != null)
        {
            var headerCells = headerRow.Element(ns + "TablixCells")?.Elements(ns + "TablixCell");
            var headerHeight = headerRow.Element(ns + "Height")?.Value;
            var height = ParseSize(headerHeight ?? "0.25in");
            
            if (headerCells?.Any() == true)
            {
                var headerValues = headerCells.Select(cell => GetTablixCellValue(cell, ns)).Where(v => !string.IsNullOrEmpty(v)).ToArray();
                if (headerValues.Length > 0)
                {
                    sb.AppendLine($"{GetIndent()}.WithHeaderRow(row => row");
                    _indentLevel++;
                    foreach (var value in headerValues)
                    {
                        sb.AppendLine($"{GetIndent()}.WithCell(\"{EscapeString(value)}\")");
                    }
                    _indentLevel--;
                    sb.AppendLine($"{GetIndent()})");
                }
            }
        }
        
        // Add data rows
        var dataRows = tablix.Element(ns + "TablixBody")?.Element(ns + "TablixRows")?.Elements(ns + "TablixRow")?.Skip(1);
        if (dataRows?.Any() == true)
        {
            foreach (var row in dataRows)
            {
                var dataCells = row.Element(ns + "TablixCells")?.Elements(ns + "TablixCell");
                var rowHeight = row.Element(ns + "Height")?.Value;
                var height = ParseSize(rowHeight ?? "0.25in");
                
                if (dataCells?.Any() == true)
                {
                    var dataValues = dataCells.Select(cell => GetTablixCellValue(cell, ns)).Where(v => !string.IsNullOrEmpty(v)).ToArray();
                    if (dataValues.Length > 0)
                    {
                        sb.AppendLine($"{GetIndent()}.WithDataRow(row => row");
                        _indentLevel++;
                        foreach (var value in dataValues)
                        {
                            sb.AppendLine($"{GetIndent()}.WithCell(\"{EscapeString(value)}\")");
                        }
                        _indentLevel--;
                        sb.AppendLine($"{GetIndent()})");
                    }
                }
            }
        }
        
        _indentLevel--;
        sb.AppendLine($"{GetIndent()})");
    }
    
    private void AddStyleCode(XElement? style, XNamespace ns, StringBuilder sb)
    {
        if (style == null) return;
        
        var fontSize = style.Element(ns + "FontSize")?.Value;
        var fontWeight = style.Element(ns + "FontWeight")?.Value;
        var fontStyle = style.Element(ns + "FontStyle")?.Value;
        var color = style.Element(ns + "Color")?.Value;
        var backgroundColor = style.Element(ns + "BackgroundColor")?.Value;
        var textAlign = style.Element(ns + "TextAlign")?.Value;
        
        if (!string.IsNullOrEmpty(fontSize))
        {
            sb.AppendLine($"{GetIndent()}.WithFontSize({ParseSize(fontSize)})");
        }
        
        if (fontWeight?.Equals("Bold", StringComparison.OrdinalIgnoreCase) == true)
        {
            sb.AppendLine($"{GetIndent()}.Bold()");
        }
        
        if (fontStyle?.Equals("Italic", StringComparison.OrdinalIgnoreCase) == true)
        {
            sb.AppendLine($"{GetIndent()}.Italic()");
        }
        
        if (!string.IsNullOrEmpty(color))
        {
            sb.AppendLine($"{GetIndent()}.WithColor(\"{color}\")");
        }
        
        if (!string.IsNullOrEmpty(backgroundColor))
        {
            sb.AppendLine($"{GetIndent()}.WithBackgroundColor(\"{backgroundColor}\")");
        }
        
        if (!string.IsNullOrEmpty(textAlign))
        {
            sb.AppendLine($"{GetIndent()}.WithTextAlign(\"{textAlign}\")");
        }
    }
    
    private ElementBounds? GetElementBounds(XElement element, XNamespace ns)
    {
        var left = element.Element(ns + "Left")?.Value;
        var top = element.Element(ns + "Top")?.Value;
        var width = element.Element(ns + "Width")?.Value;
        var height = element.Element(ns + "Height")?.Value;
        
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(top) || 
            string.IsNullOrEmpty(width) || string.IsNullOrEmpty(height))
            return null;
            
        return new ElementBounds
        {
            Left = ParseSize(left),
            Top = ParseSize(top),
            Width = ParseSize(width),
            Height = ParseSize(height)
        };
    }
    
    private string GetTablixCellValue(XElement cell, XNamespace ns)
    {
        return cell.Element(ns + "CellContents")?.Element(ns + "Textbox")?.Element(ns + "Paragraphs")?.Element(ns + "Paragraph")?.Element(ns + "TextRuns")?.Element(ns + "TextRun")?.Element(ns + "Value")?.Value ?? "";
    }
    
    private double ParseSize(string size)
    {
        if (string.IsNullOrEmpty(size)) return 0;
        
        size = size.Replace("in", "").Replace("cm", "").Replace("pt", "");
        if (double.TryParse(size, out var result))
        {
            return result;
        }
        return 0;
    }
    
    private string EscapeString(string input)
    {
        return input?.Replace("\"", "\\\"") ?? "";
    }
    
    private string GetIndent()
    {
        return new string(' ', _indentLevel * 4);
    }
    
    private class ElementBounds
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}