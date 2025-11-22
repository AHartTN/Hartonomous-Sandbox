namespace Hartonomous.Workers.EmbeddingGenerator.Models;

/// <summary>
/// Parameters for embedding generation job
/// </summary>
internal class EmbeddingJobParameters
{
    public long AtomId { get; set; }
    public int TenantId { get; set; }
    public string? Modality { get; set; }
}
