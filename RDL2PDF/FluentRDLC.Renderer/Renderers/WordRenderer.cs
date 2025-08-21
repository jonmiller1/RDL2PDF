using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FluentRDLC.Renderer.Renderers
{
    public partial class WordRenderer : IRenderer
    {
        public RenderFormat Format => RenderFormat.Word;

        public string GetFileExtension() => ".html";
        public string GetMimeType() => "text/html";

        public byte[] Render(RenderContext context)
        {
            var htmlContent = GenerateWordHtml(context);
            return Encoding.UTF8.GetBytes(htmlContent);
        }

        private string GenerateWordHtml(RenderContext context)
        {
            var html = new StringBuilder();
            
            // Word-compatible HTML header
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word'>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>RDLC Report</title>");
            html.AppendLine("<!--[if gte mso 9]>");
            html.AppendLine("<xml>");
            html.AppendLine("<w:WordDocument>");
            html.AppendLine("<w:View>Print</w:View>");
            html.AppendLine("<w:Zoom>90</w:Zoom>");
            html.AppendLine("<w:DoNotPromptForConvert/>");
            html.AppendLine("<w:DoNotShowRevisions/>");
            html.AppendLine("<w:DoNotPrintRevisions/>");
            html.AppendLine("<w:DoNotShowMarkup/>");
            html.AppendLine("<w:DoNotShowComments/>");
            html.AppendLine("<w:DoNotShowInsertionsAndDeletions/>");
            html.AppendLine("<w:DoNotShowPropertyChanges/>");
            html.AppendLine("</w:WordDocument>");
            html.AppendLine("</xml>");
            html.AppendLine("<![endif]-->");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; font-size: 11pt; margin: 1in; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 10px 0; }");
            html.AppendLine("th, td { border: 1px solid #000; padding: 5px; text-align: left; }");
            html.AppendLine("th { background-color: #f0f0f0; font-weight: bold; }");
            html.AppendLine("h1, h2, h3 { color: #333; }");
            html.AppendLine(".header { border-bottom: 2px solid #000; margin-bottom: 20px; }");
            html.AppendLine(".footer { border-top: 2px solid #000; margin-top: 20px; }");
            html.AppendLine(".chart-placeholder { border: 1px dashed #ccc; padding: 20px; text-align: center; color: #666; }");
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
                RenderReportItemsToHtml(html, reportElement, context);
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
                html.AppendLine($"<p>{System.Web.HttpUtility.HtmlEncode(text)}</p>");
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
            var title = chartElement.Element(context.RdlcNamespace + "ChartAreas")?.Element(context.RdlcNamespace + "ChartArea")?.Element(context.RdlcNamespace + "ChartTitle")?.Element(context.RdlcNamespace + "Caption")?.Value ?? "Chart";
            
            html.AppendLine("<div class='chart-placeholder'>");
            html.AppendLine($"<h3>{System.Web.HttpUtility.HtmlEncode(title)}</h3>");
            html.AppendLine("<p>[Chart placeholder - charts not supported in Word format]</p>");
            html.AppendLine("</div>");
        }

        private void RenderGaugePanelToHtml(StringBuilder html, XElement gaugePanelElement, RenderContext context)
        {
            html.AppendLine("<div class='chart-placeholder'>");
            html.AppendLine("<h3>Gauge</h3>");
            html.AppendLine("<p>[Gauge placeholder - gauges not supported in Word format]</p>");
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
                                html.AppendLine($"<img src='data:{mimeType};base64,{base64Data}' alt='{System.Web.HttpUtility.HtmlEncode(imageName)}' style='max-width: 100%;' />");
                            }
                            catch
                            {
                                html.AppendLine($"<p style='color: red;'>[Image could not be loaded: {System.Web.HttpUtility.HtmlEncode(imageName)}]</p>");
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

        [GeneratedRegex(@"=Fields!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex FieldRegex();

        [GeneratedRegex(@"=Parameters!(\w+)\.Value", RegexOptions.IgnoreCase)]
        private static partial Regex ParameterRegex();

        [GeneratedRegex(@"=PageNumber", RegexOptions.IgnoreCase)]
        private static partial Regex PageNumberRegex();
    }
}