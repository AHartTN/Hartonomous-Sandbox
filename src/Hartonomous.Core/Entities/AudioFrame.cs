using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents frame-by-frame temporal audio data with spectral features for detailed analysis.
/// Each frame captures a time slice of audio with computed acoustic features.
/// Maps to dbo.AudioFrames table from 02_MultiModalData.sql.
/// Stored with COLUMNSTORE for extreme compression.
/// </summary>
public sealed class AudioFrame
{
    /// <summary>
    /// Gets or sets the identifier of the parent audio data.
    /// </summary>
    public long AudioId { get; set; }

    /// <summary>
    /// Gets or sets the sequential frame number within the audio (0-based).
    /// </summary>
    public long FrameNumber { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of this frame in milliseconds from the start of the audio.
    /// </summary>
    public long TimestampMs { get; set; }

    /// <summary>
    /// Gets or sets the amplitude of the left (or mono) channel.
    /// </summary>
    public float? AmplitudeL { get; set; }

    /// <summary>
    /// Gets or sets the amplitude of the right channel (null for mono audio).
    /// </summary>
    public float? AmplitudeR { get; set; }

    /// <summary>
    /// Gets or sets the spectral centroid - center of mass of the spectrum (brightness measure).
    /// </summary>
    public float? SpectralCentroid { get; set; }

    /// <summary>
    /// Gets or sets the spectral rolloff - frequency below which 85% of energy is contained.
    /// </summary>
    public float? SpectralRolloff { get; set; }

    /// <summary>
    /// Gets or sets the zero-crossing rate - rate of sign changes in the signal (noisiness measure).
    /// </summary>
    public float? ZeroCrossingRate { get; set; }

    /// <summary>
    /// Gets or sets the root mean square energy of the frame (loudness measure).
    /// </summary>
    public float? RmsEnergy { get; set; }

    /// <summary>
    /// Gets or sets the Mel-frequency cepstral coefficients (MFCCs) for speech and music analysis.
    /// Typically 13 coefficients representing the spectral envelope.
    /// </summary>
    public SqlVector<float>? Mfcc { get; set; }

    /// <summary>
    /// Gets or sets the frame embedding vector from an audio neural network.
    /// </summary>
    public SqlVector<float>? FrameEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent audio data.
    /// </summary>
    public AudioData Audio { get; set; } = null!;
}
