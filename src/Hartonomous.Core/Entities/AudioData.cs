using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Primary audio storage with spectral and waveform representations.
/// Maps to dbo.AudioData table from 02_MultiModalData.sql.
/// </summary>
public sealed class AudioData
{
    public long AudioId { get; set; }
    public string? SourcePath { get; set; }

    // Raw data
    public byte[]? RawData { get; set; }
    public int SampleRate { get; set; }
    public long DurationMs { get; set; }
    public byte NumChannels { get; set; } // 1 (mono), 2 (stereo)
    public string? Format { get; set; } // 'wav', 'mp3', 'flac'

    // Spectral representations (as spatial data) - NetTopologySuite types
    public Geometry? Spectrogram { get; set; } // 2D heatmap: time × frequency
    public Geometry? MelSpectrogram { get; set; }

    // Waveforms as geometry
    public Geometry? WaveformLeft { get; set; } // LINESTRING
    public Geometry? WaveformRight { get; set; }

    // Vector representations
    public SqlVector<float>? GlobalEmbedding { get; set; } // VECTOR(768)
    public int? GlobalEmbeddingDim { get; set; }

    // Metadata
    public string? Metadata { get; set; } // JSON

    public DateTime? IngestionDate { get; set; }

    // Navigation properties
    public ICollection<AudioFrame> Frames { get; set; } = new List<AudioFrame>();
}
