using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class CachedActivation : ICachedActivation
{
    public long CacheId { get; set; }

    public int ModelId { get; set; }

    public long LayerId { get; set; }

    public byte[] InputHash { get; set; } = null!;

    public byte[]? ActivationOutput { get; set; }

    public string? OutputShape { get; set; }

    public long HitCount { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime LastAccessed { get; set; }

    public long ComputeTimeSavedMs { get; set; }

    public virtual ModelLayer Layer { get; set; } = null!;

    public virtual Model Model { get; set; } = null!;
}
