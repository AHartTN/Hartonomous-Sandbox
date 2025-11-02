using System.Collections.Generic;
using Hartonomous.Core.Interfaces;

namespace ModelIngestion.Content;

/// <summary>
/// Result returned by a content extractor before atom ingestion occurs.
/// </summary>
public sealed record ContentExtractionResult(
    IReadOnlyList<AtomIngestionRequest> AtomRequests,
    IReadOnlyDictionary<string, string>? Diagnostics = null);
