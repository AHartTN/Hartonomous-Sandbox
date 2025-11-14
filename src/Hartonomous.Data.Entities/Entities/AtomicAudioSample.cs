using System;
using System.Collections.Generic;
using Hartonomous.Shared.Contracts.Entities;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomicAudioSample : IAtomicAudioSample, IReferenceTrackedEntity
{
    public byte[] SampleHash { get; set; } = null!;

    public short AmplitudeInt16 { get; set; }

    public float AmplitudeNormalized { get; set; }

    public byte[] SampleBytes { get; set; } = null!;

    public Geometry? AmplitudePoint { get; set; }

    public long ReferenceCount { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime LastReferenced { get; set; }
}
