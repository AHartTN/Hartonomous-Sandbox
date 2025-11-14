using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class TensorAtomCoefficient : ITensorAtomCoefficient
{
    public long TensorAtomCoefficientId { get; set; }

    public long TensorAtomId { get; set; }

    public long ParentLayerId { get; set; }

    public string? TensorRole { get; set; }

    public float Coefficient { get; set; }

    public virtual ModelLayer ParentLayer { get; set; } = null!;

    public virtual TensorAtom TensorAtom { get; set; } = null!;
}
