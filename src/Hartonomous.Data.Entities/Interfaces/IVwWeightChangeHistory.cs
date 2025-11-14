using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IVwWeightChangeHistory
{
    long TensorAtomCoefficientId { get; set; }
    long TensorAtomId { get; set; }
    long ParentLayerId { get; set; }
    string? TensorRole { get; set; }
    float Coefficient { get; set; }
    DateTime ChangedAt { get; set; }
    DateTime ValidUntil { get; set; }
    int? DurationSeconds { get; set; }
    float? PreviousCoefficient { get; set; }
    float? CoefficientDelta { get; set; }
}
