using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Globalization;

namespace FluetRDLC.Test;

[TestClass]
public class RdlcExpressionEvaluatorTests
{
    private RdlcExpressionEvaluator _evaluator;
    private DataTable _testDataTable;

    [TestInitialize]
    public void Setup()
    {
        _evaluator = new RdlcExpressionEvaluator();
        
        // Create test data table
        _testDataTable = new DataTable("TestData");
        _testDataTable.Columns.Add("Name", typeof(string));
        _testDataTable.Columns.Add("Amount", typeof(decimal));
        _testDataTable.Columns.Add("Quantity", typeof(int));
        _testDataTable.Columns.Add("Date", typeof(DateTime));
        
        _testDataTable.Rows.Add("Item1", 100.50m, 5, new DateTime(2024, 1, 15));
        _testDataTable.Rows.Add("Item2", 250.75m, 10, new DateTime(2024, 2, 20));
        _testDataTable.Rows.Add("Item3", 75.25m, 3, new DateTime(2024, 3, 10));
        
        // Setup evaluator with test data
        _evaluator.SetDataSources(new Dictionary<string, DataTable> { { "TestData", _testDataTable } });
        _evaluator.SetParameters(new Dictionary<string, object?>
        {
            { "CompanyName", "Test Company" },
            { "ReportDate", new DateTime(2024, 1, 1) },
            { "TaxRate", 0.08m }
        });
    }

    [TestMethod]
    public void TestParameterAccess()
    {
        // Test Parameters!ParameterName.Value syntax
        var result = _evaluator.EvaluateExpression("=Parameters!CompanyName.Value");
        Assert.AreEqual("Test Company", result);

        var result2 = _evaluator.EvaluateExpression("=Parameters!TaxRate.Value");
        Assert.AreEqual("0.08", result2);
    }

    [TestMethod]
    public void TestFieldAccess()
    {
        // Set current row for field testing
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        var result = _evaluator.EvaluateExpression("=Fields!Name.Value");
        Assert.AreEqual("Item1", result);

        var result2 = _evaluator.EvaluateExpression("=Fields!Amount.Value");
        Assert.AreEqual("100.5", result2);
    }

    [TestMethod]
    public void TestFormatFunction_Currency()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        var result = _evaluator.EvaluateExpression("=Format(Fields!Amount.Value, 'C2')");
        
