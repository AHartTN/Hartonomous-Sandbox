using NetTopologySuite.Geometries;
using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a unique atomic pixel with content-addressable deduplication.
/// Stores RGBA values and spatial color representation.
/// </summary>
public class AtomicPixel : IReferenceTrackedEntity
{
    /// <summary>
    /// Gets or sets the SHA256 hash of the pixel (r,g,b,a) - serves as primary key.
    /// </summary>
    public byte[] PixelHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the red channel value (0-255).
    /// </summary>
    public byte R { get; set; }

    /// <summary>
    /// Gets or sets the green channel value (0-255).
    /// </summary>
    public byte G { get; set; }

    /// <summary>
    /// Gets or sets the blue channel value (0-255).
    /// </summary>
    public byte B { get; set; }

    /// <summary>
    /// Gets or sets the alpha channel value (0-255).
    /// </summary>
    public byte A { get; set; } = 255;

    /// <summary>
    /// Gets or sets the spatial representation in RGB color space.
    /// </summary>
    public Point? ColorPoint { get; set; }

    /// <summary>
    /// Gets or sets the number of times this pixel is referenced.
    /// </summary>
    public long ReferenceCount { get; set; }

    /// <summary>
    /// Gets or sets when this pixel was first seen.
    /// </summary>
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this pixel was last referenced.
    /// </summary>
    public DateTime LastReferenced { get; set; } = DateTime.UtcNow;
}