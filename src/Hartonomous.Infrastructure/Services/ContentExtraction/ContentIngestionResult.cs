using System.Collections.Generic;
using System.Linq;
using Hartonomous.Core.Interfaces;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

/// <summary>
/// Aggregated result after atom ingestion completes for a single content request.
/// </summary>
public sealed record ContentIngestionResult(
    string SourceId,
    IReadOnlyList<AtomIngestionResult> AtomResults,
    IReadOnlyDictionary<string, string>? Diagnostics = null)
{
    public int CreatedCount => AtomResults.Count(static r => !r.WasDuplicate);

    public int DuplicateCount => AtomResults.Count(static r => r.WasDuplicate);
}
