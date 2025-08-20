using FluentRDLC;
using FluentRDLC.Renderer;

namespace FluetRDLC.Test
{
    [TestClass]
    public sealed class GeneratorTests
    {
        /// <summary>
        /// Simple sales report example
        /// </summary>
        [TestMethod]
        public void CreateSimpleSalesReport()
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
        [TestMethod]
        public void CreateDashboard()
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
        [TestMethod]
        public void CreateEmployeeDirectory()
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
        [TestMethod]
        public void CreateInvoiceTemplate()
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
        [TestMethod]
        public void CreateProductCatalog()
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
}
