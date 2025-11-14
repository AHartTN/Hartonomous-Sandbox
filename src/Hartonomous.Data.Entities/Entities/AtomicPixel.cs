using System;
using System.Collections.Generic;
using Hartonomous.Shared.Contracts.Entities;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AtomicPixel : IAtomicPixel, IReferenceTrackedEntity
{
    public byte[] PixelHash { get; set; } = null!;

    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }

    public byte A { get; set; }

    public byte[] RgbaBytes { get; set; } = null!;

    public Geometry? ColorPoint { get; set; }

    public long ReferenceCount { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime LastReferenced { get; set; }
}
