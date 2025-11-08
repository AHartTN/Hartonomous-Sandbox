namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Response from creating a SQL Server graph edge
/// </summary>
public class SqlGraphCreateEdgeResponse
{
    public long EdgeId { get; set; }
    public long FromNodeId { get; set; }
    public long ToNodeId { get; set; }
    public required string EdgeType { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
