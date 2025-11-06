using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents audio data with spectral and waveform geometric representations for multi-modal analysis.
/// Stores audio as spatial geometries enabling time-frequency queries and pattern matching.
/// Maps to dbo.AudioData table from 02_MultiModalData.sql.
/// </summary>
public sealed class AudioData
{
    /// <summary>
    /// Gets or sets the unique identifier for the audio data.
    /// </summary>
    public long AudioId { get; set; }

    /// <summary>
    /// Gets or sets the file system path where the audio is stored.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Gets or sets the sample rate in Hz (e.g., 44100, 48000, 16000).
    /// </summary>
    public int SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the audio in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels (1 for mono, 2 for stereo).
    /// </summary>
    public byte NumChannels { get; set; }

    /// <summary>
    /// Gets or sets the audio format/codec (e.g., 'wav', 'mp3', 'flac', 'opus').
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the spectrogram as a 2D geometric heatmap (time × frequency).
    /// Enables spatial queries on frequency content over time.
    /// </summary>
    public Geometry? Spectrogram { get; set; }

    /// <summary>
    /// Gets or sets the mel-scale spectrogram geometry for perceptually-aligned audio analysis.
    /// </summary>
    public Geometry? MelSpectrogram { get; set; }

    /// <summary>
    /// Gets or sets the left (or mono) channel waveform as a LINESTRING geometry.
    /// X coordinate = time, Y coordinate = amplitude.
    /// </summary>
    public Geometry? WaveformLeft { get; set; }

    /// <summary>
    /// Gets or sets the right channel waveform as a LINESTRING geometry (null for mono audio).
    /// </summary>
    public Geometry? WaveformRight { get; set; }

    /// <summary>
    /// Gets or sets the global audio embedding vector (typically from audio transformer or CNN).
    /// </summary>
    public SqlVector<float>? GlobalEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the dimensionality of the global embedding vector.
    /// </summary>
    public int? GlobalEmbeddingDim { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., detected speech, music genres, transcripts).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the audio was ingested into the system.
    /// </summary>
    public DateTime? IngestionDate { get; set; }

    /// <summary>
    /// Gets or sets the collection of individual audio frames for temporal analysis.
    /// </summary>
    public ICollection<AudioFrame> Frames { get; set; } = new List<AudioFrame>();
}
