using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAudioDatum
{
    long AudioId { get; set; }
    string? SourcePath { get; set; }
    int SampleRate { get; set; }
    long DurationMs { get; set; }
    byte NumChannels { get; set; }
    string? Format { get; set; }
    Geometry? Spectrogram { get; set; }
    Geometry? MelSpectrogram { get; set; }
    Geometry? WaveformLeft { get; set; }
    Geometry? WaveformRight { get; set; }
    SqlVector<float>? GlobalEmbedding { get; set; }
    string? Metadata { get; set; }
    DateTime? IngestionDate { get; set; }
    ICollection<AudioFrame> AudioFrames { get; set; }
}
