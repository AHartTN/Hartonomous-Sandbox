using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class TransformerInferenceResult : ITransformerInferenceResult
{
    public int Id { get; set; }

    public Guid ProblemId { get; set; }

    public string InputSequence { get; set; } = null!;

    public int ModelId { get; set; }

    public int Layers { get; set; }

    public int AttentionHeads { get; set; }

    public int FeedForwardDim { get; set; }

    public string? LayerResults { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Model Model { get; set; } = null!;
}
