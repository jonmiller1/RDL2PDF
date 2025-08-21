using FluentRDLC.Renderer.Renderers;

namespace FluentRDLC.Renderer
{
    public static class RendererFactory
    {
        public static IRenderer CreateRenderer(RenderFormat format)
        {
            return format switch
            {
                RenderFormat.PDF => new PdfRenderer(),
                RenderFormat.Word => new WordRenderer(),
                RenderFormat.Excel => new ExcelRenderer(),
                RenderFormat.HTML => new HtmlRenderer(),
                RenderFormat.PNG => new PngRenderer(),
                _ => throw new ArgumentException($"Unsupported render format: {format}", nameof(format))
            };
        }

        public static IEnumerable<RenderFormat> GetSupportedFormats()
        {
            return Enum.GetValues<RenderFormat>();
        }

        public static string GetFileExtension(RenderFormat format)
        {
            var renderer = CreateRenderer(format);
            return renderer.GetFileExtension();
        }

        public static string GetMimeType(RenderFormat format)
        {
            var renderer = CreateRenderer(format);
            return renderer.GetMimeType();
        }
    }
}