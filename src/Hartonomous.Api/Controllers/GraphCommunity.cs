using System.Collections.Generic;

namespace Hartonomous.Api.Controllers;

public class GraphCommunity
{
    public string CommunityId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<long> Members { get; set; } = new();
    public double Cohesion { get; set; }
}
