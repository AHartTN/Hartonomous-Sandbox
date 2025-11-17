using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class SpatialLandmarks : ISpatialLandmarks
{
    public int LandmarkId { get; set; }

    public SqlVector<float> LandmarkVector { get; set; }

    public Geometry? LandmarkPoint { get; set; }

    public string? SelectionMethod { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedUtc { get; set; }
}
