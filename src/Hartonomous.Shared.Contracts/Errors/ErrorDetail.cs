using System.Collections.Generic;

namespace Hartonomous.Shared.Contracts.Errors;

/// <summary>
/// Represents a machine-readable error detail shared across Hartonomous surfaces.
/// </summary>
public sealed record ErrorDetail(
    string Code,
    string Message,
    string? Target = null,
    IReadOnlyDictionary<string, object?>? Properties = null);
