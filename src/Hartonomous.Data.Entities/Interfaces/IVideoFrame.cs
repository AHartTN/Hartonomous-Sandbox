using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IVideoFrame
{
    long FrameId { get; set; }
    long VideoId { get; set; }
    long FrameNumber { get; set; }
    long TimestampMs { get; set; }
    Geometry? PixelCloud { get; set; }
    Geometry? ObjectRegions { get; set; }
    Geometry? MotionVectors { get; set; }
    Geometry? OpticalFlow { get; set; }
    SqlVector<float>? FrameEmbedding { get; set; }
    byte[]? PerceptualHash { get; set; }
    Video Video { get; set; }
}
