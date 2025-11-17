using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IStreamFusionResult
{
    int Id { get; set; }
    string StreamIds { get; set; }
    string FusionType { get; set; }
    string? Weights { get; set; }
    byte[]? FusedStream { get; set; }
    int? ComponentCount { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
}
