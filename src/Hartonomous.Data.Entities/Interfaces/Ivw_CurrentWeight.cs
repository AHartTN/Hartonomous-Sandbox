using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface Ivw_CurrentWeight
{
    long TensorAtomCoefficientId { get; set; }
    long TensorAtomId { get; set; }
    long AtomId { get; set; }
    int? ModelId { get; set; }
    long? LayerId { get; set; }
    string AtomType { get; set; }
    long ParentLayerId { get; set; }
    string? TensorRole { get; set; }
    float Coefficient { get; set; }
    DateTime LastUpdated { get; set; }
    float? ImportanceScore { get; set; }
    string? AtomDescription { get; set; }
    string? AtomSource { get; set; }
}
