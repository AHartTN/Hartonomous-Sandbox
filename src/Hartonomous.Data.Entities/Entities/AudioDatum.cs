using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AudioDatum : IAudioDatum
{
    public long AudioId { get; set; }

    public string? SourcePath { get; set; }

    public int SampleRate { get; set; }

    public long DurationMs { get; set; }

    public byte NumChannels { get; set; }

    public string? Format { get; set; }

    public Geometry? Spectrogram { get; set; }

    public Geometry? MelSpectrogram { get; set; }

    public Geometry? WaveformLeft { get; set; }

    public Geometry? WaveformRight { get; set; }

    public SqlVector<float>? GlobalEmbedding { get; set; }

    public string? Metadata { get; set; }

    public DateTime? IngestionDate { get; set; }

    public virtual ICollection<AudioFrame> AudioFrames { get; set; } = new List<AudioFrame>();
}
