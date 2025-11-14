using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class VwWeightChangeHistory : IVwWeightChangeHistory
{
    public long TensorAtomCoefficientId { get; set; }

    public long TensorAtomId { get; set; }

    public long ParentLayerId { get; set; }

    public string? TensorRole { get; set; }

    public float Coefficient { get; set; }

    public DateTime ChangedAt { get; set; }

    public DateTime ValidUntil { get; set; }

    public int? DurationSeconds { get; set; }

    public float? PreviousCoefficient { get; set; }

    public float? CoefficientDelta { get; set; }
}
