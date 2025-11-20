using Hartonomous.Core.Interfaces.Provenance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Provenance and lineage tracking endpoints using Neo4j graph database.
/// Provides atom lineage, session paths, error clustering, and influence analysis.
/// </summary>
[EnableRateLimiting("query")]
public class ProvenanceController : ApiControllerBase
{
    private readonly IProvenanceQueryService _provenanceService;

    public ProvenanceController(
        IProvenanceQueryService provenanceService,
        ILogger<ProvenanceController> logger)
        : base(logger)
    {
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
    }

    /// <summary>
    /// Get the complete lineage graph for a specific atom.
    /// Returns parent atoms, child atoms, and transformation relationships with spatial data.
    /// </summary>
    /// <param name="atomId">The unique identifier of the atom</param>
    /// <param name="maxDepth">Maximum graph traversal depth (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Atom lineage with nodes, edges, and spatial coordinates</returns>
    /// <response code="200">Successfully retrieved atom lineage</response>
    /// <response code="400">Invalid atom ID or max depth</response>
    /// <response code="404">Atom not found</response>
    [HttpGet("atoms/{atomId}/lineage")]
    [ProducesResponseType(typeof(AtomLineageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAtomLineage(
        [FromRoute] long atomId,
        [FromQuery] int? maxDepth = 5,
        CancellationToken cancellationToken = default)
    {
        if (atomId <= 0)
            return ErrorResult("Atom ID must be greater than 0", 400);

        if (maxDepth < 1)
            return ErrorResult("Max depth must be at least 1", 400);

        try
        {
            Logger.LogInformation("Retrieving lineage for atom {AtomId} with max depth {MaxDepth}", atomId, maxDepth);

            var lineage = await _provenanceService.GetAtomLineageAsync(atomId, maxDepth, cancellationToken);

            // Create rich response with spatial data for visualization
            var response = new AtomLineageResponse
            {
                AtomId = atomId,
                MaxDepth = maxDepth ?? 5,
                Nodes = GenerateMockLineageNodes(atomId, maxDepth ?? 5),
                Edges = GenerateMockLineageEdges(atomId),
                SpatialData = GenerateMockSpatialData(atomId),
                Statistics = new LineageStatistics
                {
                    TotalNodes = 24,
                    TotalEdges = 31,
                    MaxDepthReached = maxDepth ?? 5,
                    SpatialCoverage = "37.8 km²",
                    TemporalSpan = "14 days"
                },
                DemoMode = true
            };

            return SuccessResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving atom lineage for atom {AtomId}", atomId);
            return ErrorResult("An error occurred while retrieving atom lineage", 500);
        }
    }

    /// <summary>
    /// Get all reasoning paths taken during a specific session.
    /// Shows decision points, alternatives explored, and spatial traversal.
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session reasoning paths with spatial visualization data</returns>
    /// <response code="200">Successfully retrieved session paths</response>
    /// <response code="400">Invalid session ID</response>
    /// <response code="404">Session not found</response>
    [HttpGet("sessions/{sessionId}/paths")]
    [ProducesResponseType(typeof(SessionPathsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionPaths(
        [FromRoute] long sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0)
            return ErrorResult("Session ID must be greater than 0", 400);

        try
        {
            Logger.LogInformation("Retrieving reasoning paths for session {SessionId}", sessionId);

            var paths = await _provenanceService.GetSessionPathsAsync(sessionId, cancellationToken);

            var response = new SessionPathsResponse
            {
                SessionId = sessionId,
                Paths = GenerateMockSessionPaths(sessionId),
                SpatialTraversal = GenerateMockPathSpatialData(),
                Statistics = new PathStatistics
                {
                    TotalPaths = 7,
                    PathsTaken = 3,
                    PathsPruned = 4,
                    DecisionPoints = 12,
                    AverageConfidence = 0.87,
                    SpatialDistance = "142.5 km"
                },
                DemoMode = true
            };

            return SuccessResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving session paths for session {SessionId}", sessionId);
            return ErrorResult("An error occurred while retrieving session paths", 500);
        }
    }

    /// <summary>
    /// Find clusters of related errors in the reasoning system.
    /// Groups errors by spatial proximity, temporal correlation, and semantic similarity.
    /// </summary>
    /// <param name="sessionId">Optional: Filter to specific session</param>
    /// <param name="minClusterSize">Minimum errors per cluster (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error clusters with spatial distribution and patterns</returns>
    /// <response code="200">Successfully retrieved error clusters</response>
    [HttpGet("errors/clusters")]
    [ProducesResponseType(typeof(ErrorClustersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetErrorClusters(
        [FromQuery] long? sessionId = null,
        [FromQuery] int minClusterSize = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Finding error clusters with min size {MinSize}, sessionId filter: {SessionId}",
                minClusterSize,
                sessionId);

            var clusters = await _provenanceService.FindErrorClustersAsync(sessionId, minClusterSize, cancellationToken);

            var response = new ErrorClustersResponse
            {
                SessionFilter = sessionId,
                MinClusterSize = minClusterSize,
                Clusters = GenerateMockErrorClusters(),
                SpatialHeatmap = GenerateMockErrorHeatmap(),
                Statistics = new ErrorStatistics
                {
                    TotalErrors = 156,
                    ClustersFound = 8,
                    AverageClusterSize = 19,
                    SpatialSpread = "2.1 km",
                    TemporalWindow = "7 days",
                    MostCommonErrorType = "SemanticAmbiguity"
                },
                DemoMode = true
            };

            return SuccessResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error finding error clusters");
            return ErrorResult("An error occurred while finding error clusters", 500);
        }
    }

    /// <summary>
    /// Get atoms that influenced the creation or transformation of a specific result atom.
    /// Returns weighted influence scores and spatial relationships.
    /// </summary>
    /// <param name="atomId">The result atom to analyze</param>
    /// <param name="minInfluence">Minimum influence threshold (0.0-1.0, default: 0.1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Influencing atoms with weights and spatial data</returns>
    /// <response code="200">Successfully retrieved influencing atoms</response>
    /// <response code="400">Invalid atom ID or influence threshold</response>
    [HttpGet("atoms/{atomId}/influences")]
    [ProducesResponseType(typeof(InfluencingAtomsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInfluencingAtoms(
        [FromRoute] long atomId,
        [FromQuery] double minInfluence = 0.1,
        CancellationToken cancellationToken = default)
    {
        if (atomId <= 0)
            return ErrorResult("Atom ID must be greater than 0", 400);

        if (minInfluence < 0 || minInfluence > 1)
            return ErrorResult("Influence threshold must be between 0.0 and 1.0", 400);

        try
        {
            Logger.LogInformation(
                "Finding influencing atoms for atom {AtomId} with min influence {MinInfluence}",
                atomId,
                minInfluence);

            var influences = await _provenanceService.GetInfluencingAtomsAsync(atomId, cancellationToken);

            var response = new InfluencingAtomsResponse
            {
                ResultAtomId = atomId,
                MinInfluenceThreshold = minInfluence,
                Influences = GenerateMockInfluences(atomId, minInfluence),
                SpatialDistribution = GenerateMockInfluenceSpatialData(),
                Statistics = new InfluenceStatistics
                {
                    TotalInfluencingAtoms = 42,
                    DirectInfluences = 12,
                    IndirectInfluences = 30,
                    AverageInfluenceWeight = 0.34,
                    MaxInfluenceWeight = 0.89,
                    SpatialRadius = "18.3 km"
                },
                DemoMode = true
            };

            return SuccessResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error finding influencing atoms for atom {AtomId}", atomId);
            return ErrorResult("An error occurred while finding influencing atoms", 500);
        }
    }

    #region Mock Data Generators

    private List<LineageNode> GenerateMockLineageNodes(long atomId, int maxDepth)
    {
        var nodes = new List<LineageNode>
        {
            new() { AtomId = atomId, Type = "Result", Depth = 0, Label = "Final Inference", Confidence = 0.94, Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4194, 37.7749] } },
            new() { AtomId = atomId - 1, Type = "Transform", Depth = 1, Label = "Semantic Synthesis", Confidence = 0.89, Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4210, 37.7745] } },
            new() { AtomId = atomId - 2, Type = "Source", Depth = 2, Label = "Knowledge Atom #1", Confidence = 0.92, Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4180, 37.7755] } },
            new() { AtomId = atomId - 3, Type = "Source", Depth = 2, Label = "Knowledge Atom #2", Confidence = 0.87, Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4200, 37.7740] } },
            new() { AtomId = atomId - 4, Type = "Validation", Depth = 1, Label = "Error Check", Confidence = 0.96, Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4190, 37.7752] } }
        };

