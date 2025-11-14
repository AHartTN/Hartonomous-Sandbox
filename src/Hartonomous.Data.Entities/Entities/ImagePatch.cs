using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class ImagePatch : IImagePatch
{
    public long PatchId { get; set; }

    public long ImageId { get; set; }

    public long? ParentAtomId { get; set; }

    public int? PatchIndex { get; set; }

    public int PatchX { get; set; }

    public int PatchY { get; set; }

    public int? RowIndex { get; set; }

    public int? ColIndex { get; set; }

    public int PatchWidth { get; set; }

    public int PatchHeight { get; set; }

    public Geometry PatchRegion { get; set; } = null!;

    public Geometry? PatchGeometry { get; set; }

    public SqlVector<float>? PatchEmbedding { get; set; }

    public Geometry? DominantColor { get; set; }

    public float? MeanIntensity { get; set; }

    public double? MeanR { get; set; }

    public double? MeanG { get; set; }

    public double? MeanB { get; set; }

    public float? StdIntensity { get; set; }

    public double? Variance { get; set; }

    public int TenantId { get; set; }

    public virtual Image Image { get; set; } = null!;

    public virtual Atom? ParentAtom { get; set; }
}
