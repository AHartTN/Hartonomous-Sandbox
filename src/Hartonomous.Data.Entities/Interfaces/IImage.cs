using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IImage
{
    long ImageId { get; set; }
    string? SourcePath { get; set; }
    string? SourceUrl { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    int Channels { get; set; }
    string? Format { get; set; }
    Geometry? PixelCloud { get; set; }
    Geometry? EdgeMap { get; set; }
    Geometry? ObjectRegions { get; set; }
    Geometry? SaliencyRegions { get; set; }
    SqlVector<float>? GlobalEmbedding { get; set; }
    string? Metadata { get; set; }
    DateTime? IngestionDate { get; set; }
    DateTime? LastAccessed { get; set; }
    long AccessCount { get; set; }
    ICollection<ImagePatch> ImagePatches { get; set; }
}
