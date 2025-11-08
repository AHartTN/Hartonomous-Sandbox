namespace Hartonomous.Api.DTOs.Graph.Query;

public class GraphQueryResponse
{
    public required List<Dictionary<string, object>> Results { get; set; }
    public int ResultCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? Query { get; set; }
}
