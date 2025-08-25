using System.Text;

namespace FluentReports.Core.PdfRenderer;

public class PdfDocument : IDisposable
{
    private readonly List<PdfPage> _pages = new();
    private readonly Dictionary<int, string> _objects = new();
    private int _nextObjectId = 1;
    private bool _disposed = false;

    public float DPI { get; set; } = 72f; // Default 72 DPI (standard PDF)

    public PdfPage AddPage(float width = 612f, float height = 792f) // Default Letter size in points
    {
        var page = new PdfPage(this, width, height);
        _pages.Add(page);
        return page;
    }

    public float InchesToPoints(float inches) => inches * DPI;
    public float PointsToInches(float points) => points / DPI;
    public float PixelsToPoints(float pixels) => pixels * 72f / DPI;
    public float PointsToPixels(float points) => points * DPI / 72f;

    internal int ReserveObjectId()
    {
        return _nextObjectId++;
    }

    internal void SetObject(int id, string content)
    {
        _objects[id] = content;
    }

    public byte[] ToByteArray()
    {
        var pdf = new StringBuilder();
        
        // PDF Header
        pdf.AppendLine("%PDF-1.4");
        
        var xrefPositions = new List<long>();
        
        // Prepare pages to generate their object content
        foreach (var page in _pages)
        {
            page.PrepareObjects();
        }
        
        // Get all object IDs in order
        var sortedObjects = _objects.Keys.OrderBy(x => x).ToList();
        
        // Write all objects
        foreach (var objId in sortedObjects)
        {
            xrefPositions.Add(pdf.Length);
            pdf.AppendLine($"{objId} 0 obj");
            pdf.AppendLine(_objects[objId]);
            pdf.AppendLine("endobj");
        }
        
        // Cross-reference table
        var xrefPos = pdf.Length;
        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {sortedObjects.Count + 1}");
        pdf.AppendLine("0000000000 65535 f ");
        
        foreach (var pos in xrefPositions)
        {
            pdf.AppendLine($"{pos:D10} 00000 n ");
        }
        
        // Trailer
        pdf.AppendLine("trailer");
        pdf.AppendLine("<<");
        pdf.AppendLine($"/Size {sortedObjects.Count + 1}");
        pdf.AppendLine("/Root 1 0 R");
        pdf.AppendLine(">>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefPos.ToString());
        pdf.AppendLine("%%EOF");
        
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var page in _pages)
            {
                page.Dispose();
            }
            _pages.Clear();
            _objects.Clear();
            _disposed = true;
        }
    }
}