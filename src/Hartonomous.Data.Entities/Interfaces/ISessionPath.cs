using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public interface ISessionPath
{
    long SessionPathId { get; set; }
    Guid SessionId { get; set; }
    Geometry Path { get; set; }
    double? PathLength { get; set; }
    double? StartTime { get; set; }
    double? EndTime { get; set; }
    int TenantId { get; set; }
    DateTime CreatedAt { get; set; }
}
