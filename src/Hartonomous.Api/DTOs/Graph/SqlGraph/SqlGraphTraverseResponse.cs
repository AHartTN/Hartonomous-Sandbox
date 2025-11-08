namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Response from SQL Server graph traversal
/// </summary>
public class SqlGraphTraverseResponse
{
    public long StartAtomId { get; set; }
    public long? EndAtomId { get; set; }
    public required List<SqlGraphPathEntry> Paths { get; set; }
    public int TotalPathsFound { get; set; }
    public int ExecutionTimeMs { get; set; }
}
