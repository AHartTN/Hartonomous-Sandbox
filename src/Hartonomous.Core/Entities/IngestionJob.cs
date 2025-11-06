namespace Hartonomous.Core.Entities;

/// <summary>
/// Tracks ingestion operations and their metrics for auditing, performance monitoring, and troubleshooting.
/// Each ingestion job represents a unit of work processing content through an ingestion pipeline.
/// </summary>
public class IngestionJob
{
    /// <summary>
    /// Gets or sets the unique identifier for the ingestion job.
    /// </summary>
    public long IngestionJobId { get; set; }

    /// <summary>
    /// Gets or sets the name of the pipeline that processed this job.
    /// </summary>
    public required string PipelineName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the job started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the job completed (successfully or with errors).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the current status of the job (e.g., 'Pending', 'InProgress', 'Completed', 'Failed').
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the URI of the source content being ingested.
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., error messages, statistics, configuration).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the collection of atoms produced or referenced by this ingestion job.
    /// </summary>
    public ICollection<IngestionJobAtom> JobAtoms { get; set; } = new List<IngestionJobAtom>();
}
