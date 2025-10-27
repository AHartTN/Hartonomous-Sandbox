using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Frame-by-frame temporal audio data with spectral features.
/// Maps to dbo.AudioFrames table from 02_MultiModalData.sql.
/// Stored with COLUMNSTORE for extreme compression.
/// </summary>
public sealed class AudioFrame
{
    public long AudioId { get; set; }
    public long FrameNumber { get; set; }
    public long TimestampMs { get; set; }

    // Amplitude data
    public float? AmplitudeL { get; set; }
    public float? AmplitudeR { get; set; }

    // Spectral features per frame
    public float? SpectralCentroid { get; set; }
    public float? SpectralRolloff { get; set; }
    public float? ZeroCrossingRate { get; set; }
    public float? RmsEnergy { get; set; }

    // MFCC features
    public SqlVector<float>? Mfcc { get; set; } // VECTOR(13)

    // Frame embedding
    public SqlVector<float>? FrameEmbedding { get; set; } // VECTOR(768)

    // Navigation property
    public AudioData Audio { get; set; } = null!;
}
