using CustomRDLCRenderer;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RDL2PDF;
using System.Data;
using System.Text;

/// <summary>
/// Example usage program
/// </summary>
class Program
{
    static void Main(string[] args)
    {



        QuestPDF.Settings.License = LicenseType.Community;

        try
        {
            Console.WriteLine("Custom RDLC Renderer with QuestPDF Demo");
            Console.WriteLine("========================================");

            var renderer = new RDLCRenderer();



            // Create sample data
            var employeeData = CreateSampleEmployeeData();
            renderer.AddDataSource("EmployeeDataSet", employeeData);
            renderer.AddParameter("ReportTitle", "Employee Report - QuestPDF Renderer");

            // Render using sample RDLC content
            Console.WriteLine("Rendering employee table report...");
            var rdlcContent = RDLCSampleHelper.CreateSimpleEmployeeReport();
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);

            // Save the PDF
            var outputPath = @"QuestPdfRenderedReport.pdf";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, pdfBytes);

            Console.WriteLine($"Employee report rendered successfully to: {outputPath}");
            Console.WriteLine($"File size: {pdfBytes.Length} bytes");

            // Example 2: Simple text report
            Console.WriteLine("\nRendering simple text report...");
            var textRenderer = new RDLCRenderer();
            textRenderer.AddParameter("ReportTitle", "Simple Text Report");

            var textRdlc = RDLCSampleHelper.CreateSimpleTextReport();
            var textPdfBytes = textRenderer.RenderToPdfFromContent(textRdlc);

            var textOutputPath = @"SimpleTextReport.pdf";
            File.WriteAllBytes(textOutputPath, textPdfBytes);
            Console.WriteLine($"Text report rendered successfully to: {textOutputPath}");

            // Example 3: Advanced renderer with custom options
            Console.WriteLine("\nRendering with advanced options...");
            var options = new RenderingOptions
            {
                DefaultFontFamily = "Times New Roman",
                DefaultFontSize = 10,
                HeaderBackgroundColor = Colors.Blue.Lighten4,
                BorderColor = Colors.Blue.Medium,
                BorderWidth = 1.5f,
                MinimumRowHeight = 25
            };

            var advancedRenderer = new AdvancedRDLCRenderer(options);
            advancedRenderer.AddDataSource("EmployeeDataSet", employeeData);
            advancedRenderer.AddParameter("ReportTitle", "Advanced Styled Report");

            var advancedPdfBytes = advancedRenderer.RenderToPdfFromContent(rdlcContent);
            var advancedOutputPath = @"AdvancedStyledReport.pdf";
            File.WriteAllBytes(advancedOutputPath, advancedPdfBytes);
            Console.WriteLine($"Advanced report rendered successfully to: {advancedOutputPath}");

            // Example 4: Large dataset test
            Console.WriteLine("\nTesting with larger dataset...");
            var largeDataset = CreateLargeEmployeeDataset(100);
            var largeRenderer = new RDLCRenderer();
            largeRenderer.AddDataSource("EmployeeDataSet", largeDataset);
            largeRenderer.AddParameter("ReportTitle", "Large Dataset Report (100 employees)");

            var largePdfBytes = largeRenderer.RenderToPdfFromContent(rdlcContent);
            var largeOutputPath = @"LargeDatasetReport.pdf";
            File.WriteAllBytes(largeOutputPath, largePdfBytes);
            Console.WriteLine($"Large dataset report rendered successfully to: {largeOutputPath}");
            Console.WriteLine($"Large dataset file size: {largePdfBytes.Length} bytes");

            // Example 5: Custom table styling
            Console.WriteLine("\nDemonstrating table features...");
            var customData = CreateCustomStyledData();
            var customRenderer = new RDLCRenderer();
            customRenderer.AddDataSource("EmployeeDataSet", customData);
            customRenderer.AddParameter("ReportTitle", "Custom Styled Employee Report");

            var customPdfBytes = customRenderer.RenderToPdfFromContent(rdlcContent);
            var customOutputPath = @"CustomStyledReport.pdf";
            File.WriteAllBytes(customOutputPath, customPdfBytes);
            Console.WriteLine($"Custom styled report rendered successfully to: {customOutputPath}");

