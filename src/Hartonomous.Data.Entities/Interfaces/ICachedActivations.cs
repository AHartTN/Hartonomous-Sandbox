using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ICachedActivations
{
    long CacheId { get; set; }
    int ModelId { get; set; }
    long LayerId { get; set; }
    byte[] InputHash { get; set; }
    byte[]? ActivationOutput { get; set; }
    string? OutputShape { get; set; }
    long HitCount { get; set; }
    DateTime CreatedDate { get; set; }
    DateTime LastAccessed { get; set; }
    long ComputeTimeSavedMs { get; set; }
    ModelLayers Layer { get; set; }
    Models Model { get; set; }
}