        // Should format as currency with 2 decimal places (generic currency symbol)
        Assert.AreEqual("¤100.50", result);
    }

    [TestMethod]
    public void TestFormatFunction_Numeric()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[1], "TestData");
        
        var result = _evaluator.EvaluateExpression("=Format(Fields!Amount.Value, 'N2')");
        Assert.AreEqual("250.75", result);
    }

    [TestMethod]
    public void TestFormatFunction_DateTime()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        var result = _evaluator.EvaluateExpression("=Format(Fields!Date.Value, 'MM/dd/yyyy')");
        Assert.AreEqual("01/15/2024", result);
    }

    [TestMethod]
    public void TestSumFunction()
    {
        var result = _evaluator.EvaluateExpression("=Sum(Fields!Amount.Value, 'TestData')");
        
        // Sum should be 100.50 + 250.75 + 75.25 = 426.50
        Assert.AreEqual("426.5", result);
    }

    [TestMethod]
    public void TestSumFunction_IntegerField()
    {
        var result = _evaluator.EvaluateExpression("=Sum(Fields!Quantity.Value, 'TestData')");
        
        // Sum should be 5 + 10 + 3 = 18
        Assert.AreEqual("18", result);
    }

    [TestMethod]
    public void TestStringConcatenation_VBStyle()
    {
        var result = _evaluator.EvaluateExpression("='Company: ' + Parameters!CompanyName.Value");
        Assert.AreEqual("Company: Test Company", result);
    }

    [TestMethod]
    public void TestStringConcatenation_WithLineBreaks()
    {
        var result = _evaluator.EvaluateExpression("=Parameters!CompanyName.Value + vbCrLf + 'Report Date: ' + Parameters!ReportDate.Value");
        
        // Should contain line break
        Assert.IsTrue(result.Contains("Test Company\nReport Date:"));
    }

    [TestMethod]
    public void TestComplexExpression_InvoiceTotal()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        // Test complex expression like in invoice
        var result = _evaluator.EvaluateExpression("='Subtotal: $' + Format(Sum(Fields!Amount.Value, 'TestData'), 'N2') + vbCrLf + 'TOTAL: $' + Format(Sum(Fields!Amount.Value, 'TestData'), 'N2')");
        
        Assert.IsTrue(result.Contains("Subtotal: $426.50"));
        Assert.IsTrue(result.Contains("TOTAL: $426.50"));
        Assert.IsTrue(result.Contains("\n"));
    }

    [TestMethod]
    public void TestMathematicalExpressions()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        // Test basic math
        var result = _evaluator.EvaluateExpression("=Fields!Amount.Value * Fields!Quantity.Value");
        Assert.AreEqual("502.5", result); // 100.5 * 5 = 502.5
    }

    [TestMethod]
    public void TestConditionalExpressions()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        // Test conditional logic (though RDLC typically uses IIf function)
        var result = _evaluator.EvaluateExpression("=if(Fields!Amount.Value > 100, 'High', 'Low')");
        Assert.AreEqual("High", result);
    }

    [TestMethod]
    public void TestParameterInCalculation()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        // Test using parameter in calculation
        var result = _evaluator.EvaluateExpression("=Fields!Amount.Value * Parameters!TaxRate.Value");
        Assert.AreEqual("8.04", result); // 100.50 * 0.08 = 8.04
    }

    [TestMethod]
    public void TestInvalidExpression_ReturnsError()
    {
        var result = _evaluator.EvaluateExpression("=SomeInvalidFunction()");
        Assert.IsTrue(result.StartsWith("[Error:"));
    }

    [TestMethod]
    public void TestEmptyExpression_ReturnsEmpty()
    {
        var result = _evaluator.EvaluateExpression("");
        Assert.AreEqual(string.Empty, result);
        
        var result2 = _evaluator.EvaluateExpression(null);
        Assert.AreEqual(string.Empty, result2);
    }

    [TestMethod]
    public void TestNonExpression_ReturnsAsIs()
    {
        // Test literal text (no = prefix)
        var result = _evaluator.EvaluateExpression("Simple Text");
        Assert.IsTrue(result.Contains("Simple Text"));
    }

    [TestMethod]
    public void TestFormatWithDollarSign()
    {
        _evaluator.SetCurrentRow(_testDataTable.Rows[0], "TestData");
        
        // Test prefixing with dollar sign
        var result = _evaluator.EvaluateExpression("='$' + Format(Fields!Amount.Value, 'N2')");
        Assert.AreEqual("$100.50", result);
    }

    [TestMethod]
    public void TestMultipleDataSources()
    {
        // Create second data table
        var dataTable2 = new DataTable("OtherData");
        dataTable2.Columns.Add("Value", typeof(decimal));
        dataTable2.Rows.Add(50.0m);
        dataTable2.Rows.Add(75.0m);
        
        _evaluator.SetDataSources(new Dictionary<string, DataTable> 
        { 
            { "TestData", _testDataTable },
            { "OtherData", dataTable2 }
        });
        
        var result1 = _evaluator.EvaluateExpression("=Sum(Fields!Amount.Value, 'TestData')");
        Assert.AreEqual("426.5", result1);
        
        var result2 = _evaluator.EvaluateExpression("=Sum(Fields!Value.Value, 'OtherData')");
        Assert.AreEqual("125", result2);
    }

    [TestMethod]
    public void TestDateFormatting()
    {
        _evaluator.SetParameters(new Dictionary<string, object?>
        {
            { "CurrentDate", new DateTime(2024, 12, 25) }
        });
        
        var result = _evaluator.EvaluateExpression("=Format(Parameters!CurrentDate.Value, 'MM/dd/yyyy')");
        Assert.AreEqual("12/25/2024", result);
    }
}