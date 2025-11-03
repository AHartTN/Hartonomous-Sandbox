using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a unique atomic audio sample with content-addressable deduplication.
/// Stores normalized amplitude values for audio processing.
/// </summary>
public class AtomicAudioSample : IReferenceTrackedEntity
{
    /// <summary>
    /// Gets or sets the SHA256 hash of the sample - serves as primary key.
    /// </summary>
    public byte[] SampleHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the normalized amplitude (-1.0 to 1.0).
    /// </summary>
    public float AmplitudeNormalized { get; set; }

    /// <summary>
    /// Gets or sets the raw int16 amplitude value.
    /// </summary>
    public short AmplitudeInt16 { get; set; }

    /// <summary>
    /// Gets or sets the number of times this sample is referenced.
    /// </summary>
    public long ReferenceCount { get; set; }

    /// <summary>
    /// Gets or sets when this sample was first seen.
    /// </summary>
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this sample was last referenced.
    /// </summary>
    public DateTime LastReferenced { get; set; } = DateTime.UtcNow;
}