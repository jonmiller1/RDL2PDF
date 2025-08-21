using FluentRDLC.Renderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Data;

namespace FluetRDLC.Test
{
    [TestClass]
    public class InvoiceTests
    {
        [TestMethod]
        public void CreateInvoiceWithCompanyLogo()
        {
            // Create invoice test data
            var renderer = new RDLCRenderer();
            var invoiceData = TestDataFactory.CreateInvoiceData();
            renderer.AddDataSource("InvoiceItems", CreateInvoiceDataTable(invoiceData));
            
            // Add invoice parameters
            renderer.AddParameter("InvoiceNumber", "INV-2024-001");
            renderer.AddParameter("InvoiceDate", DateTime.Now.ToString("MM/dd/yyyy"));
            renderer.AddParameter("DueDate", DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"));
            renderer.AddParameter("CompanyName", "TechSolutions LLC");
            renderer.AddParameter("CompanyAddress", "123 Business Ave");
            renderer.AddParameter("CompanyCityState", "Seattle, WA 98101");
            renderer.AddParameter("CompanyPhone", "(555) 123-4567");
            renderer.AddParameter("CompanyEmail", "info@techsolutions.com");
            renderer.AddParameter("CustomerName", "ABC Corporation");
            renderer.AddParameter("CustomerAddress", "456 Client Street");
            renderer.AddParameter("CustomerCityState", "Portland, OR 97201");
            
            // Create RDLC content for invoice with company logo
            var rdlcContent = CreateInvoiceRdlcContent();
            
            Console.WriteLine("Generating Invoice PDF with Company Logo...");
            var pdfBytes = renderer.RenderToPdfFromContent(rdlcContent);
            
            var outputPath = GetOutputPath("Invoice_CompanyLogo.pdf");
            File.WriteAllBytes(outputPath, pdfBytes);
            Console.WriteLine($"Invoice saved to: {outputPath}");
            Console.WriteLine($"File size: {pdfBytes.Length} bytes");
            
            // Verify the PDF was generated with substantial content
            Assert.IsTrue(pdfBytes.Length > 5000, "Invoice PDF should be generated with substantial content including logo and data");
            
            // Test multi-format generation
            Console.WriteLine("Generating Invoice in multiple formats...");
            
            // HTML version
            var htmlBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.HTML);
            File.WriteAllBytes(GetOutputPath("Invoice_CompanyLogo.html"), htmlBytes);
            
            // CSV version (for line items)
            var csvBytes = renderer.RenderFromContent(rdlcContent, RenderFormat.Excel);
            File.WriteAllBytes(GetOutputPath("Invoice_CompanyLogo.csv"), csvBytes);
            
            Console.WriteLine("Invoice generated in PDF, HTML, and CSV formats");
        }
        
        private DataTable CreateInvoiceDataTable(System.Collections.Generic.List<InvoiceItem> invoiceItems)
        {
            var table = new DataTable();
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("LineTotal", typeof(decimal));
            table.Columns.Add("TaxAmount", typeof(decimal));
            table.Columns.Add("TotalWithTax", typeof(decimal));
            
            foreach (var item in invoiceItems)
            {
                table.Rows.Add(
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    item.TaxAmount,
                    item.TotalWithTax
                );
            }
            
            return table;
        }
        
