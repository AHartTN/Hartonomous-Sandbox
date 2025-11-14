using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class Weight : IWeight
{
    public long WeightId { get; set; }

    public long LayerId { get; set; }

    public int NeuronIndex { get; set; }

    public string WeightType { get; set; } = null!;

    public float Value { get; set; }

    public float? Gradient { get; set; }

    public float? Momentum { get; set; }

    public DateTime LastUpdated { get; set; }

    public int UpdateCount { get; set; }

    public float? ImportanceScore { get; set; }

    public virtual ModelLayer Layer { get; set; } = null!;
}
