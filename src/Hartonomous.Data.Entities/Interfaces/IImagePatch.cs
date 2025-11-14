using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IImagePatch
{
    long PatchId { get; set; }
    long ImageId { get; set; }
    long? ParentAtomId { get; set; }
    int? PatchIndex { get; set; }
    int PatchX { get; set; }
    int PatchY { get; set; }
    int? RowIndex { get; set; }
    int? ColIndex { get; set; }
    int PatchWidth { get; set; }
    int PatchHeight { get; set; }
    Geometry PatchRegion { get; set; }
    Geometry? PatchGeometry { get; set; }
    SqlVector<float>? PatchEmbedding { get; set; }
    Geometry? DominantColor { get; set; }
    float? MeanIntensity { get; set; }
    double? MeanR { get; set; }
    double? MeanG { get; set; }
    double? MeanB { get; set; }
    float? StdIntensity { get; set; }
    double? Variance { get; set; }
    int TenantId { get; set; }
    Image Image { get; set; }
    Atom? ParentAtom { get; set; }
}
