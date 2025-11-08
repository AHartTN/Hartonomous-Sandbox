namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Response from creating a SQL Server graph node
/// </summary>
public class SqlGraphCreateNodeResponse
{
    public long NodeId { get; set; }
    public long AtomId { get; set; }
    public required string NodeType { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
