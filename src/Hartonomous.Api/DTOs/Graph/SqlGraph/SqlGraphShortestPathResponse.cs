namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Response from shortest path query
/// </summary>
public class SqlGraphShortestPathResponse
{
    public long StartAtomId { get; set; }
    public long EndAtomId { get; set; }
    public SqlGraphPathEntry? ShortestPath { get; set; }
    public bool PathFound { get; set; }
    public int ExecutionTimeMs { get; set; }
}
