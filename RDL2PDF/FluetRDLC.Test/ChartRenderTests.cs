using FluentRDLC;
using FluentRDLC.Renderer;
using System.Data;

namespace FluetRDLC.Test
{
    [TestClass]
    public sealed class ChartRenderTests
    {
        private string GetOutputPath(string fileName)
        {
            var outputDir = @"C:\Output\Charts";
            Directory.CreateDirectory(outputDir);
            return Path.Combine(outputDir, fileName);
        }

        [TestMethod]
        public void CreateColumnChart()
        {
            // Create object-based sales data for column chart
            var salesData = TestDataFactory.CreateSalesData();
            
            RdlcReportBuilder
                .Create("ColumnChartReport")
                .WithObjectDataSource<SalesRecord>("SalesDB")
                .WithDataSet("SalesData", ds => ds
                    .UsingDataSource("SalesDB")
                    .WithQuery("") // Not needed for object data sources
                    .WithFieldsFromType<SalesRecord>())

                .WithTitle("Monthly Revenue - Column Chart (Object Data)", 0, 0.5, 8)
                .WithSalesChart("SalesData", "Month", "Revenue", 1, 2, 6, 4)

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("ColumnChart_Object.rdlc"));

            // Render to PDF using object data source
            var renderer = new RDLCRenderer();
            renderer.AddObjectDataSource("SalesData", salesData);
            var rdlcContent = File.ReadAllText(GetOutputPath("ColumnChart_Object.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("ColumnChart_Object.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "PDF should be generated with reasonable size");
        }

        [TestMethod]
        public void CreateBarChart()
        {
            // Create object-based product data for horizontal bar chart
            var productData = TestDataFactory.CreateProductData();
            
            RdlcReportBuilder
                .Create("BarChartReport")
                .WithObjectDataSource<ProductSales>("ProductDB")
                .WithDataSet("ProductData", ds => ds
                    .UsingDataSource("ProductDB")
                    .WithQuery("") // Not needed for object data sources
                    .WithFieldsFromType<ProductSales>())

                .WithTitle("Sales by Category - Bar Chart (Object Data)", 0, 0.5, 8)
                .WithChart("BarChart_Test", chart => chart
                    .UsingDataSet("ProductData")
                    .WithData("Category", "Sales")
                    .WithBounds(1, 2, 6, 4)
                    .AsBar()
                    .WithTitle("Category Performance")
                    .ShowLegend())

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("BarChart_Object.rdlc"));

            // Render to PDF using object data source
            var renderer = new RDLCRenderer();
            renderer.AddObjectDataSource("ProductData", productData);
            var rdlcContent = File.ReadAllText(GetOutputPath("BarChart_Object.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("BarChart_Object.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "PDF should be generated with reasonable size");
        }

        [TestMethod]
        public void CreatePieChart()
        {
            // Create market share data for pie chart
            var marketData = CreateMarketShareData();
            
            RdlcReportBuilder
                .Create("PieChartReport")
                .WithSqlDataSource("MarketDB", "mock_connection_string")
                .WithDataSet("MarketData", ds => ds                    
                    .UsingDataSource("MarketDB")
                    .WithQuery("SELECT Company, MarketShare FROM MarketAnalysis")
                    .WithStringField("Company")
                    .WithDecimalField("MarketShare"))                

                .WithTitle("Market Share Analysis - Pie Chart", 0, 0.5, 8)
                .WithPieChart("MarketData", "Company", "MarketShare", 2, 2, 4)

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("PieChart.rdlc"));

            // Render to PDF
            var renderer = new RDLCRenderer();
            renderer.AddDataSource("MarketData", marketData);
            var rdlcContent = File.ReadAllText(GetOutputPath("PieChart.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("PieChart.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "PDF should be generated with reasonable size");
        }

        [TestMethod]
        public void CreateLineChart()
        {
            // Create trend data for line chart
            var trendData = CreateTrendData();
            
            RdlcReportBuilder
                .Create("LineChartReport")
                .WithSqlDataSource("TrendsDB", "mock_connection_string")
                .WithDataSet("TrendData", ds => ds
                    .UsingDataSource("TrendsDB")
                    .WithQuery("SELECT Period, Value FROM TrendAnalysis")
                    .WithStringField("Period")
                    .WithDecimalField("Value"))

                .WithTitle("Performance Trends - Line Chart", 0, 0.5, 8)
                .WithLineChart("TrendData", "Period", "Value", 1, 2, 6, 3)

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("LineChart.rdlc"));

