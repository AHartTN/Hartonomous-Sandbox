using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class Image : IImage
{
    public long ImageId { get; set; }

    public string? SourcePath { get; set; }

    public string? SourceUrl { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Channels { get; set; }

    public string? Format { get; set; }

    public Geometry? PixelCloud { get; set; }

    public Geometry? EdgeMap { get; set; }

    public Geometry? ObjectRegions { get; set; }

    public Geometry? SaliencyRegions { get; set; }

    public SqlVector<float>? GlobalEmbedding { get; set; }

    public string? Metadata { get; set; }

    public DateTime? IngestionDate { get; set; }

    public DateTime? LastAccessed { get; set; }

    public long AccessCount { get; set; }

    public virtual ICollection<ImagePatch> ImagePatches { get; set; } = new List<ImagePatch>();
}
