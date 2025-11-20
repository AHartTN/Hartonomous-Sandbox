using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public class GraphRelationship
{
    public long From { get; set; }
    public long To { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Weight { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