            // Render to PDF
            var renderer = new RDLCRenderer();
            renderer.AddDataSource("TrendData", trendData);
            var rdlcContent = File.ReadAllText(GetOutputPath("LineChart.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("LineChart.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "PDF should be generated with reasonable size");
        }

        [TestMethod]
        public void CreateMultipleChartsReport()
        {
            // Create comprehensive dashboard with multiple chart types
            var dashboardData = CreateDashboardData();
            
            RdlcReportBuilder
                .Create("MultiChartDashboard")
                .WithSqlDataSource("DashboardDB", "mock_connection_string")
                .WithDataSet("DashboardData", ds => ds
                    .UsingDataSource("DashboardDB")
                    .WithQuery("SELECT Category, Q1, Q2, Q3, Q4 FROM QuarterlyData")
                    .WithStringField("Category")
                    .WithDecimalField("Q1")
                    .WithDecimalField("Q2")
                    .WithDecimalField("Q3")
                    .WithDecimalField("Q4"))

                .WithTitle("Executive Dashboard - Multiple Charts", 0, 0.5, 8)

                // Top row: Column and Pie charts
                .WithSalesChart("DashboardData", "Category", "Q1", 0.5, 1.5, 3.5, 2.5)
                .WithPieChart("DashboardData", "Category", "Q2", 4.5, 1.5, 3)

                // Separator line
                .WithHorizontalLine(0, 4.2, 8)

                // Bottom row: Line chart
                .WithLineChart("DashboardData", "Category", "Q3", 1, 5, 6, 2.5)

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("MultiChartDashboard.rdlc"));

            // Render to PDF
            var renderer = new RDLCRenderer();
            renderer.AddDataSource("DashboardData", dashboardData);
            var rdlcContent = File.ReadAllText(GetOutputPath("MultiChartDashboard.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("MultiChartDashboard.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 2000, "Multi-chart PDF should be larger");
        }

        [TestMethod]
        public void CreateChartWithLargeDataset()
        {
            // Test chart performance with larger dataset
            var largeData = CreateLargeDataset();
            
            RdlcReportBuilder
                .Create("LargeDataChart")
                .WithSqlDataSource("BigDataDB", "mock_connection_string")
                .WithDataSet("LargeData", ds => ds
                    .UsingDataSource("BigDataDB")
                    .WithQuery("SELECT Item, Value FROM BigDataTable")
                    .WithStringField("Item")
                    .WithDecimalField("Value"))

                .WithTitle("Large Dataset Performance Test", 0, 0.5, 8)
                .WithSalesChart("LargeData", "Item", "Value", 1, 2, 6, 5)

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("LargeDataChart.rdlc"));

            // Render to PDF
            var renderer = new RDLCRenderer();
            renderer.AddDataSource("LargeData", largeData);
            var rdlcContent = File.ReadAllText(GetOutputPath("LargeDataChart.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("LargeDataChart.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "Large dataset PDF should be generated");
        }

        // Test data creation methods
        private static DataTable CreateSalesData()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Month", typeof(string));
            dataTable.Columns.Add("Revenue", typeof(decimal));

            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            var revenues = new[] { 125000m, 140000m, 135000m, 180000m, 165000m, 190000m };

            for (int i = 0; i < months.Length; i++)
            {
                dataTable.Rows.Add(months[i], revenues[i]);
            }

            return dataTable;
        }

        private static DataTable CreateProductData()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Category", typeof(string));
            dataTable.Columns.Add("Sales", typeof(decimal));

            dataTable.Rows.Add("Electronics", 450000m);
            dataTable.Rows.Add("Clothing", 320000m);
            dataTable.Rows.Add("Books", 180000m);
            dataTable.Rows.Add("Home & Garden", 275000m);
            dataTable.Rows.Add("Sports", 220000m);

