using System.Data;
using System.Xml.Linq;

namespace FluentRDLC.Renderer
{
    public enum RenderFormat
    {
        PDF,
        Word,
        Excel,
        HTML,
        PNG
    }

    public class RenderContext
    {
        public XDocument RdlcDocument { get; set; } = null!;
        public XNamespace? RdlcNamespace { get; set; }
        public Dictionary<string, DataTable> DataSources { get; set; } = [];
        public Dictionary<string, object> Parameters { get; set; } = [];
        public int CurrentPageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public DataRow? CurrentDataRow { get; set; }
        public int Width { get; set; } = 600;
        public int Height { get; set; } = 800;
    }

    public interface IRenderer
    {
        RenderFormat Format { get; }
        byte[] Render(RenderContext context);
        string GetFileExtension();
        string GetMimeType();
    }
}