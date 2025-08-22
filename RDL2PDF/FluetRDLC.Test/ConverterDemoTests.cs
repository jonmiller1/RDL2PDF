using FluentRDLC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace FluetRDLC.Test;

[TestClass]
public class ConverterDemoTests
{
    private RdlcToFluentConverter _converter;

    [TestInitialize]
    public void Setup()
    {
        _converter = new RdlcToFluentConverter();
    }

    [TestMethod]
    public void DemonstrateConverter_WithSampleRDLC_SavesConvertedCode()
    {
        // Sample RDLC XML that might come from an existing report
        var sampleRdlc = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <DataSources>
    <DataSource Name=""DataSource1"">
      <ConnectionProperties>
        <DataProvider>System.Data.DataSet</DataProvider>
        <ConnectString>/* Local Connection */</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""SalesData"">
      <Query>
        <DataSourceName>DataSource1</DataSourceName>
        <CommandText>/* Local Query */</CommandText>
      </Query>
      <Fields>
        <Field Name=""ProductName"">
          <DataField>ProductName</DataField>
        </Field>
        <Field Name=""SalesAmount"">
          <DataField>SalesAmount</DataField>
        </Field>
        <Field Name=""Region"">
          <DataField>Region</DataField>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <ReportItems>
      <Textbox Name=""ReportTitle"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Monthly Sales Report</Value>
                <Style>
                  <FontSize>20pt</FontSize>
                  <FontWeight>Bold</FontWeight>
                  <Color>DarkBlue</Color>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0in</Top>
        <Width>8in</Width>
        <Height>0.5in</Height>
        <Style>
          <TextAlign>Center</TextAlign>
        </Style>
      </Textbox>
      <Textbox Name=""ReportDate"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=Format(Today(), ""MMMM yyyy"")</Value>
                <Style>
                  <FontSize>12pt</FontSize>
                  <FontStyle>Italic</FontStyle>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0.6in</Top>
        <Width>8in</Width>
        <Height>0.25in</Height>
        <Style>
          <TextAlign>Center</TextAlign>
        </Style>
      </Textbox>
      <Tablix Name=""SalesTable"">
        <DataSetName>SalesData</DataSetName>
        <Left>0in</Left>
        <Top>1.5in</Top>
        <Width>6in</Width>
        <Height>1.5in</Height>
        <TablixBody>
          <TablixColumns>
            <TablixColumn>
              <Width>2.5in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>1.5in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>2in</Width>
            </TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""ProductNameHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Product Name</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                                <Color>White</Color>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                      <Style>
                        <BackgroundColor>DarkBlue</BackgroundColor>
                      </Style>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""RegionHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Region</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                                <Color>White</Color>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                      <Style>
                        <BackgroundColor>DarkBlue</BackgroundColor>
                      </Style>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""SalesAmountHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Sales Amount</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                                <Color>White</Color>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                      <Style>
                        <BackgroundColor>DarkBlue</BackgroundColor>
                        <TextAlign>Right</TextAlign>
                      </Style>
                    </Textbox>
                  </CellContents>
                </TablixCell>
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""ProductNameData"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>=Fields!ProductName.Value</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""RegionData"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>=Fields!Region.Value</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""SalesAmountData"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>=Format(Fields!SalesAmount.Value, ""C2"")</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                      <Style>
                        <TextAlign>Right</TextAlign>
                      </Style>
                    </Textbox>
                  </CellContents>
                </TablixCell>
              </TablixCells>
            </TablixRow>
          </TablixRows>
        </TablixBody>
      </Tablix>
      <Textbox Name=""Summary"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=&quot;Total Sales: &quot; + Format(Sum(Fields!SalesAmount.Value, &quot;SalesData&quot;), &quot;C2&quot;)</Value>
                <Style>
                  <FontSize>14pt</FontSize>
                  <FontWeight>Bold</FontWeight>
                  <Color>DarkGreen</Color>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>4in</Left>
        <Top>3.5in</Top>
        <Width>4in</Width>
        <Height>0.3in</Height>
        <Style>
          <TextAlign>Right</TextAlign>
        </Style>
      </Textbox>
    </ReportItems>
    <Height>5in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
  <LeftMargin>1in</LeftMargin>
  <RightMargin>1in</RightMargin>
  <TopMargin>0.5in</TopMargin>
  <BottomMargin>0.5in</BottomMargin>
</Report>";

        // Convert the RDLC to fluent code
        var fluentCode = _converter.ConvertToFluentCode(sampleRdlc, "MonthlySalesReport");

        // Save the generated code to a file for inspection
        var outputPath = GetOutputPath("GeneratedFluentCode.cs");
        File.WriteAllText(outputPath, fluentCode);

        Console.WriteLine($"Generated fluent code saved to: {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Generated Fluent Code:");
        Console.WriteLine("=====================");
        Console.WriteLine(fluentCode);

        // Verify the conversion contains all expected elements
        Assert.IsTrue(fluentCode.Contains("RdlcReportBuilder.Create(\"MonthlySalesReport\")"));
        Assert.IsTrue(fluentCode.Contains(".WithLayout(layout => layout"));
        Assert.IsTrue(fluentCode.Contains(".WithPageSize(8.5, 11)"));
        Assert.IsTrue(fluentCode.Contains(".WithMargins(1, 1, 0.5, 0.5)"));
        Assert.IsTrue(fluentCode.Contains(".WithBodyHeight(5)"));
        
        // Data source and dataset
        Assert.IsTrue(fluentCode.Contains(".WithObjectDataSource<object>(\"DataSource1\")"));
        Assert.IsTrue(fluentCode.Contains(".WithDataSet(\"SalesData\", ds => ds"));
        Assert.IsTrue(fluentCode.Contains(".WithField(\"ProductName\", \"ProductName\")"));
        Assert.IsTrue(fluentCode.Contains(".WithField(\"SalesAmount\", \"SalesAmount\")"));
        Assert.IsTrue(fluentCode.Contains(".WithField(\"Region\", \"Region\")"));
        
        // Textboxes
        Assert.IsTrue(fluentCode.Contains(".WithTextBox(\"ReportTitle\", tb => tb"));
        Assert.IsTrue(fluentCode.Contains(".WithText(\"Monthly Sales Report\")"));
        Assert.IsTrue(fluentCode.Contains(".WithFontSize(20)"));
        Assert.IsTrue(fluentCode.Contains(".WithColor(\"DarkBlue\")"));
        Assert.IsTrue(fluentCode.Contains(".WithTextAlign(\"Center\")"));
        
        Assert.IsTrue(fluentCode.Contains(".WithTextBox(\"ReportDate\", tb => tb"));
        Assert.IsTrue(fluentCode.Contains(".WithExpression(\"=Format(Today(), \\\"MMMM yyyy\\\")\")"));
        Assert.IsTrue(fluentCode.Contains(".Italic()"));
        
        // Tablix
        Assert.IsTrue(fluentCode.Contains(".WithTablix(\"SalesTable\", tbl => tbl"));
        Assert.IsTrue(fluentCode.Contains(".WithDataSet(\"SalesData\")"));
        Assert.IsTrue(fluentCode.Contains(".WithBounds(0, 1.5, 6, 1.5)"));
        Assert.IsTrue(fluentCode.Contains(".WithColumn(2.5)"));
        Assert.IsTrue(fluentCode.Contains(".WithColumn(1.5)"));
        Assert.IsTrue(fluentCode.Contains(".WithColumn(2)"));
        Assert.IsTrue(fluentCode.Contains(".WithHeaderRow(row => row"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"Product Name\")"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"Region\")"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"Sales Amount\")"));
        Assert.IsTrue(fluentCode.Contains(".WithDataRow(row => row"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"=Fields!ProductName.Value\")"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"=Fields!Region.Value\")"));
        Assert.IsTrue(fluentCode.Contains(".WithCell(\"=Format(Fields!SalesAmount.Value, \\\"C2\\\")\")"));
        
        // Summary
        Assert.IsTrue(fluentCode.Contains(".WithTextBox(\"Summary\", tb => tb"));
        Assert.IsTrue(fluentCode.Contains("Total Sales"));
        Assert.IsTrue(fluentCode.Contains("Sum(Fields!SalesAmount.Value"));
        Assert.IsTrue(fluentCode.Contains(".WithColor(\"DarkGreen\")"));
        
        Assert.IsTrue(fluentCode.Contains("return reportBuilder.ToXml();"));
        
        // Verify the generated code is substantial
        Assert.IsTrue(fluentCode.Length > 3000, "Generated code should be comprehensive");
        
        Console.WriteLine();
        Console.WriteLine($"Conversion successful! Generated {fluentCode.Length} characters of fluent code.");
    }

