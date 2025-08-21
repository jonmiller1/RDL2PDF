using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FluentRDLC.Renderer.Renderers
{
    public partial class ExcelRenderer : IRenderer
    {
        public RenderFormat Format => RenderFormat.Excel;

        public string GetFileExtension() => ".csv";
        public string GetMimeType() => "text/csv";

        public byte[] Render(RenderContext context)
        {
            var csvContent = GenerateExcelCsv(context);
            return Encoding.UTF8.GetBytes(csvContent);
        }

        private string GenerateExcelXml(RenderContext context)
        {
            var xml = new StringBuilder();
            
            // Excel XML header with proper schema
            xml.AppendLine("<?xml version='1.0' encoding='UTF-8'?>");
            xml.AppendLine("<?mso-application progid='Excel.Sheet'?>");
            xml.AppendLine("<Workbook xmlns='urn:schemas-microsoft-com:office:spreadsheet'");
            xml.AppendLine(" xmlns:o='urn:schemas-microsoft-com:office:office'");
            xml.AppendLine(" xmlns:x='urn:schemas-microsoft-com:office:excel'");
            xml.AppendLine(" xmlns:ss='urn:schemas-microsoft-com:office:spreadsheet'");
            xml.AppendLine(" xmlns:html='http://www.w3.org/TR/REC-html40'>");

            // Document properties
            xml.AppendLine("<DocumentProperties xmlns='urn:schemas-microsoft-com:office:office'>");
            xml.AppendLine("<Title>RDLC Report</Title>");
            xml.AppendLine("<Created>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</Created>");
            xml.AppendLine("</DocumentProperties>");

            // Excel workbook properties
            xml.AppendLine("<ExcelWorkbook xmlns='urn:schemas-microsoft-com:office:excel'>");
            xml.AppendLine("<WindowHeight>12000</WindowHeight>");
            xml.AppendLine("<WindowWidth>15000</WindowWidth>");
            xml.AppendLine("<WindowTopX>240</WindowTopX>");
            xml.AppendLine("<WindowTopY>75</WindowTopY>");
            xml.AppendLine("<ProtectStructure>False</ProtectStructure>");
            xml.AppendLine("<ProtectWindows>False</ProtectWindows>");
            xml.AppendLine("</ExcelWorkbook>");

            // Styles
            xml.AppendLine("<Styles>");
            xml.AppendLine("<Style ss:ID='Default' ss:Name='Normal'>");
            xml.AppendLine("<Alignment ss:Vertical='Bottom'/>");
            xml.AppendLine("<Borders/>");
            xml.AppendLine("<Font ss:FontName='Calibri' x:Family='Swiss' ss:Size='11' ss:Color='#000000'/>");
            xml.AppendLine("<Interior/>");
            xml.AppendLine("<NumberFormat/>");
            xml.AppendLine("<Protection/>");
            xml.AppendLine("</Style>");
            xml.AppendLine("<Style ss:ID='Header'>");
            xml.AppendLine("<Alignment ss:Vertical='Bottom'/>");
            xml.AppendLine("<Borders>");
            xml.AppendLine("<Border ss:Position='Bottom' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("<Border ss:Position='Left' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("<Border ss:Position='Right' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("<Border ss:Position='Top' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("</Borders>");
            xml.AppendLine("<Font ss:FontName='Calibri' x:Family='Swiss' ss:Size='11' ss:Color='#000000' ss:Bold='1'/>");
            xml.AppendLine("<Interior ss:Color='#D9D9D9' ss:Pattern='Solid'/>");
            xml.AppendLine("</Style>");
            xml.AppendLine("<Style ss:ID='Data'>");
            xml.AppendLine("<Alignment ss:Vertical='Bottom'/>");
            xml.AppendLine("<Borders>");
            xml.AppendLine("<Border ss:Position='Bottom' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("<Border ss:Position='Left' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("<Border ss:Position='Right' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("<Border ss:Position='Top' ss:LineStyle='Continuous' ss:Weight='1'/>");
            xml.AppendLine("</Borders>");
            xml.AppendLine("<Font ss:FontName='Calibri' x:Family='Swiss' ss:Size='11' ss:Color='#000000'/>");
            xml.AppendLine("</Style>");
            xml.AppendLine("</Styles>");

            // Worksheet
            xml.AppendLine("<Worksheet ss:Name='Report'>");
            xml.AppendLine("<Table>");

            var currentRow = 1;

            // Header
            var pageHeader = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageHeader");
            if (pageHeader != null)
            {
                currentRow = RenderReportItemsToExcel(xml, pageHeader, context, currentRow);
                currentRow++; // Add spacing
            }

            // Body content
            var reportElement = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "Body");
            if (reportElement != null)
            {
                currentRow = RenderReportItemsToExcel(xml, reportElement, context, currentRow);
            }

            // Footer
            var pageFooter = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageFooter");
            if (pageFooter != null)
            {
                currentRow++; // Add spacing
                RenderReportItemsToExcel(xml, pageFooter, context, currentRow);
            }

            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");
            xml.AppendLine("</Workbook>");
            
            return xml.ToString();
        }

        private int RenderReportItemsToExcel(StringBuilder xml, XElement parent, RenderContext context, int startRow)
        {
            var reportItemsElement = parent.Element(context.RdlcNamespace + "ReportItems");
            if (reportItemsElement == null) return startRow;

            var currentRow = startRow;

            foreach (var item in reportItemsElement.Elements())
            {
                if (item.Name.LocalName == "Textbox")
                {
                    currentRow = RenderTextboxToExcel(xml, item, context, currentRow);
                }
                else if (item.Name.LocalName == "Tablix")
                {
                    currentRow = RenderTablixToExcel(xml, item, context, currentRow);
                }
                else if (item.Name.LocalName == "Chart")
                {
                    currentRow = RenderChartToExcel(xml, item, context, currentRow);
                }
                else if (item.Name.LocalName == "GaugePanel")
                {
                    currentRow = RenderGaugePanelToExcel(xml, item, context, currentRow);
                }
                else if (item.Name.LocalName == "Image")
                {
                    currentRow = RenderImageToExcel(xml, item, context, currentRow);
                }
            }

            return currentRow;
        }

        private int RenderTextboxToExcel(StringBuilder xml, XElement textboxElement, RenderContext context, int row)
        {
            var valueElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");

            if (valueElement != null)
            {
                var text = ProcessTextValue(valueElement.Value, context);
                xml.AppendLine($"<Row ss:Index='{row}'>");
                xml.AppendLine($"<Cell><Data ss:Type='String'>{EscapeXml(text)}</Data></Cell>");
                xml.AppendLine("</Row>");
                return row + 1;
            }

            return row;
        }

        private int RenderTablixToExcel(StringBuilder xml, XElement tablixElement, RenderContext context, int startRow)
        {
            var dataSetName = tablixElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return startRow;

            var dataTable = context.DataSources[dataSetName];
            var headerElement = tablixElement.Element(context.RdlcNamespace + "TablixBody")?.Element(context.RdlcNamespace + "TablixRows")?.Elements().FirstOrDefault();
            var columnsElement = tablixElement.Element(context.RdlcNamespace + "TablixColumnHierarchy")?.Element(context.RdlcNamespace + "TablixMembers");

            if (headerElement == null || columnsElement == null)
                return startRow;

            var currentRow = startRow;

            // Header row
            xml.AppendLine($"<Row ss:Index='{currentRow}'>");
            var headerCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements();
            if (headerCells != null)
            {
                foreach (var cell in headerCells)
                {
                    var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                    
                    var cellText = cellTextElement?.Value ?? "";
                    xml.AppendLine($"<Cell ss:StyleID='Header'><Data ss:Type='String'>{EscapeXml(cellText)}</Data></Cell>");
                }
            }
            xml.AppendLine("</Row>");
            currentRow++;

            // Data rows
            foreach (DataRow row in dataTable.Rows)
            {
                context.CurrentDataRow = row;
                xml.AppendLine($"<Row ss:Index='{currentRow}'>");
                
                var dataCells = headerElement.Element(context.RdlcNamespace + "TablixCells")?.Elements();
                if (dataCells != null)
                {
                    foreach (var cell in dataCells)
                    {
                        var cellTextElement = cell.Element(context.RdlcNamespace + "CellContents")?.Element(context.RdlcNamespace + "Textbox")?.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");
                        
                        var cellText = ProcessTextValue(cellTextElement?.Value ?? "", context);
                        
                        // Try to determine data type
                        var dataType = "String";
                        if (double.TryParse(cellText, out _))
                        {
                            dataType = "Number";
                        }
                        else if (DateTime.TryParse(cellText, out _))
                        {
                            dataType = "DateTime";
                        }
                        
                        xml.AppendLine($"<Cell ss:StyleID='Data'><Data ss:Type='{dataType}'>{EscapeXml(cellText)}</Data></Cell>");
                    }
                }
                xml.AppendLine("</Row>");
                currentRow++;
            }

            return currentRow + 1; // Add spacing after table
        }

        private int RenderChartToExcel(StringBuilder xml, XElement chartElement, RenderContext context, int row)
        {
            var title = chartElement.Element(context.RdlcNamespace + "ChartAreas")?.Element(context.RdlcNamespace + "ChartArea")?.Element(context.RdlcNamespace + "ChartTitle")?.Element(context.RdlcNamespace + "Caption")?.Value ?? "Chart";
            
            xml.AppendLine($"<Row ss:Index='{row}'>");
            xml.AppendLine($"<Cell><Data ss:Type='String'>{EscapeXml(title)}</Data></Cell>");
            xml.AppendLine("</Row>");

            xml.AppendLine($"<Row ss:Index='{row + 1}'>");
            xml.AppendLine("<Cell><Data ss:Type='String'>[Chart placeholder - charts not supported in Excel format]</Data></Cell>");
            xml.AppendLine("</Row>");

            return row + 3; // Title + placeholder + spacing
        }

        private int RenderGaugePanelToExcel(StringBuilder xml, XElement gaugePanelElement, RenderContext context, int row)
        {
            xml.AppendLine($"<Row ss:Index='{row}'>");
            xml.AppendLine("<Cell><Data ss:Type='String'>Gauge</Data></Cell>");
            xml.AppendLine("</Row>");

            xml.AppendLine($"<Row ss:Index='{row + 1}'>");
            xml.AppendLine("<Cell><Data ss:Type='String'>[Gauge placeholder - gauges not supported in Excel format]</Data></Cell>");
            xml.AppendLine("</Row>");

            return row + 3; // Title + placeholder + spacing
        }

        private int RenderImageToExcel(StringBuilder xml, XElement imageElement, RenderContext context, int row)
        {
            var sourceElement = imageElement.Element(context.RdlcNamespace + "Source");
            if (sourceElement?.Value == "Embedded")
            {
                var valueElement = imageElement.Element(context.RdlcNamespace + "Value");
                if (valueElement != null)
                {
                    var imageName = valueElement.Value;
                    xml.AppendLine($"<Row ss:Index='{row}'>");
                    xml.AppendLine($"<Cell><Data ss:Type='String'>[Image: {EscapeXml(imageName)} - images not supported in Excel format]</Data></Cell>");
                    xml.AppendLine("</Row>");
                    return row + 1;
                }
            }

            return row;
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

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&apos;");
        }

        [GeneratedRegex(@"=Fields!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex FieldRegex();

        [GeneratedRegex(@"=Parameters!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex ParameterRegex();

        [GeneratedRegex(@"=PageNumber", RegexOptions.IgnoreCase)]
        private static partial Regex PageNumberRegex();

        private string GenerateExcelCsv(RenderContext context)
        {
            var csv = new StringBuilder();
            
            // Header
            var pageHeader = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageHeader");
            if (pageHeader != null)
            {
                AppendReportItemsAsCsv(csv, pageHeader, context);
            }
            
            // Body content
            var reportElement = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "Body");
            if (reportElement != null)
            {
                AppendReportItemsAsCsv(csv, reportElement, context);
            }
            
            // Footer
            var pageFooter = context.RdlcDocument.Root?.Element(context.RdlcNamespace + "PageFooter");
            if (pageFooter != null)
            {
                AppendReportItemsAsCsv(csv, pageFooter, context);
            }
            
            return csv.ToString();
        }
        
        private void AppendReportItemsAsCsv(StringBuilder csv, XElement parent, RenderContext context)
        {
            var reportItemsElement = parent.Element(context.RdlcNamespace + "ReportItems");
            if (reportItemsElement == null) return;

            foreach (var item in reportItemsElement.Elements())
            {
                if (item.Name.LocalName == "Textbox")
                {
                    AppendTextboxAsCsv(csv, item, context);
                }
                else if (item.Name.LocalName == "Tablix")
                {
                    AppendTablixAsCsv(csv, item, context);
                }
            }
        }
        
        private void AppendTextboxAsCsv(StringBuilder csv, XElement textboxElement, RenderContext context)
        {
            var valueElement = textboxElement.Element(context.RdlcNamespace + "Paragraphs")?.Element(context.RdlcNamespace + "Paragraph")?.Element(context.RdlcNamespace + "TextRuns")?.Element(context.RdlcNamespace + "TextRun")?.Element(context.RdlcNamespace + "Value");

            if (valueElement != null)
            {
                var text = ProcessTextValue(valueElement.Value, context);
                csv.AppendLine(EscapeCsv(text));
            }
        }
        
        private void AppendTablixAsCsv(StringBuilder csv, XElement tablixElement, RenderContext context)
        {
            var dataSetName = tablixElement.Element(context.RdlcNamespace + "DataSetName")?.Value;
            if (string.IsNullOrEmpty(dataSetName) || !context.DataSources.ContainsKey(dataSetName))
                return;

            var dataTable = context.DataSources[dataSetName];
            
            // Header row
            csv.AppendLine(string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(column => EscapeCsv(column.ColumnName))));
            
            // Data rows
            foreach (DataRow row in dataTable.Rows)
            {
                csv.AppendLine(string.Join(",", row.ItemArray.Select(field => EscapeCsv(field?.ToString() ?? ""))));
            }
        }
        
        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
                
            if (text.Contains(',') || text.Contains('"') || text.Contains('\n'))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            
            return text;
        }
    }
}