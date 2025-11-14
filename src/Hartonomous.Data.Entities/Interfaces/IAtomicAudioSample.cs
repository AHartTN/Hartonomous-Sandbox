using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomicAudioSample
{
    byte[] SampleHash { get; set; }
    short AmplitudeInt16 { get; set; }
    float AmplitudeNormalized { get; set; }
    byte[] SampleBytes { get; set; }
    Geometry? AmplitudePoint { get; set; }
    long ReferenceCount { get; set; }
    DateTime FirstSeen { get; set; }
    DateTime LastReferenced { get; set; }
}
