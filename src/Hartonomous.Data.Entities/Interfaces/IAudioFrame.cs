using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAudioFrame
{
    long AudioId { get; set; }
    long FrameNumber { get; set; }
    long? ParentAtomId { get; set; }
    int? FrameIndex { get; set; }
    long TimestampMs { get; set; }
    double? StartTimeSec { get; set; }
    double? EndTimeSec { get; set; }
    float? AmplitudeL { get; set; }
    float? AmplitudeR { get; set; }
    double? RmsAmplitude { get; set; }
    double? PeakAmplitude { get; set; }
    Geometry? WaveformGeometry { get; set; }
    float? SpectralCentroid { get; set; }
    float? SpectralRolloff { get; set; }
    float? ZeroCrossingRate { get; set; }
    float? RmsEnergy { get; set; }
    byte[]? Mfcc { get; set; }
    SqlVector<float>? FrameEmbedding { get; set; }
    int TenantId { get; set; }
    AudioDatum Audio { get; set; }
    Atom? ParentAtom { get; set; }
}
