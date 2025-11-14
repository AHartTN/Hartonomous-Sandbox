using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomicWeight
{
    byte[] WeightHash { get; set; }
    float WeightValue { get; set; }
    byte[] WeightBytes { get; set; }
    Geometry? ValuePoint { get; set; }
    long ReferenceCount { get; set; }
    DateTime FirstSeen { get; set; }
    DateTime LastReferenced { get; set; }
}
