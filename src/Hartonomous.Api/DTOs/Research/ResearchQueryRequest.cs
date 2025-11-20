using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Research;


public class ResearchQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public List<string>? Filters { get; set; }
    public string? SortBy { get; set; }
    public int Limit { get; set; } = 10;
}
