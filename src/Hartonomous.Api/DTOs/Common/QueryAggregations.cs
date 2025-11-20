using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Common;


public class QueryAggregations
{
    public int TotalMatches { get; set; }
    public Dictionary<string, int> ByAtomType { get; set; } = new();
    public Dictionary<string, int> BySource { get; set; } = new();
    public List<string> TopTags { get; set; } = new();
}
