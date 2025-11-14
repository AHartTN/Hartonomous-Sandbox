using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IWeight
{
    long WeightId { get; set; }
    long LayerId { get; set; }
    int NeuronIndex { get; set; }
    string WeightType { get; set; }
    float Value { get; set; }
    float? Gradient { get; set; }
    float? Momentum { get; set; }
    DateTime LastUpdated { get; set; }
    int UpdateCount { get; set; }
    float? ImportanceScore { get; set; }
    ModelLayer Layer { get; set; }
}
