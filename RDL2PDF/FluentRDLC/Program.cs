using System;
using System.IO;
using FluentRDLC;
using FluentRDLC.Renderer;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("RDLC to PDF Renderer Demo with Headers, Footers, and Indicators");
            Console.WriteLine("================================================================");

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

            Console.WriteLine("\n=== Enhanced Features ===");
            Console.WriteLine("✓ Report Headers with title and date");
            Console.WriteLine("✓ Report Footers with page numbers");
            Console.WriteLine("✓ Gauge indicators with color ranges");
            Console.WriteLine("✓ Linear gauges for progress tracking");
            Console.WriteLine("✓ Data bars for visual comparison");
            Console.WriteLine("✓ Sparklines for trend visualization");
            Console.WriteLine("✓ Page number expressions (=PageNumber)");
            Console.WriteLine("✓ Dynamic expressions and formatting");
            Console.WriteLine("✓ Multi-range color coding for indicators");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}