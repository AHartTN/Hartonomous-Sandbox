using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITensorAtomCoefficient
{
    long TensorAtomCoefficientId { get; set; }
    long TensorAtomId { get; set; }
    long ParentLayerId { get; set; }
    string? TensorRole { get; set; }
    float Coefficient { get; set; }
    ModelLayer ParentLayer { get; set; }
    TensorAtom TensorAtom { get; set; }
}
