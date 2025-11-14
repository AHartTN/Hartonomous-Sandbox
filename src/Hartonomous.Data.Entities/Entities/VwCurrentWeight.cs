using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class VwCurrentWeight : IVwCurrentWeight
{
    public long TensorAtomCoefficientId { get; set; }

    public long TensorAtomId { get; set; }

    public long AtomId { get; set; }

    public int? ModelId { get; set; }

    public long? LayerId { get; set; }

    public string AtomType { get; set; } = null!;

    public long ParentLayerId { get; set; }

    public string? TensorRole { get; set; }

    public float Coefficient { get; set; }

    public DateTime LastUpdated { get; set; }

    public float? ImportanceScore { get; set; }

    public string? AtomDescription { get; set; }

    public string? AtomSource { get; set; }
}
