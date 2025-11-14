using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITransformerInferenceResult
{
    int Id { get; set; }
    Guid ProblemId { get; set; }
    string InputSequence { get; set; }
    int ModelId { get; set; }
    int Layers { get; set; }
    int AttentionHeads { get; set; }
    int FeedForwardDim { get; set; }
    string? LayerResults { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
    Model Model { get; set; }
}
