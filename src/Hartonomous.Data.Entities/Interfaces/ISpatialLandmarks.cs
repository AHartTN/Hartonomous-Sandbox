using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface ISpatialLandmarks
{
    int LandmarkId { get; set; }
    SqlVector<float> LandmarkVector { get; set; }
    Geometry? LandmarkPoint { get; set; }
    string? SelectionMethod { get; set; }
    string? Description { get; set; }
    DateTime CreatedUtc { get; set; }
}
