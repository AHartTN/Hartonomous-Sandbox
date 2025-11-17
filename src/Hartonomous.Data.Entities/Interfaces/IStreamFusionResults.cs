using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IStreamFusionResults
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