        private string CreateInvoiceRdlcContent()
        {
            return @"<?xml version='1.0' encoding='utf-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <Body>
    <Height>11in</Height>
    <ReportItems>
      <!-- Company Logo and Header -->
      <Rectangle Name='HeaderSection'>
        <Top>0in</Top>
        <Left>0in</Left>
        <Height>2in</Height>
        <Width>8in</Width>
        <ReportItems>
          <Image Name='CompanyLogo'>
            <Source>Embedded</Source>
            <Value>CompanyLogo</Value>
            <Top>0.1in</Top>
            <Left>0.2in</Left>
            <Height>1.5in</Height>
            <Width>2in</Width>
          </Image>
          <Textbox Name='CompanyInfo'>
            <Top>0.2in</Top>
            <Left>3in</Left>
            <Height>1.5in</Height>
            <Width>4.5in</Width>
            <Paragraphs>
              <Paragraph>
                <TextRuns>
                  <TextRun>
                    <Value>=Parameters!CompanyName.Value + vbCrLf + 
                           Parameters!CompanyAddress.Value + vbCrLf + 
                           Parameters!CompanyCityState.Value + vbCrLf + 
                           'Phone: ' + Parameters!CompanyPhone.Value + vbCrLf + 
                           'Email: ' + Parameters!CompanyEmail.Value</Value>
                    <Style>
                      <FontSize>11pt</FontSize>
                      <FontWeight>Bold</FontWeight>
                    </Style>
                  </TextRun>
                </TextRuns>
              </Paragraph>
            </Paragraphs>
          </Textbox>
        </ReportItems>
      </Rectangle>
      
      <!-- Invoice Title and Details -->
      <Textbox Name='InvoiceTitle'>
        <Top>2.2in</Top>
        <Left>0in</Left>
        <Height>0.5in</Height>
        <Width>8in</Width>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>INVOICE</Value>
                <Style>
                  <FontSize>24pt</FontSize>
                  <FontWeight>Bold</FontWeight>
                  <TextAlign>Center</TextAlign>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
      
      <!-- Invoice Info -->
      <Rectangle Name='InvoiceInfo'>
        <Top>2.8in</Top>
        <Left>0in</Left>
        <Height>1.2in</Height>
        <Width>8in</Width>
        <ReportItems>
          <Textbox Name='InvoiceDetails'>
            <Top>0in</Top>
            <Left>0in</Left>
            <Height>1.2in</Height>
            <Width>4in</Width>
            <Paragraphs>
              <Paragraph>
                <TextRuns>
                  <TextRun>
                    <Value>'Invoice #: ' + Parameters!InvoiceNumber.Value + vbCrLf + 
                           'Invoice Date: ' + Parameters!InvoiceDate.Value + vbCrLf + 
                           'Due Date: ' + Parameters!DueDate.Value</Value>
                    <Style>
                      <FontSize>11pt</FontSize>
                    </Style>
                  </TextRun>
                </TextRuns>
              </Paragraph>
            </Paragraphs>
          </Textbox>
          <Textbox Name='CustomerDetails'>
            <Top>0in</Top>
            <Left>4.5in</Left>
            <Height>1.2in</Height>
            <Width>3.5in</Width>
            <Paragraphs>
              <Paragraph>
                <TextRuns>
                  <TextRun>
                    <Value>'Bill To:' + vbCrLf + 
                           Parameters!CustomerName.Value + vbCrLf + 
                           Parameters!CustomerAddress.Value + vbCrLf + 
                           Parameters!CustomerCityState.Value</Value>
                    <Style>
                      <FontSize>11pt</FontSize>
                    </Style>
                  </TextRun>
                </TextRuns>
              </Paragraph>
            </Paragraphs>
          </Textbox>
        </ReportItems>
      </Rectangle>
      
      <!-- Invoice Items Table -->
      <Tablix Name='InvoiceItemsTable'>
        <DataSetName>InvoiceItems</DataSetName>
        <Top>4.2in</Top>
        <Left>0in</Left>
        <Height>1.5in</Height>
        <Width>8in</Width>
        <TablixBody>
          <TablixColumns>
            <TablixColumn>
              <Width>3.5in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>0.8in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>1.2in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>1.2in</Width>
            </TablixColumn>
            <TablixColumn>
              <Width>1.3in</Width>
            </TablixColumn>
          </TablixColumns>
          <TablixRows>
            <!-- Header Row -->
            <TablixRow>
              <Height>0.3in</Height>
              <TablixCells>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='DescriptionHeader'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Description</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='QtyHeader'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Qty</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='UnitPriceHeader'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Unit Price</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='TaxHeader'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Tax</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='TotalHeader'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>Total</Value>
                              <Style>
                                <FontWeight>Bold</FontWeight>
                              </Style>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
              </TablixCells>
            </TablixRow>
            <!-- Data Row -->
            <TablixRow>
              <Height>0.3in</Height>
              <TablixCells>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='Description'>
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
                    <Textbox Name='Quantity'>
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
                    <Textbox Name='UnitPrice'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>='$' + Format(Fields!UnitPrice.Value, 'N2')</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='TaxAmount'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>='$' + Format(Fields!TaxAmount.Value, 'N2')</Value>
                            </TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name='Total'>
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun>
                              <Value>='$' + Format(Fields!TotalWithTax.Value, 'N2')</Value>
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
        <TablixColumnHierarchy>
          <TablixMembers>
            <TablixMember />
            <TablixMember />
            <TablixMember />
            <TablixMember />
            <TablixMember />
          </TablixMembers>
        </TablixColumnHierarchy>
        <TablixRowHierarchy>
          <TablixMembers>
            <TablixMember>
              <Static>true</Static>
            </TablixMember>
            <TablixMember>
              <Group Name='InvoiceItemGroup'>
                <GroupExpressions>
                  <GroupExpression>=Fields!Description.Value</GroupExpression>
                </GroupExpressions>
              </Group>
            </TablixMember>
          </TablixMembers>
        </TablixRowHierarchy>
      </Tablix>
      
      <!-- Invoice Total -->
      <Textbox Name='InvoiceTotal'>
        <Top>6.0in</Top>
        <Left>5.5in</Left>
        <Height>0.8in</Height>
        <Width>2.5in</Width>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>'Subtotal: $' + Format(Sum(Fields!LineTotal.Value, 'InvoiceItems'), 'N2') + vbCrLf + 
                       'Tax: $' + Format(Sum(Fields!TaxAmount.Value, 'InvoiceItems'), 'N2') + vbCrLf + 
                       'TOTAL: $' + Format(Sum(Fields!TotalWithTax.Value, 'InvoiceItems'), 'N2')</Value>
                <Style>
                  <FontSize>12pt</FontSize>
                  <FontWeight>Bold</FontWeight>
                  <TextAlign>Right</TextAlign>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
      
      <!-- Payment Terms -->
      <Textbox Name='PaymentTerms'>
        <Top>7.0in</Top>
        <Left>0in</Left>
        <Height>1.0in</Height>
        <Width>8in</Width>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Payment Terms: Net 30 days' + vbCrLf + vbCrLf + 
                       'Thank you for your business!' + vbCrLf + 
                       'For questions about this invoice, please contact us at ' + Parameters!CompanyPhone.Value</Value>
                <Style>
                  <FontSize>10pt</FontSize>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
    </ReportItems>
  </Body>
  <Width>8.5in</Width>
  <Page>
    <PageHeight>11in</PageHeight>
    <PageWidth>8.5in</PageWidth>
    <LeftMargin>0.5in</LeftMargin>
    <RightMargin>0.5in</RightMargin>
    <TopMargin>0.5in</TopMargin>
    <BottomMargin>0.5in</BottomMargin>
  </Page>
  <EmbeddedImages>
    <EmbeddedImage Name='CompanyLogo'>
      <ImageData>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</ImageData>
      <MIMEType>image/png</MIMEType>
    </EmbeddedImage>
  </EmbeddedImages>
</Report>";
        }
        
        private static string GetOutputPath(string fileName)
        {
            var outputDir = @"C:\Output";
            Directory.CreateDirectory(outputDir);
            return Path.Combine(outputDir, fileName);
        }
    }
}