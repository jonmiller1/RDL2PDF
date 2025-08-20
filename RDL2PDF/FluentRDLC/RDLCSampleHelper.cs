using System.Data;

namespace FluentRDLC
{
    public static class RDLCSampleHelper
    {
        public static string CreateEmployeeReportWithHeaderFooter()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" 
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <PageHeader>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""HeaderTitle"">
        <Top>0.1in</Top>
        <Left>0in</Left>
        <Width>8in</Width>
        <Height>0.25in</Height>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=Parameters!ReportTitle.Value</Value>
                <Style>
                  <FontSize>18pt</FontSize>
                  <FontWeight>Bold</FontWeight>
                  <TextAlign>Center</TextAlign>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
      <Textbox Name=""HeaderDate"">
        <Top>0.4in</Top>
        <Left>6in</Left>
        <Width>2in</Width>
        <Height>0.2in</Height>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=Now()</Value>
                <Style>
                  <FontSize>10pt</FontSize>
                  <TextAlign>Right</TextAlign>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
    </ReportItems>
  </PageHeader>
  <Body>
    <ReportItems>
      <Tablix Name=""EmployeeTable"">
        <TablixBody>
          <TablixColumns>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1in</Width></TablixColumn>
            <TablixColumn><Width>1in</Width></TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""FirstNameHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>First Name</Value></TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""LastNameHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>Last Name</Value></TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""DepartmentHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>Department</Value></TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""SalaryHeader"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>Salary</Value></TextRun>
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
                    <Textbox Name=""FirstName"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>=Fields!FirstName.Value</Value></TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""LastName"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>=Fields!LastName.Value</Value></TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""Department"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>=Fields!Department.Value</Value></TextRun>
                          </TextRuns>
                        </Paragraph>
                      </Paragraphs>
                    </Textbox>
                  </CellContents>
                </TablixCell>
                <TablixCell>
                  <CellContents>
                    <Textbox Name=""Salary"">
                      <Paragraphs>
                        <Paragraph>
                          <TextRuns>
                            <TextRun><Value>=Fields!Salary.Value</Value></TextRun>
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
            <TablixMember /><TablixMember /><TablixMember /><TablixMember />
          </TablixMembers>
        </TablixColumnHierarchy>
        <TablixRowHierarchy>
          <TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name=""Details"" /></TablixMember>
          </TablixMembers>
        </TablixRowHierarchy>
        <DataSetName>EmployeeDataSet</DataSetName>
      </Tablix>
    </ReportItems>
  </Body>
  <PageFooter>
    <Height>0.5in</Height>
    <ReportItems>
      <Textbox Name=""FooterPageNumber"">
        <Top>0.1in</Top>
        <Left>3in</Left>
        <Width>2in</Width>
        <Height>0.2in</Height>
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>=PageNumber</Value>
                <Style>
                  <FontSize>10pt</FontSize>
                  <TextAlign>Center</TextAlign>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
    </ReportItems>
  </PageFooter>
</Report>";
        }

        public static string CreateIndicatorReport()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Body>
    <ReportItems>
      <Textbox Name=""Title"">
        <Paragraphs>
          <Paragraph>
            <TextRuns>
              <TextRun>
                <Value>Performance Dashboard</Value>
                <Style>
                  <FontSize>18pt</FontSize>
                  <FontWeight>Bold</FontWeight>
                </Style>
              </TextRun>
            </TextRuns>
          </Paragraph>
        </Paragraphs>
      </Textbox>
      <Gauge Name=""PerformanceGauge"">
        <Top>1in</Top>
        <Left>0in</Left>
        <Width>3in</Width>
        <Height>2in</Height>
        <Value>=Fields!PerformanceScore.Value</Value>
        <MinimumValue>0</MinimumValue>
        <MaximumValue>100</MaximumValue>
        <IndicatorStates>
          <IndicatorState>
            <StartValue>0</StartValue>
            <EndValue>30</EndValue>
            <Color>Red</Color>
          </IndicatorState>
          <IndicatorState>
            <StartValue>30</StartValue>
            <EndValue>70</EndValue>
            <Color>Yellow</Color>
          </IndicatorState>
          <IndicatorState>
            <StartValue>70</StartValue>
            <EndValue>100</EndValue>
            <Color>Green</Color>
          </IndicatorState>
        </IndicatorStates>
        <DataSetName>MetricsDataSet</DataSetName>
      </Gauge>
      <DataBar Name=""SalesDataBar"">
        <Top>1in</Top>
        <Left>4in</Left>
        <Width>3in</Width>
        <Height>0.5in</Height>
        <Value>=Fields!SalesAmount.Value</Value>
        <MinimumValue>0</MinimumValue>
        <MaximumValue>=Fields!SalesTarget.Value</MaximumValue>
        <DataSetName>MetricsDataSet</DataSetName>
      </DataBar>
      <LinearGauge Name=""QualityGauge"">
        <Top>2in</Top>
        <Left>0in</Left>
        <Width>4in</Width>
        <Height>1in</Height>
        <Value>=Fields!QualityScore.Value</Value>
        <MinimumValue>0</MinimumValue>
        <MaximumValue>5</MaximumValue>
        <IndicatorStates>
          <IndicatorState>
            <StartValue>0</StartValue>
            <EndValue>2</EndValue>
            <Color>Red</Color>
          </IndicatorState>
          <IndicatorState>
            <StartValue>2</StartValue>
            <EndValue>4</EndValue>
            <Color>Orange</Color>
          </IndicatorState>
          <IndicatorState>
            <StartValue>4</StartValue>
            <EndValue>5</EndValue>
            <Color>Green</Color>
          </IndicatorState>
        </IndicatorStates>
        <DataSetName>MetricsDataSet</DataSetName>
      </LinearGauge>
      <Sparkline Name=""TrendSparkline"">
        <Top>3.5in</Top>
        <Left>0in</Left>
        <Width>6in</Width>
        <Height>1in</Height>
        <Value>=Fields!TrendValue.Value</Value>
        <MinimumValue>0</MinimumValue>
        <MaximumValue>100</MaximumValue>
        <DataSetName>MetricsDataSet</DataSetName>
      </Sparkline>
    </ReportItems>
  </Body>
</Report>";
        }

        public static DataTable CreateSampleEmployeeData()
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

        public static DataTable CreateSampleMetricsData()
        {
            var table = new DataTable("Metrics");
            table.Columns.Add("PerformanceScore", typeof(double));
            table.Columns.Add("SalesAmount", typeof(double));
            table.Columns.Add("SalesTarget", typeof(double));
            table.Columns.Add("QualityScore", typeof(double));
            table.Columns.Add("TrendValue", typeof(double));

            table.Rows.Add(85.5, 125000, 150000, 4.2, 75.0);

            return table;
        }
    }
}