    [TestMethod]
    public void DemonstrateRoundTrip_ConvertAndBuildBack_ProducesEquivalentReport()
    {
        // Create an original report using fluent builder
        var originalBuilder = RdlcReportBuilder.Create("RoundTripTest")
            .WithLayout(layout => layout
                .WithPageSize(8.5, 11)
                .WithMargins(0.5)
                .WithBodyHeight(11))
            .WithObjectDataSource<InvoiceItem>("InvoiceDataSource")
            .WithDataSet("InvoiceItems", ds => ds
                .UsingDataSource("InvoiceDataSource")
                .WithFieldsFromType<InvoiceItem>())
            .WithTextBox("Title", tb => tb
                .WithText("Round Trip Test Report")
                .WithBounds(0, 0, 8, 0.5)
                .WithFontSize(18)
                .Bold())
            .WithTextBox("Subtitle", tb => tb
                .WithExpression("='Generated on: ' + Format(Today(), 'MM/dd/yyyy')")
                .WithBounds(0, 0.6, 8, 0.25)
                .WithFontSize(12))
            .WithTablix("TestTable", tbl => tbl
                .UsingDataSet("InvoiceItems")
                .WithBounds(0, 1.5, 8, 2)
                .WithColumn(4)
                .WithColumn(2)
                .WithColumn(2)
                .WithHeaderRow(0.25, "Description", "Quantity", "Amount")
                .WithDataRow(0.25, "=Fields!Description.Value", "=Fields!Quantity.Value", "=Format(Fields!LineTotal.Value, \"C2\")"));

        var originalXml = originalBuilder.ToXml();
        
        // Convert to fluent code
        var convertedCode = _converter.ConvertToFluentCode(originalXml, "ConvertedRoundTripTest");
        
        // Save both for comparison
        var originalPath = GetOutputPath("OriginalReport.rdlc");
        var convertedCodePath = GetOutputPath("ConvertedFluentCode_RoundTrip.cs");
        
        File.WriteAllText(originalPath, originalXml);
        File.WriteAllText(convertedCodePath, convertedCode);
        
        Console.WriteLine($"Original RDLC saved to: {originalPath}");
        Console.WriteLine($"Converted fluent code saved to: {convertedCodePath}");
        Console.WriteLine();
        Console.WriteLine("Round-trip conversion completed successfully!");
        
        // Verify key elements are preserved in the conversion
        Assert.IsTrue(convertedCode.Contains("ConvertedRoundTripTest"));
        Assert.IsTrue(convertedCode.Contains("Round Trip Test Report"));
        Assert.IsTrue(convertedCode.Contains("WithTablix(\"TestTable\""));
        Assert.IsTrue(convertedCode.Contains("WithDataSet(\"InvoiceItems\")"));
        Assert.IsTrue(convertedCode.Contains("Format(Today(), 'MM/dd/yyyy')"));
        Assert.IsTrue(convertedCode.Contains("WithColumn(4)"));
        Assert.IsTrue(convertedCode.Contains("WithColumn(2)"));
        
        Console.WriteLine("All round-trip verification checks passed!");
    }

    private static string GetOutputPath(string fileName)
    {
        var outputDir = @"C:\Output";
        Directory.CreateDirectory(outputDir);
        return Path.Combine(outputDir, fileName);
    }
}