            return dataTable;
        }

        private static DataTable CreateMarketShareData()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Company", typeof(string));
            dataTable.Columns.Add("MarketShare", typeof(decimal));

            dataTable.Rows.Add("Company A", 35.5m);
            dataTable.Rows.Add("Company B", 28.2m);
            dataTable.Rows.Add("Company C", 18.7m);
            dataTable.Rows.Add("Company D", 12.1m);
            dataTable.Rows.Add("Others", 5.5m);

            return dataTable;
        }

        private static DataTable CreateTrendData()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Period", typeof(string));
            dataTable.Columns.Add("Value", typeof(decimal));

            var periods = new[] { "Q1 2023", "Q2 2023", "Q3 2023", "Q4 2023", "Q1 2024", "Q2 2024" };
            var values = new[] { 100m, 120m, 135m, 110m, 145m, 160m };

            for (int i = 0; i < periods.Length; i++)
            {
                dataTable.Rows.Add(periods[i], values[i]);
            }

            return dataTable;
        }

        private static DataTable CreateDashboardData()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Category", typeof(string));
            dataTable.Columns.Add("Q1", typeof(decimal));
            dataTable.Columns.Add("Q2", typeof(decimal));
            dataTable.Columns.Add("Q3", typeof(decimal));
            dataTable.Columns.Add("Q4", typeof(decimal));

            dataTable.Rows.Add("North", 125000m, 140000m, 135000m, 150000m);
            dataTable.Rows.Add("South", 110000m, 125000m, 145000m, 160000m);
            dataTable.Rows.Add("East", 95000m, 105000m, 120000m, 140000m);
            dataTable.Rows.Add("West", 140000m, 155000m, 150000m, 175000m);

            return dataTable;
        }

        private static DataTable CreateLargeDataset()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Item", typeof(string));
            dataTable.Columns.Add("Value", typeof(decimal));

            var random = new Random(42); // Fixed seed for consistent tests
            
            for (int i = 1; i <= 20; i++) // 20 data points
            {
                var itemName = $"Item_{i:D2}";
                var value = (decimal)(random.NextDouble() * 100000 + 10000);
                dataTable.Rows.Add(itemName, Math.Round(value, 2));
            }

            return dataTable;
        }

        [TestMethod]
        public void TestAllChartTypesInOneReport()
        {
            // Create a report with all chart types to test Microcharts rendering
            var testData = CreateMultiMetricData();
            
            RdlcReportBuilder
                .Create("AllChartsTest")
                .WithSqlDataSource("AllChartsDB", "mock_connection_string")
                .WithDataSet("MetricData", ds => ds
                    .UsingDataSource("AllChartsDB")
                    .WithQuery("SELECT Metric, Value1, Value2, Value3 FROM Metrics")
                    .WithStringField("Metric")
                    .WithDecimalField("Value1")
                    .WithDecimalField("Value2")
                    .WithDecimalField("Value3"))

                .WithTitle("All Chart Types - Microcharts Test", 0, 0.5, 8)

                // Column Chart
                .WithChart("ColumnChart", chart => chart
                    .UsingDataSet("MetricData")
                    .WithData("Metric", "Value1")
                    .WithBounds(0.5, 1.5, 3.5, 2)
                    .AsColumn()
                    .WithTitle("Column Chart")
                    .ShowLegend())

                // Bar Chart  
                .WithChart("BarChart", chart => chart
                    .UsingDataSet("MetricData")
                    .WithData("Metric", "Value2")
                    .WithBounds(4.5, 1.5, 3.5, 2)
                    .AsBar()
                    .WithTitle("Bar Chart")
                    .ShowLegend())

                // Line Chart
                .WithChart("LineChart", chart => chart
                    .UsingDataSet("MetricData")
                    .WithData("Metric", "Value3")
                    .WithBounds(0.5, 4, 3.5, 2)
                    .AsLine()
                    .WithTitle("Line Chart")
                    .ShowLegend())

                // Pie Chart
                .WithChart("PieChart", chart => chart
                    .UsingDataSet("MetricData")
                    .WithData("Metric", "Value1")
                    .WithBounds(4.5, 4, 3, 3)
                    .AsPie()
                    .WithTitle("Pie Chart")
                    .ShowLegend())

                .WithLayout(layout => layout.Letter().WithMargins(0.5))
                .SaveTo(GetOutputPath("AllChartsTest.rdlc"));

            // Render to PDF
            var renderer = new RDLCRenderer();
            renderer.AddDataSource("MetricData", testData);
            var rdlcContent = File.ReadAllText(GetOutputPath("AllChartsTest.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("AllChartsTest.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 3000, "PDF with all chart types should be substantial");
        }

        private static DataTable CreateMultiMetricData()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Metric", typeof(string));
            dataTable.Columns.Add("Value1", typeof(decimal));
            dataTable.Columns.Add("Value2", typeof(decimal));
            dataTable.Columns.Add("Value3", typeof(decimal));

            dataTable.Rows.Add("Performance", 85.5m, 78.2m, 92.1m);
            dataTable.Rows.Add("Quality", 92.3m, 89.1m, 94.5m);
            dataTable.Rows.Add("Efficiency", 78.9m, 85.7m, 81.3m);
            dataTable.Rows.Add("Satisfaction", 91.2m, 87.8m, 93.6m);
            dataTable.Rows.Add("Innovation", 76.4m, 82.9m, 88.7m);

            return dataTable;
        }

        [TestMethod]
        public void CreateObjectDataSourceReport()
        {
            // Test multiple object data sources in one report
            var employeeData = TestDataFactory.CreateEmployeeData();
            var salesData = TestDataFactory.CreateSalesData();
            
            RdlcReportBuilder
                .Create("ObjectDataSourceReport")
                .WithObjectDataSource<Employee>("EmployeeDB")
                .WithObjectDataSource<SalesRecord>("SalesDB")
                
                .WithDataSet("Employees", ds => ds
                    .UsingDataSource("EmployeeDB")
                    .WithQuery("") // Not needed for object data sources
                    .WithFieldsFromType<Employee>())
                    
                .WithDataSet("Sales", ds => ds
                    .UsingDataSource("SalesDB")
                    .WithQuery("") // Not needed for object data sources
                    .WithFieldsFromType<SalesRecord>())

                .WithTitle("Employee & Sales Dashboard - Object Data Sources", 0, 0.5, 8)

                // Employee count by department (simulated)
                .WithLabel("Employee Information:", 0, 1.5)
                .WithFieldValue("FirstName", 0, 1.8, 2)
                .WithFieldValue("LastName", 2.5, 1.8, 2)
                .WithFieldValue("Department", 5, 1.8, 2.5)

                .WithHorizontalLine(0, 2.2, 8)

                // Sales chart
                .WithSalesChart("Sales", "Month", "Revenue", 1, 2.7, 6, 3)

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("ObjectDataSourceReport.rdlc"));

            // Render to PDF using object data sources
            var renderer = new RDLCRenderer();
            renderer.AddObjectDataSource("Employees", employeeData);
            renderer.AddObjectDataSource("Sales", salesData);
            var rdlcContent = File.ReadAllText(GetOutputPath("ObjectDataSourceReport.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("ObjectDataSourceReport.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "PDF with object data sources should be generated");
        }

        [TestMethod]
        public void CreateDashboardMetricsChart()
        {
            // Test using DashboardMetric objects for charts
            var metricsData = TestDataFactory.CreateMetricsData();
            
            RdlcReportBuilder
                .Create("DashboardMetricsReport")
                .WithObjectDataSource<DashboardMetric>("MetricsDB")
                .WithDataSet("Metrics", ds => ds
                    .UsingDataSource("MetricsDB")
                    .WithQuery("") // Not needed for object data sources
                    .WithFieldsFromType<DashboardMetric>())

                .WithTitle("Performance Metrics Dashboard", 0, 0.5, 8)

                // Current vs Target chart
                .WithChart("CurrentVsTarget", chart => chart
                    .UsingDataSet("Metrics")
                    .WithData("MetricName", "CurrentValue")
                    .WithBounds(0.5, 1.5, 3.5, 2.5)
                    .AsColumn()
                    .WithTitle("Current Values")
                    .ShowLegend())

                // Target values as pie chart
                .WithChart("TargetDistribution", chart => chart
                    .UsingDataSet("Metrics")
                    .WithData("MetricName", "TargetValue")
                    .WithBounds(4.5, 1.5, 3, 3)
                    .AsPie()
                    .WithTitle("Target Distribution")
                    .ShowLegend())

                .WithLayout(layout => layout.Letter().WithMargins(0.75))
                .SaveTo(GetOutputPath("DashboardMetrics_Object.rdlc"));

            // Render to PDF
            var renderer = new RDLCRenderer();
            renderer.AddObjectDataSource("Metrics", metricsData);
            var rdlcContent = File.ReadAllText(GetOutputPath("DashboardMetrics_Object.rdlc"));
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            File.WriteAllBytes(GetOutputPath("DashboardMetrics_Object.pdf"), pdfBytes);
            
            Assert.IsTrue(pdfBytes.Length > 1000, "Dashboard metrics PDF should be generated");
        }
    }
}