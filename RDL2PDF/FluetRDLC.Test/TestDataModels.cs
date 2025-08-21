using System;
using System.Collections.Generic;

namespace FluetRDLC.Test
{
    #region Sales and Financial Models

    public class SalesRecord
    {
        public string Month { get; set; } = "";
        public decimal Revenue { get; set; }
        public int Units { get; set; }
        public string Region { get; set; } = "";
        public DateTime SaleDate { get; set; }
        public string SalesRep { get; set; } = "";
    }

    public class ProductSales
    {
        public string Category { get; set; } = "";
        public decimal Sales { get; set; }
        public int Quantity { get; set; }
        public double GrowthRate { get; set; }
        public bool IsTopPerformer { get; set; }
    }

    public class MarketShareData
    {
        public string Company { get; set; } = "";
        public decimal MarketShare { get; set; }
        public string Sector { get; set; } = "";
        public int Rank { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TrendData
    {
        public string Period { get; set; } = "";
        public decimal Value { get; set; }
        public decimal PreviousValue { get; set; }
        public double ChangePercent { get; set; }
        public string Category { get; set; } = "";
    }

    #endregion

    #region Employee and HR Models

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentMetrics
    {
        public string Department { get; set; } = "";
        public int EmployeeCount { get; set; }
        public decimal AverageSalary { get; set; }
        public double SatisfactionRating { get; set; }
        public int OpenPositions { get; set; }
    }

    #endregion

    #region Inventory and Product Models

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public int StockLevel { get; set; }
        public bool IsDiscontinued { get; set; }
        public DateTime LastOrderDate { get; set; }
    }

    public class InventoryItem
    {
        public string ItemCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }
        public string Supplier { get; set; } = "";
        public DateTime LastRestocked { get; set; }
    }

    #endregion

    #region Dashboard and Analytics Models

    public class DashboardMetric
    {
        public string MetricName { get; set; } = "";
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public decimal PreviousValue { get; set; }
        public string Unit { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime MeasureDate { get; set; }
    }

    public class QuarterlyPerformance
    {
        public string Category { get; set; } = "";
        public decimal Q1 { get; set; }
        public decimal Q2 { get; set; }
        public decimal Q3 { get; set; }
        public decimal Q4 { get; set; }
        public decimal YearTotal { get; set; }
        public double YearOverYearGrowth { get; set; }
    }

    public class CustomerData
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Industry { get; set; } = "";
        public decimal LifetimeValue { get; set; }
        public int OrderCount { get; set; }
        public DateTime FirstOrderDate { get; set; }
        public DateTime LastOrderDate { get; set; }
        public bool IsVIP { get; set; }
    }

    #endregion

    /// <summary>
    /// Factory class to create test data instances
    /// </summary>
    public static class TestDataFactory
    {
        public static List<SalesRecord> CreateSalesData()
        {
            return new List<SalesRecord>
            {
                new SalesRecord { Month = "January", Revenue = 125000m, Units = 450, Region = "North", SaleDate = new DateTime(2024, 1, 31), SalesRep = "John Smith" },
                new SalesRecord { Month = "February", Revenue = 140000m, Units = 520, Region = "North", SaleDate = new DateTime(2024, 2, 29), SalesRep = "John Smith" },
                new SalesRecord { Month = "March", Revenue = 135000m, Units = 480, Region = "South", SaleDate = new DateTime(2024, 3, 31), SalesRep = "Jane Doe" },
                new SalesRecord { Month = "April", Revenue = 180000m, Units = 650, Region = "South", SaleDate = new DateTime(2024, 4, 30), SalesRep = "Jane Doe" },
                new SalesRecord { Month = "May", Revenue = 165000m, Units = 590, Region = "East", SaleDate = new DateTime(2024, 5, 31), SalesRep = "Mike Johnson" },
                new SalesRecord { Month = "June", Revenue = 190000m, Units = 720, Region = "West", SaleDate = new DateTime(2024, 6, 30), SalesRep = "Sarah Wilson" }
            };
        }

        public static List<ProductSales> CreateProductData()
        {
            return new List<ProductSales>
            {
                new ProductSales { Category = "Electronics", Sales = 450000m, Quantity = 1200, GrowthRate = 15.5, IsTopPerformer = true },
                new ProductSales { Category = "Clothing", Sales = 320000m, Quantity = 2500, GrowthRate = 8.2, IsTopPerformer = false },
                new ProductSales { Category = "Books", Sales = 180000m, Quantity = 3200, GrowthRate = -2.1, IsTopPerformer = false },
                new ProductSales { Category = "Home & Garden", Sales = 275000m, Quantity = 800, GrowthRate = 12.3, IsTopPerformer = true },
                new ProductSales { Category = "Sports", Sales = 220000m, Quantity = 950, GrowthRate = 5.7, IsTopPerformer = false }
            };
        }

        public static List<MarketShareData> CreateMarketShareData()
        {
            return new List<MarketShareData>
            {
                new MarketShareData { Company = "Company A", MarketShare = 35.5m, Sector = "Technology", Rank = 1, LastUpdated = DateTime.Now.AddDays(-1) },
                new MarketShareData { Company = "Company B", MarketShare = 28.2m, Sector = "Technology", Rank = 2, LastUpdated = DateTime.Now.AddDays(-1) },
                new MarketShareData { Company = "Company C", MarketShare = 18.7m, Sector = "Technology", Rank = 3, LastUpdated = DateTime.Now.AddDays(-1) },
                new MarketShareData { Company = "Company D", MarketShare = 12.1m, Sector = "Technology", Rank = 4, LastUpdated = DateTime.Now.AddDays(-1) },
                new MarketShareData { Company = "Others", MarketShare = 5.5m, Sector = "Technology", Rank = 5, LastUpdated = DateTime.Now.AddDays(-1) }
            };
        }

