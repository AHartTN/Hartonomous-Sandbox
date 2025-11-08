namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Single path in SQL Server graph traversal result
/// </summary>
public class SqlGraphPathEntry
{
    public required List<long> NodeIds { get; set; }
    public required List<long> AtomIds { get; set; }
    public required List<string> EdgeTypes { get; set; }
    public int PathLength { get; set; }
    public double TotalWeight { get; set; }
}
