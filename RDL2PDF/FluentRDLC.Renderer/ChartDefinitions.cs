namespace FluentRDLC.Renderer
{
    public enum ChartType
    {
        Column,
        Bar,
        Line,
        Pie,
        Area
    }

    public class ChartDefinition
    {
        public ChartType Type { get; set; }
        public ChartType ChartType { get; set; }
        public string Title { get; set; } = "";
        public List<ChartSeries> Series { get; set; } = [];
        public List<string> Categories { get; set; } = [];
        public int Width { get; set; } = 400;
        public int Height { get; set; } = 300;
    }

    public class ChartSeries
    {
        public string Name { get; set; } = "";
        public List<double> Values { get; set; } = [];
        public List<ChartDataPoint> DataPoints { get; set; } = [];
        public string Color { get; set; } = "";
    }

    public class ChartDataPoint
    {
        public double Value { get; set; }
        public string Category { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public class ChartSeriesData
    {
        public string Name { get; set; } = "";
        public List<ChartDataPoint> DataPoints { get; set; } = [];
    }
}