        public static List<TrendData> CreateTrendData()
        {
            return new List<TrendData>
            {
                new TrendData { Period = "Q1 2023", Value = 100m, PreviousValue = 95m, ChangePercent = 5.3, Category = "Performance" },
                new TrendData { Period = "Q2 2023", Value = 120m, PreviousValue = 100m, ChangePercent = 20.0, Category = "Performance" },
                new TrendData { Period = "Q3 2023", Value = 135m, PreviousValue = 120m, ChangePercent = 12.5, Category = "Performance" },
                new TrendData { Period = "Q4 2023", Value = 110m, PreviousValue = 135m, ChangePercent = -18.5, Category = "Performance" },
                new TrendData { Period = "Q1 2024", Value = 145m, PreviousValue = 110m, ChangePercent = 31.8, Category = "Performance" },
                new TrendData { Period = "Q2 2024", Value = 160m, PreviousValue = 145m, ChangePercent = 10.3, Category = "Performance" }
            };
        }

        public static List<Employee> CreateEmployeeData()
        {
            return new List<Employee>
            {
                new Employee { EmployeeId = 1001, FirstName = "John", LastName = "Smith", Department = "Engineering", Email = "john.smith@company.com", HireDate = new DateTime(2020, 3, 15), Salary = 95000m, IsActive = true },
                new Employee { EmployeeId = 1002, FirstName = "Jane", LastName = "Doe", Department = "Marketing", Email = "jane.doe@company.com", HireDate = new DateTime(2019, 7, 22), Salary = 78000m, IsActive = true },
                new Employee { EmployeeId = 1003, FirstName = "Mike", LastName = "Johnson", Department = "Sales", Email = "mike.johnson@company.com", HireDate = new DateTime(2021, 1, 8), Salary = 65000m, IsActive = true },
                new Employee { EmployeeId = 1004, FirstName = "Sarah", LastName = "Wilson", Department = "HR", Email = "sarah.wilson@company.com", HireDate = new DateTime(2018, 11, 12), Salary = 72000m, IsActive = true },
                new Employee { EmployeeId = 1005, FirstName = "David", LastName = "Brown", Department = "Finance", Email = "david.brown@company.com", HireDate = new DateTime(2017, 5, 3), Salary = 88000m, IsActive = false }
            };
        }

        public static List<QuarterlyPerformance> CreateQuarterlyData()
        {
            return new List<QuarterlyPerformance>
            {
                new QuarterlyPerformance { Category = "North", Q1 = 125000m, Q2 = 140000m, Q3 = 135000m, Q4 = 150000m, YearTotal = 550000m, YearOverYearGrowth = 12.5 },
                new QuarterlyPerformance { Category = "South", Q1 = 110000m, Q2 = 125000m, Q3 = 145000m, Q4 = 160000m, YearTotal = 540000m, YearOverYearGrowth = 8.7 },
                new QuarterlyPerformance { Category = "East", Q1 = 95000m, Q2 = 105000m, Q3 = 120000m, Q4 = 140000m, YearTotal = 460000m, YearOverYearGrowth = 15.2 },
                new QuarterlyPerformance { Category = "West", Q1 = 140000m, Q2 = 155000m, Q3 = 150000m, Q4 = 175000m, YearTotal = 620000m, YearOverYearGrowth = 6.3 }
            };
        }

        public static List<DashboardMetric> CreateMetricsData()
        {
            return new List<DashboardMetric>
            {
                new DashboardMetric { MetricName = "Performance", CurrentValue = 85.5m, TargetValue = 90m, PreviousValue = 82.1m, Unit = "%", Status = "Good", MeasureDate = DateTime.Now },
                new DashboardMetric { MetricName = "Quality", CurrentValue = 92.3m, TargetValue = 95m, PreviousValue = 91.8m, Unit = "%", Status = "Excellent", MeasureDate = DateTime.Now },
                new DashboardMetric { MetricName = "Efficiency", CurrentValue = 78.9m, TargetValue = 85m, PreviousValue = 76.4m, Unit = "%", Status = "Needs Improvement", MeasureDate = DateTime.Now },
                new DashboardMetric { MetricName = "Satisfaction", CurrentValue = 91.2m, TargetValue = 88m, PreviousValue = 89.7m, Unit = "%", Status = "Excellent", MeasureDate = DateTime.Now },
                new DashboardMetric { MetricName = "Innovation", CurrentValue = 76.4m, TargetValue = 80m, PreviousValue = 74.8m, Unit = "%", Status = "Good", MeasureDate = DateTime.Now }
            };
        }

        public static List<InvoiceItem> CreateInvoiceData()
        {
            return new List<InvoiceItem>
            {
                new InvoiceItem { Description = "Professional Consulting Services", Quantity = 40, UnitPrice = 150.00m, Tax = 0.08m },
                new InvoiceItem { Description = "Software License (Annual)", Quantity = 1, UnitPrice = 2400.00m, Tax = 0.08m },
                new InvoiceItem { Description = "Training Sessions", Quantity = 8, UnitPrice = 300.00m, Tax = 0.08m },
                new InvoiceItem { Description = "Technical Support", Quantity = 12, UnitPrice = 120.00m, Tax = 0.08m }
            };
        }
    }

    #region Invoice Models

    public class InvoiceItem
    {
        public string Description { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Tax { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
        public decimal TaxAmount => LineTotal * Tax;
        public decimal TotalWithTax => LineTotal + TaxAmount;
    }

    public class Company
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Website { get; set; } = "";
        public string TaxId { get; set; } = "";
    }

    #endregion
}