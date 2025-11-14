using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class AudioFrame : IAudioFrame
{
    public long AudioId { get; set; }

    public long FrameNumber { get; set; }

    public long? ParentAtomId { get; set; }

    public int? FrameIndex { get; set; }

    public long TimestampMs { get; set; }

    public double? StartTimeSec { get; set; }

    public double? EndTimeSec { get; set; }

    public float? AmplitudeL { get; set; }

    public float? AmplitudeR { get; set; }

    public double? RmsAmplitude { get; set; }

    public double? PeakAmplitude { get; set; }

    public Geometry? WaveformGeometry { get; set; }

    public float? SpectralCentroid { get; set; }

    public float? SpectralRolloff { get; set; }

    public float? ZeroCrossingRate { get; set; }

    public float? RmsEnergy { get; set; }

    public byte[]? Mfcc { get; set; }

    public SqlVector<float>? FrameEmbedding { get; set; }

    public int TenantId { get; set; }

    public virtual AudioDatum Audio { get; set; } = null!;

    public virtual Atom? ParentAtom { get; set; }
}
