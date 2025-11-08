using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.Query;

public class GraphQueryRequest
{
    [Required]
    public required string CypherQuery { get; set; }

    public Dictionary<string, object>? Parameters { get; set; }

    [Range(1, 10000)]
    public int Limit { get; set; } = 100;
}
