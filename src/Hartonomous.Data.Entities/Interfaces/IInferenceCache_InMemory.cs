using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IInferenceCache_InMemory
{
    long CacheId { get; set; }
    string CacheKey { get; set; }
    int ModelId { get; set; }
    string InferenceType { get; set; }
    byte[] InputHash { get; set; }
    byte[] OutputData { get; set; }
    byte[]? IntermediateStates { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime LastAccessedUtc { get; set; }
    long AccessCount { get; set; }
    long? SizeBytes { get; set; }
    double? ComputeTimeMs { get; set; }
}
