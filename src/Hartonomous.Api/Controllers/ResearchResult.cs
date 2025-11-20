using System.Collections.Generic;

namespace Hartonomous.Api.Controllers;

public class ResearchResult
{
    public long AtomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string Source { get; set; } = string.Empty;
    public string AtomType { get; set; } = string.Empty;
    public GeoPoint Location { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}
