namespace Hartonomous.Core.Entities;

/// <summary>
/// Tracks ingestion operations and their metrics.
/// </summary>
public class IngestionJob
{
    public long IngestionJobId { get; set; }

    public required string PipelineName { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? Status { get; set; }

    public string? SourceUri { get; set; }

    public string? Metadata { get; set; }

    public ICollection<IngestionJobAtom> JobAtoms { get; set; } = new List<IngestionJobAtom>();
}
