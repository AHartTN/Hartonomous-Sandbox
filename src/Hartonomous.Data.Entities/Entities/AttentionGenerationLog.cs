using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AttentionGenerationLog : IAttentionGenerationLog
{
    public int Id { get; set; }

    public int ModelId { get; set; }

    public string InputAtomIds { get; set; } = null!;

    public string? ContextJson { get; set; }

    public int MaxTokens { get; set; }

    public double Temperature { get; set; }

    public int TopK { get; set; }

    public double TopP { get; set; }

    public int AttentionHeads { get; set; }

    public long GenerationStreamId { get; set; }

    public string? GeneratedAtomIds { get; set; }

    public int DurationMs { get; set; }

    public int TenantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Model Model { get; set; } = null!;
}
