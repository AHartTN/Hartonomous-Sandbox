using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Configuration surface for CDC polling and checkpointing behavior.
/// </summary>
public sealed class CdcOptions
{
    /// <summary>
    /// Configuration section name used to bind options.
    /// </summary>
    public const string SectionName = "Cdc";

    [Range(1, 10_000)]
    public int BatchSize { get; set; } = 250;

    [Range(250, 60_000)]
    public int PollIntervalMilliseconds { get; set; } = 1_000;

    [Required]
    public string CheckpointTableName { get; set; } = "dbo.CdcCheckpoints";

    /// <summary>
    /// Optional maximum age (minutes) for LSN checkpoint retention before compaction.
    /// </summary>
    [Range(5, 10_080)]
    public int RetentionMinutes { get; set; } = 1_440;
}
