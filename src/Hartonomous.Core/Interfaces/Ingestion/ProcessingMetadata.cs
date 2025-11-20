using System.Collections.Generic;

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Processing metadata about atomization.
/// </summary>
public class ProcessingMetadata
{
    /// <summary>
    /// Total atoms extracted.
    /// </summary>
    public required int TotalAtoms { get; init; }

    /// <summary>
    /// Unique atoms (after deduplication).
    /// </summary>
    public required int UniqueAtoms { get; init; }

    /// <summary>
    /// Processing duration in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Atomizer type used.
    /// </summary>
    public required string AtomizerType { get; init; }

    /// <summary>
    /// Format detected.
    /// </summary>
    public string? DetectedFormat { get; init; }

    /// <summary>
    /// Any warnings encountered.
    /// </summary>
    public List<string>? Warnings { get; init; }
}
