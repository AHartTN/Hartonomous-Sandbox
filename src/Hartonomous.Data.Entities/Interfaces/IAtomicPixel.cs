using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtomicPixel
{
    byte[] PixelHash { get; set; }
    byte R { get; set; }
    byte G { get; set; }
    byte B { get; set; }
    byte A { get; set; }
    byte[] RgbaBytes { get; set; }
    Geometry? ColorPoint { get; set; }
    long ReferenceCount { get; set; }
    DateTime FirstSeen { get; set; }
    DateTime LastReferenced { get; set; }
}
