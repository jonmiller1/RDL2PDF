using FluentRDLC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace FluetRDLC.Test;

[TestClass]
public class RdlcToFluentConverterTests
{
    private RdlcToFluentConverter _converter;

    [TestInitialize]
    public void Setup()
    {
        _converter = new RdlcToFluentConverter();
    }

    [TestMethod]
    public void ConvertToFluentCode_EmptyReport_GeneratesBasicStructure()
    {
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

        var result = _converter.ConvertToFluentCode(rdlcXml, "TestReport");

        Assert.IsTrue(result.Contains("RdlcReportBuilder.Create(\"TestReport\")"));
        Assert.IsTrue(result.Contains(".WithLayout(layout => layout"));
        Assert.IsTrue(result.Contains(".WithPageSize(8.5, 11)"));
        Assert.IsTrue(result.Contains(".WithMargins(0.5, 0.5, 0.5, 0.5)"));
        Assert.IsTrue(result.Contains(".WithBodyHeight(11)"));
        Assert.IsTrue(result.Contains("return reportBuilder.ToXml();"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithTextbox_GeneratesTextboxCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""TitleTextBox"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Invoice Report</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>1in</Left>
        <Top>0.5in</Top>
        <Width>6in</Width>
        <Height>0.5in</Height>
        <Style>
          <FontSize>16pt</FontSize>
          <FontWeight>Bold</FontWeight>
          <Color>DarkBlue</Color>
        </Style>
      </Textbox>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithTextBox(\"TitleTextBox\", tb => tb"));
        Assert.IsTrue(result.Contains(".WithText(\"Invoice Report\")"));
        Assert.IsTrue(result.Contains(".WithBounds(1, 0.5, 6, 0.5)"));
        Assert.IsTrue(result.Contains(".WithFontSize(16)"));
        Assert.IsTrue(result.Contains(".Bold()"));
        Assert.IsTrue(result.Contains(".WithColor(\"DarkBlue\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithExpression_GeneratesExpressionCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""ExpressionTextBox"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=Parameters!CompanyName.Value + "" - "" + Format(Today(), ""MM/dd/yyyy"")</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0in</Top>
        <Width>4in</Width>
        <Height>0.25in</Height>
      </Textbox>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithTextBox(\"ExpressionTextBox\", tb => tb"));
        Assert.IsTrue(result.Contains(".WithExpression(\"=Parameters!CompanyName.Value + \\\" - \\\" + Format(Today(), \\\"MM/dd/yyyy\\\")\")"));
        Assert.IsTrue(result.Contains(".WithBounds(0, 0, 4, 0.25)"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithImage_GeneratesImageCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Image Name=""CompanyLogo"">
        <Source>Embedded</Source>
        <Value>LogoImage</Value>
        <Left>0.2in</Left>
        <Top>0.1in</Top>
        <Width>2in</Width>
        <Height>1in</Height>
      </Image>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <EmbeddedImages>
    <EmbeddedImage Name=""LogoImage"" MIMEType=""image/png"">
      <ImageData>iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==</ImageData>
    </EmbeddedImage>
  </EmbeddedImages>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithEmbeddedImageFromBytes(\"LogoImage\", Convert.FromBase64String(\"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==\"), \"image/png\")"));
        Assert.IsTrue(result.Contains(".WithImage(\"CompanyLogo\", img => img"));
        Assert.IsTrue(result.Contains(".WithEmbeddedImageSource(\"LogoImage\")"));
        Assert.IsTrue(result.Contains(".WithBounds(0.2, 0.1, 2, 1)"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithExternalImage_GeneratesExternalImageCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Image Name=""ExternalImage"">
        <Source>External</Source>
        <Value>https://example.com/image.png</Value>
        <Left>1in</Left>
        <Top>1in</Top>
        <Width>3in</Width>
        <Height>2in</Height>
      </Image>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithImage(\"ExternalImage\", img => img"));
        Assert.IsTrue(result.Contains(".WithImageSource(\"https://example.com/image.png\")"));
        Assert.IsTrue(result.Contains(".WithBounds(1, 1, 3, 2)"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithDataSource_GeneratesDataSourceCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <DataSources>
    <DataSource Name=""ObjectDataSource"">
      <ConnectionProperties>
        <DataProvider>System.Data.DataSet</DataProvider>
        <ConnectString>/* Local Connection */</ConnectString>
      </ConnectionProperties>
    </DataSource>
    <DataSource Name=""SqlDataSource"">
      <ConnectionProperties>
        <DataProvider>SQL</DataProvider>
        <ConnectString>Server=localhost;Database=TestDB</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <Body>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithObjectDataSource<object>(\"ObjectDataSource\")"));
        Assert.IsTrue(result.Contains(".WithDataSource(\"SqlDataSource\", \"Server=localhost;Database=TestDB\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithDataSet_GeneratesDataSetCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <DataSources>
    <DataSource Name=""TestDataSource"">
      <ConnectionProperties>
        <DataProvider>System.Data.DataSet</DataProvider>
        <ConnectString>/* Local Connection */</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""InvoiceItems"">
      <Query>
        <DataSourceName>TestDataSource</DataSourceName>
        <CommandText>SELECT * FROM InvoiceItems</CommandText>
      </Query>
      <Fields>
        <Field Name=""Description"">
          <DataField>Description</DataField>
        </Field>
        <Field Name=""Amount"">
          <DataField>Amount</DataField>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithDataSet(\"InvoiceItems\", ds => ds"));
        Assert.IsTrue(result.Contains(".UsingDataSource(\"TestDataSource\")"));
        Assert.IsTrue(result.Contains(".WithQuery(\"SELECT * FROM InvoiceItems\")"));
        Assert.IsTrue(result.Contains(".WithField(\"Description\", \"Description\")"));
        Assert.IsTrue(result.Contains(".WithField(\"Amount\", \"Amount\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithTablix_GeneratesTablixCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Tablix Name=""InvoiceTable"">
        <DataSetName>InvoiceItems</DataSetName>
        <Left>0in</Left>
        <Top>2in</Top>
        <Width>8in</Width>
        <Height>2in</Height>
        <TablixBody>
          <TablixColumns>
            <TablixColumn>
              <Width>2in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>1in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>1.5in</Width>
            </TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""HeaderDescription"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Description</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""HeaderQuantity"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Quantity</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""HeaderAmount"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Amount</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
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
                    <Textbox Name=""DataDescription"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>=Fields!Description.Value</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""DataQuantity"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>=Fields!Quantity.Value</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""DataAmount"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>=Format(Fields!Amount.Value, ""C2"")</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
              </TablixCells>
            </TablixRow>
          </TablixRows>
        </TablixBody>
      </Tablix>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithTablix(\"InvoiceTable\", tbl => tbl"));
        Assert.IsTrue(result.Contains(".WithDataSet(\"InvoiceItems\")"));
        Assert.IsTrue(result.Contains(".WithBounds(0, 2, 8, 2)"));
        Assert.IsTrue(result.Contains(".WithColumn(2)"));
        Assert.IsTrue(result.Contains(".WithColumn(1)"));
        Assert.IsTrue(result.Contains(".WithColumn(1.5)"));
        Assert.IsTrue(result.Contains(".WithHeaderRow(row => row"));
        Assert.IsTrue(result.Contains(".WithCell(\"Description\")"));
        Assert.IsTrue(result.Contains(".WithCell(\"Quantity\")"));
        Assert.IsTrue(result.Contains(".WithCell(\"Amount\")"));
        Assert.IsTrue(result.Contains(".WithDataRow(row => row"));
        Assert.IsTrue(result.Contains(".WithCell(\"=Fields!Description.Value\")"));
        Assert.IsTrue(result.Contains(".WithCell(\"=Fields!Quantity.Value\")"));
        Assert.IsTrue(result.Contains(".WithCell(\"=Format(Fields!Amount.Value, \\\"C2\\\")\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithComplexStyles_GeneratesStyleCode()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""StyledTextBox"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Styled Text</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0in</Top>
        <Width>3in</Width>
        <Height>0.5in</Height>
        <Style>
          <FontSize>14pt</FontSize>
          <FontWeight>Bold</FontWeight>
          <FontStyle>Italic</FontStyle>
          <Color>Red</Color>
          <BackgroundColor>Yellow</BackgroundColor>
          <TextAlign>Center</TextAlign>
        </Style>
      </Textbox>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithFontSize(14)"));
        Assert.IsTrue(result.Contains(".Bold()"));
        Assert.IsTrue(result.Contains(".Italic()"));
        Assert.IsTrue(result.Contains(".WithColor(\"Red\")"));
        Assert.IsTrue(result.Contains(".WithBackgroundColor(\"Yellow\")"));
        Assert.IsTrue(result.Contains(".WithTextAlign(\"Center\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithCustomMargins_GeneratesCorrectMargins()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <Height>10in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
  <LeftMargin>1in</LeftMargin>
  <RightMargin>0.75in</RightMargin>
  <TopMargin>0.25in</TopMargin>
  <BottomMargin>1.5in</BottomMargin>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithMargins(1, 0.75, 0.25, 1.5)"));
        Assert.IsTrue(result.Contains(".WithBodyHeight(10)"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithMultipleReportItems_GeneratesAllItems()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""Title"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Report Title</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0in</Top>
        <Width>8in</Width>
        <Height>0.5in</Height>
      </Textbox>
      <Image Name=""Logo"">
        <Source>Embedded</Source>
        <Value>CompanyLogo</Value>
        <Left>7in</Left>
        <Top>0in</Top>
        <Width>1in</Width>
        <Height>0.5in</Height>
      </Image>
      <Textbox Name=""Subtitle"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=Parameters!ReportDate.Value</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0.6in</Top>
        <Width>4in</Width>
        <Height>0.25in</Height>
      </Textbox>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithTextBox(\"Title\", tb => tb"));
        Assert.IsTrue(result.Contains(".WithText(\"Report Title\")"));
        Assert.IsTrue(result.Contains(".WithImage(\"Logo\", img => img"));
        Assert.IsTrue(result.Contains(".WithEmbeddedImageSource(\"CompanyLogo\")"));
        Assert.IsTrue(result.Contains(".WithTextBox(\"Subtitle\", tb => tb"));
        Assert.IsTrue(result.Contains(".WithExpression(\"=Parameters!ReportDate.Value\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithSpecialCharacters_EscapesCorrectly()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""QuotedText"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Text with ""quotes"" and 'apostrophes'</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>0in</Left>
        <Top>0in</Top>
        <Width>4in</Width>
        <Height>0.25in</Height>
      </Textbox>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithText(\"Text with \\\"quotes\\\" and 'apostrophes'\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_EmptyReportName_UsesDefault()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains("RdlcReportBuilder.Create(\"ConvertedReport\")"));
    }

    [TestMethod]
    public void ConvertToFluentCode_WithDifferentSizeUnits_ParsesCorrectly()
    {
        var rdlcXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""SizedTextBox"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Test</Value>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
        <Left>2.54cm</Left>
        <Top>12pt</Top>
        <Width>5.08cm</Width>
        <Height>24pt</Height>
      </Textbox>
    </ReportItems>
    <Height>11in</Height>
  </Body>
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
</Report>";

        var result = _converter.ConvertToFluentCode(rdlcXml);

        Assert.IsTrue(result.Contains(".WithBounds(2.54, 12, 5.08, 24)"));
    }
}