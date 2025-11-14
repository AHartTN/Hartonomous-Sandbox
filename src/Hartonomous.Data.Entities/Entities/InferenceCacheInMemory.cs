using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class InferenceCacheInMemory : IInferenceCacheInMemory
{
    public long CacheId { get; set; }

    public string CacheKey { get; set; } = null!;

    public int ModelId { get; set; }

    public string InferenceType { get; set; } = null!;

    public byte[] InputHash { get; set; } = null!;

    public byte[] OutputData { get; set; } = null!;

    public byte[]? IntermediateStates { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime LastAccessedUtc { get; set; }

    public long AccessCount { get; set; }

    public long? SizeBytes { get; set; }

    public double? ComputeTimeMs { get; set; }
}
