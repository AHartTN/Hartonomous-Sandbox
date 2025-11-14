using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class SessionPath : ISessionPath
{
    public long SessionPathId { get; set; }

    public Guid SessionId { get; set; }

    public Geometry Path { get; set; } = null!;

    public double? PathLength { get; set; }

    public double? StartTime { get; set; }

    public double? EndTime { get; set; }

    public int TenantId { get; set; }

    public DateTime CreatedAt { get; set; }
}
