using FluentRDLC.Renderer;

namespace FluetRDLC.Test
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void EmployeeDataSet()
        {
            // Test 1: Employee Report with Header/Footer
            var renderer = new RDLCRenderer();
            var employeeData = RDLCSampleHelper.CreateSampleEmployeeData();
            renderer.AddDataSource("EmployeeDataSet", employeeData);
            renderer.AddParameter("ReportTitle", "Employee Report - Enhanced with Header/Footer");

            Console.WriteLine("Rendering employee report with header and footer...");
            var rdlcContent = RDLCSampleHelper.CreateEmployeeReportWithHeaderFooter();
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);

            var outputPath = @"C:\Output\EmployeeReportEnhanced.pdf";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, pdfBytes);
            Console.WriteLine($"Enhanced employee report saved to: {outputPath}");
            Console.WriteLine($"File size: {pdfBytes.Length} bytes");

        }

        [TestMethod]
        public void IndicatorDashboard()
        {
            try
            {

                // Test 2: Indicator Dashboard Report
                var indicatorRenderer = new RDLCRenderer();
                var metricsData = RDLCSampleHelper.CreateSampleMetricsData();
                indicatorRenderer.AddDataSource("MetricsDataSet", metricsData);

                Console.WriteLine("\nRendering indicator dashboard report...");
                var indicatorRdlc = RDLCSampleHelper.CreateIndicatorReport();
                var indicatorPdfBytes = indicatorRenderer.RenderToPdfFromContent(indicatorRdlc);
                var indicatorOutputPath = @"C:\Output\IndicatorDashboard.pdf";
                File.WriteAllBytes(indicatorOutputPath, indicatorPdfBytes);
                Console.WriteLine($"Indicator dashboard saved to: {indicatorOutputPath}");
                Console.WriteLine($"Dashboard file size: {indicatorPdfBytes.Length} bytes");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

        }
    }
}
