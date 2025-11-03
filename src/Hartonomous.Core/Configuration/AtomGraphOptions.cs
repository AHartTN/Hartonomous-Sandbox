namespace Hartonomous.Core.Configuration;

/// <summary>
/// Options controlling the hybrid SQL graph surface for atoms and relations.
/// </summary>
public sealed class AtomGraphOptions
{
    public const string SectionName = "AtomGraph";

    /// <summary>
    /// When true, write-through updates populate the SQL graph node and edge tables.
    /// </summary>
    public bool EnableSqlGraphWrites { get; set; } = true;

    /// <summary>
    /// When true, background synchronization jobs can call into the provided stored procedure.
    /// </summary>
    public bool EnableSynchronizationJob { get; set; } = true;

    /// <summary>
    /// When true, on-demand projections (views/procs) are created for ad-hoc graph traversals.
    /// </summary>
    public bool EnableOnDemandProjection { get; set; } = true;

    /// <summary>
    /// Default batch size for the synchronization procedure.
    /// </summary>
    public int SyncBatchSize { get; set; } = 5000;
}