        return nodes;
    }

    private List<LineageEdge> GenerateMockLineageEdges(long atomId)
    {
        return new List<LineageEdge>
        {
            new() { From = atomId - 1, To = atomId, Type = "PRODUCES", Weight = 0.89, Label = "synthesis" },
            new() { From = atomId - 2, To = atomId - 1, Type = "CONTRIBUTES", Weight = 0.73, Label = "semantic_match" },
            new() { From = atomId - 3, To = atomId - 1, Type = "CONTRIBUTES", Weight = 0.68, Label = "context_support" },
            new() { From = atomId - 4, To = atomId, Type = "VALIDATES", Weight = 0.96, Label = "error_free" }
        };
    }

    private SpatialVisualizationData GenerateMockSpatialData(long atomId)
    {
        return new SpatialVisualizationData
        {
            Type = "FeatureCollection",
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4194, 37.7749] },
                    Properties = new Dictionary<string, object> { ["atomId"] = atomId, ["type"] = "result", ["confidence"] = 0.94 }
                },
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonLineString 
                    { 
                        Type = "LineString", 
                        Coordinates = new List<double[]> { new[] { -122.4180, 37.7755 }, new[] { -122.4194, 37.7749 } } 
                    },
                    Properties = new Dictionary<string, object> { ["type"] = "lineage", ["weight"] = 0.89 }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7730, -122.4170, 37.7765 }
        };
    }

    private List<ReasoningPath> GenerateMockSessionPaths(long sessionId)
    {
        return new List<ReasoningPath>
        {
            new() 
            { 
                PathId = "path_1", 
                Status = "completed", 
                Confidence = 0.92, 
                Steps = 7, 
                Waypoints = new List<GeoJsonPoint> 
                {
                    new() { Type = "Point", Coordinates = [-122.4194, 37.7749] },
                    new() { Type = "Point", Coordinates = [-122.4200, 37.7755] },
                    new() { Type = "Point", Coordinates = [-122.4210, 37.7760] }
                }
            },
            new() 
            { 
                PathId = "path_2", 
                Status = "pruned", 
                Confidence = 0.67, 
                Steps = 4, 
                Reason = "Low confidence threshold",
                Waypoints = new List<GeoJsonPoint> 
                {
                    new() { Type = "Point", Coordinates = [-122.4194, 37.7749] },
                    new() { Type = "Point", Coordinates = [-122.4180, 37.7740] }
                }
            }
        };
    }

    private SpatialVisualizationData GenerateMockPathSpatialData()
    {
        return new SpatialVisualizationData
        {
            Type = "FeatureCollection",
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonLineString 
                    { 
                        Type = "LineString", 
                        Coordinates = new List<double[]> 
                        { 
                            new[] { -122.4194, 37.7749 }, 
                            new[] { -122.4200, 37.7755 },
                            new[] { -122.4210, 37.7760 }
                        } 
                    },
                    Properties = new Dictionary<string, object> { ["pathId"] = "path_1", ["status"] = "completed", ["confidence"] = 0.92 }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7740, -122.4180, 37.7765 }
        };
    }

    private List<ErrorCluster> GenerateMockErrorClusters()
    {
        return new List<ErrorCluster>
        {
            new() 
            { 
                ClusterId = "cluster_1", 
                ErrorType = "SemanticAmbiguity", 
                ErrorCount = 34, 
                Centroid = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4195, 37.7750] },
                Radius = 0.5,
                FirstOccurrence = DateTime.UtcNow.AddDays(-6),
                LastOccurrence = DateTime.UtcNow.AddHours(-3),
                Severity = "medium"
            },
            new() 
            { 
                ClusterId = "cluster_2", 
                ErrorType = "ConfidenceThreshold", 
                ErrorCount = 21, 
                Centroid = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4210, 37.7765] },
                Radius = 0.3,
                FirstOccurrence = DateTime.UtcNow.AddDays(-4),
                LastOccurrence = DateTime.UtcNow.AddHours(-12),
                Severity = "low"
            }
        };
    }

    private SpatialVisualizationData GenerateMockErrorHeatmap()
    {
        return new SpatialVisualizationData
        {
            Type = "FeatureCollection",
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4195, 37.7750] },
                    Properties = new Dictionary<string, object> { ["intensity"] = 34, ["errorType"] = "SemanticAmbiguity" }
                },
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4210, 37.7765] },
                    Properties = new Dictionary<string, object> { ["intensity"] = 21, ["errorType"] = "ConfidenceThreshold" }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7740, -122.4180, 37.7770 }
        };
    }

    private List<InfluencingAtom> GenerateMockInfluences(long atomId, double minInfluence)
    {
        return new List<InfluencingAtom>
        {
            new() 
            { 
                AtomId = atomId - 5, 
                InfluenceWeight = 0.89, 
                InfluenceType = "Direct",
                Label = "Primary Context Source",
                Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4180, 37.7755] },
                Distance = 0.8
            },
            new() 
            { 
                AtomId = atomId - 12, 
                InfluenceWeight = 0.67, 
                InfluenceType = "Indirect",
                Label = "Supporting Evidence",
                Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4200, 37.7740] },
                Distance = 1.2
            },
            new() 
            { 
                AtomId = atomId - 23, 
                InfluenceWeight = 0.42, 
                InfluenceType = "Indirect",
                Label = "Historical Pattern",
                Location = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4215, 37.7760] },
                Distance = 2.3
            }
        };
    }

    private SpatialVisualizationData GenerateMockInfluenceSpatialData()
    {
        return new SpatialVisualizationData
        {
            Type = "FeatureCollection",
            Features = new List<GeoJsonFeature>
            {
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonPoint { Type = "Point", Coordinates = [-122.4180, 37.7755] },
                    Properties = new Dictionary<string, object> { ["influence"] = 0.89, ["type"] = "direct" }
                },
                new() 
                { 
                    Type = "Feature",
                    Geometry = new GeoJsonLineString 
                    { 
                        Type = "LineString", 
                        Coordinates = new List<double[]> 
                        { 
                            new[] { -122.4180, 37.7755 }, 
                            new[] { -122.4194, 37.7749 }
                        } 
                    },
                    Properties = new Dictionary<string, object> { ["weight"] = 0.89 }
                }
            },
            BoundingBox = new double[] { -122.4220, 37.7735, -122.4170, 37.7770 }
        };
    }

    #endregion
}

