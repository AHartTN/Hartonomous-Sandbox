using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class VideoFrame : IVideoFrame
{
    public long FrameId { get; set; }

    public long VideoId { get; set; }

    public long FrameNumber { get; set; }

    public long TimestampMs { get; set; }

    public Geometry? PixelCloud { get; set; }

    public Geometry? ObjectRegions { get; set; }

    public Geometry? MotionVectors { get; set; }

    public Geometry? OpticalFlow { get; set; }

    public SqlVector<float>? FrameEmbedding { get; set; }

    public byte[]? PerceptualHash { get; set; }

    public virtual Video Video { get; set; } = null!;
}
