using System;

using Hartonomous.Api.DTOs.Provenance;

namespace Hartonomous.Api.DTOs.MLOps;


public record ErrorCluster
{
    public required string ClusterId { get; init; }
    public required string ErrorType { get; init; }
    public int ErrorCount { get; init; }
    public required GeoJsonPoint Centroid { get; init; }
    public double Radius { get; init; }
    public DateTime FirstOccurrence { get; init; }
    public DateTime LastOccurrence { get; init; }
    public required ErrorSeverity Severity { get; init; }
}
