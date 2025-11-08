namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Represents an embedding pattern detected during analysis
/// </summary>
public class EmbeddingPattern
{
    public long AtomId { get; set; }
    public string Modality { get; set; } = string.Empty;
    public int ClusterSize { get; set; }
}
