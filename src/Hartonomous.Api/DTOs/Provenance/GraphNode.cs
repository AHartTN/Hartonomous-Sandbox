using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public class GraphNode
{
    public long AtomId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Importance { get; set; }
    public int Connections { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