            Console.WriteLine("\n=== Summary ===");
            Console.WriteLine("All reports generated successfully!");
            Console.WriteLine("Key features demonstrated:");
            Console.WriteLine("- RDLC parsing and XML processing");
            Console.WriteLine("- Tablix/Table rendering with headers");
            Console.WriteLine("- Expression processing (Fields, Parameters)");
            Console.WriteLine("- Font styling and formatting");
            Console.WriteLine("- QuestPDF fluent API integration");
            Console.WriteLine("- Automatic page breaks and layout");
            Console.WriteLine("- Large dataset handling");
            Console.WriteLine("- Custom styling options");

            Console.WriteLine("\nQuestPDF Benefits:");
            Console.WriteLine("- Modern fluent API design");
            Console.WriteLine("- Excellent performance");
            Console.WriteLine("- Built-in responsive layouts");
            Console.WriteLine("- Rich styling capabilities");
            Console.WriteLine("- Community license available");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static DataTable CreateSampleEmployeeData()
    {
        var table = new DataTable("Employees");
        table.Columns.Add("EmployeeID", typeof(int));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("Department", typeof(string));
        table.Columns.Add("Salary", typeof(decimal));

        table.Rows.Add(1, "John", "Doe", "IT", 75000);
        table.Rows.Add(2, "Jane", "Smith", "HR", 65000);
        table.Rows.Add(3, "Bob", "Johnson", "Finance", 80000);
        table.Rows.Add(4, "Alice", "Brown", "IT", 72000);
        table.Rows.Add(5, "Charlie", "Wilson", "Marketing", 68000);
        table.Rows.Add(6, "Diana", "Davis", "Sales", 70000);
        table.Rows.Add(7, "Frank", "Miller", "IT", 78000);
        table.Rows.Add(8, "Grace", "Taylor", "HR", 63000);

        return table;
    }

    private static DataTable CreateLargeEmployeeDataset(int count)
    {
        var table = new DataTable("Employees");
        table.Columns.Add("EmployeeID", typeof(int));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("Department", typeof(string));
        table.Columns.Add("Salary", typeof(decimal));

        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Frank", "Grace", "Eve", "David" };
        var lastNames = new[] { "Smith", "Johnson", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor", "Anderson", "Thomas" };
        var departments = new[] { "IT", "HR", "Finance", "Marketing", "Sales", "Operations", "Legal", "R&D" };
        var random = new Random();

        for (int i = 1; i <= count; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var department = departments[random.Next(departments.Length)];
            var salary = random.Next(45000, 95000);

            table.Rows.Add(i, firstName, lastName, department, salary);
        }

        return table;
    }

    private static DataTable CreateCustomStyledData()
    {
        var table = new DataTable("Employees");
        table.Columns.Add("EmployeeID", typeof(int));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("Department", typeof(string));
        table.Columns.Add("Salary", typeof(decimal));

        table.Rows.Add(1, "Sarah", "Connor", "Security", 85000);
        table.Rows.Add(2, "Tony", "Stark", "R&D", 150000);
        table.Rows.Add(3, "Bruce", "Wayne", "Executive", 200000);
        table.Rows.Add(4, "Diana", "Prince", "Legal", 95000);
        table.Rows.Add(5, "Clark", "Kent", "Communications", 70000);

        return table;
    }

    private static void GenXSD()
    {
        var types = new[] { typeof(CustomerReportItem) };
        var xri = new System.Xml.Serialization.XmlReflectionImporter();
        var xss = new System.Xml.Serialization.XmlSchemas();
        var xse = new System.Xml.Serialization.XmlSchemaExporter(xss);
        foreach (var type in types)
        {
            var xtm = xri.ImportTypeMapping(type);
            xse.ExportTypeMapping(xtm);
        }
        using var sw = new System.IO.StreamWriter("ReportItemSchemas.xsd", false, Encoding.UTF8);
        for (int i = 0; i < xss.Count; i++)
        {
            var xs = xss[i];
            xs.Id = "ReportItemSchemas";
            xs.Write(sw);
        }
    }
}