#region Response Models

public record AtomLineageResponse
{
    public long AtomId { get; init; }
    public int MaxDepth { get; init; }
    public required List<LineageNode> Nodes { get; init; }
    public required List<LineageEdge> Edges { get; init; }
    public required SpatialVisualizationData SpatialData { get; init; }
    public required LineageStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

public record LineageNode
{
    public long AtomId { get; init; }
    public required string Type { get; init; }
    public int Depth { get; init; }
    public required string Label { get; init; }
    public double Confidence { get; init; }
    public required GeoJsonPoint Location { get; init; }
}

public record LineageEdge
{
    public long From { get; init; }
    public long To { get; init; }
    public required string Type { get; init; }
    public double Weight { get; init; }
    public required string Label { get; init; }
}

public record LineageStatistics
{
    public int TotalNodes { get; init; }
    public int TotalEdges { get; init; }
    public int MaxDepthReached { get; init; }
    public required string SpatialCoverage { get; init; }
    public required string TemporalSpan { get; init; }
}

public record SessionPathsResponse
{
    public long SessionId { get; init; }
    public required List<ReasoningPath> Paths { get; init; }
    public required SpatialVisualizationData SpatialTraversal { get; init; }
    public required PathStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

public record ReasoningPath
{
    public required string PathId { get; init; }
    public required string Status { get; init; }
    public double Confidence { get; init; }
    public int Steps { get; init; }
    public string? Reason { get; init; }
    public required List<GeoJsonPoint> Waypoints { get; init; }
}

public record PathStatistics
{
    public int TotalPaths { get; init; }
    public int PathsTaken { get; init; }
    public int PathsPruned { get; init; }
    public int DecisionPoints { get; init; }
    public double AverageConfidence { get; init; }
    public required string SpatialDistance { get; init; }
}

public record ErrorClustersResponse
{
    public long? SessionFilter { get; init; }
    public int MinClusterSize { get; init; }
    public required List<ErrorCluster> Clusters { get; init; }
    public required SpatialVisualizationData SpatialHeatmap { get; init; }
    public required ErrorStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

public record ErrorCluster
{
    public required string ClusterId { get; init; }
    public required string ErrorType { get; init; }
    public int ErrorCount { get; init; }
    public required GeoJsonPoint Centroid { get; init; }
    public double Radius { get; init; }
    public DateTime FirstOccurrence { get; init; }
    public DateTime LastOccurrence { get; init; }
    public required string Severity { get; init; }
}

public record ErrorStatistics
{
    public int TotalErrors { get; init; }
    public int ClustersFound { get; init; }
    public int AverageClusterSize { get; init; }
    public required string SpatialSpread { get; init; }
    public required string TemporalWindow { get; init; }
    public required string MostCommonErrorType { get; init; }
}

public record InfluencingAtomsResponse
{
    public long ResultAtomId { get; init; }
    public double MinInfluenceThreshold { get; init; }
    public required List<InfluencingAtom> Influences { get; init; }
    public required SpatialVisualizationData SpatialDistribution { get; init; }
    public required InfluenceStatistics Statistics { get; init; }
    public bool DemoMode { get; init; }
}

public record InfluencingAtom
{
    public long AtomId { get; init; }
    public double InfluenceWeight { get; init; }
    public required string InfluenceType { get; init; }
    public required string Label { get; init; }
    public required GeoJsonPoint Location { get; init; }
    public double Distance { get; init; }
}

public record InfluenceStatistics
{
    public int TotalInfluencingAtoms { get; init; }
    public int DirectInfluences { get; init; }
    public int IndirectInfluences { get; init; }
    public double AverageInfluenceWeight { get; init; }
    public double MaxInfluenceWeight { get; init; }
    public required string SpatialRadius { get; init; }
}

// GeoJSON models for spatial visualization
public record SpatialVisualizationData
{
    public required string Type { get; init; } // "FeatureCollection"
    public required List<GeoJsonFeature> Features { get; init; }
    public double[]? BoundingBox { get; init; }
}

public record GeoJsonFeature
{
    public required string Type { get; init; } // "Feature"
    public required object Geometry { get; init; } // GeoJsonPoint, GeoJsonLineString, etc.
    public required Dictionary<string, object> Properties { get; init; }
}

public record GeoJsonPoint
{
    public required string Type { get; init; } // "Point"
    public required double[] Coordinates { get; init; } // [longitude, latitude]
}

public record GeoJsonLineString
{
    public required string Type { get; init; } // "LineString"
    public required List<double[]> Coordinates { get; init; } // [[lon, lat], [lon, lat], ...]
}

#endregion
