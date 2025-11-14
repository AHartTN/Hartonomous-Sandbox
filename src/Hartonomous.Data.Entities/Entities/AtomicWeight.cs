using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomicWeight : IAtomicWeight
{
    public byte[] WeightHash { get; set; } = null!;

    public float WeightValue { get; set; }

    public byte[] WeightBytes { get; set; } = null!;

    public Geometry? ValuePoint { get; set; }

    public long ReferenceCount { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime LastReferenced { get; set; }
}
