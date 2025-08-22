using FluentRDLC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluetRDLC.Test;

[TestClass]
public class ConverterDebugTest
{
    [TestMethod]
    public void TestBasicConversion()
    {
        var converter = new RdlcToFluentConverter();
        
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Body>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
  <LeftMargin>0.5in</LeftMargin>
  <RightMargin>0.5in</RightMargin>
  <TopMargin>0.5in</TopMargin>
  <BottomMargin>0.5in</BottomMargin>
</Report>";

        var result = converter.ConvertToFluentCode(rdlcXml, "TestReport");
        
        Console.WriteLine("Generated code:");
        Console.WriteLine(result);
        
        Assert.IsTrue(result.Contains("TestReport"));
    }
}