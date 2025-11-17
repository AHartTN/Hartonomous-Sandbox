using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAttentionGenerationLog
{
    int Id { get; set; }
    int ModelId { get; set; }
    string InputAtomIds { get; set; }
    string? ContextJson { get; set; }
    int MaxTokens { get; set; }
    double Temperature { get; set; }
    int TopK { get; set; }
    double TopP { get; set; }
    int AttentionHeads { get; set; }
    long GenerationStreamId { get; set; }
    string? GeneratedAtomIds { get; set; }
    int DurationMs { get; set; }
    int TenantId { get; set; }
    DateTime CreatedAt { get; set; }
    Models Model { get; set; }
}
