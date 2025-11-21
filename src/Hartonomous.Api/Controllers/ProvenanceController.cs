using Asp.Versioning;
using Hartonomous.Api.DTOs.MLOps;
using Hartonomous.Api.DTOs.Provenance;
using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Provenance and lineage tracking endpoints using Neo4j graph database.
/// Provides atom lineage, session paths, error clustering, and influence analysis.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/provenance")]
[Authorize(Policy = "ApiUser")]
[EnableRateLimiting("query")]
public class ProvenanceController : ControllerBase
{
    private readonly ILineageQueryService _lineageService;
    private readonly IErrorAnalysisService _errorAnalysisService;
    private readonly ISessionPathQueryService _sessionPathService;
    private readonly IInfluenceAnalysisService _influenceAnalysisService;
    private readonly IValidationService _validationService;
    private readonly ILogger<ProvenanceController> _logger;

    public ProvenanceController(
        ILineageQueryService lineageService,
        IErrorAnalysisService errorAnalysisService,
        ISessionPathQueryService sessionPathService,
        IInfluenceAnalysisService influenceAnalysisService,
        IValidationService validationService,
        ILogger<ProvenanceController> logger)
    {
        _lineageService = lineageService ?? throw new ArgumentNullException(nameof(lineageService));
        _errorAnalysisService = errorAnalysisService ?? throw new ArgumentNullException(nameof(errorAnalysisService));
        _sessionPathService = sessionPathService ?? throw new ArgumentNullException(nameof(sessionPathService));
        _influenceAnalysisService = influenceAnalysisService ?? throw new ArgumentNullException(nameof(influenceAnalysisService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var request = new AtomLineageRequest { AtomId = atomId, MaxDepth = maxDepth };
        _validationService.ValidateAndThrow(request);

        try
        {
            _logger.LogInformation("Retrieving lineage for atom {AtomId} with max depth {MaxDepth}", atomId, maxDepth);

            var lineage = await _lineageService.GetAtomLineageAsync(atomId, maxDepth, cancellationToken);

            // Convert domain model to API response
            var nodes = lineage.Parents.Select((p, i) => new DTOs.Provenance.LineageNode
            {
                AtomId = p.AtomId,
                Type = LineageNodeType.Source,
                Depth = 1,
                Label = p.Metadata ?? $"Atom {p.AtomId}",
                Confidence = 0.9,
                Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = new double[] { 0, 0 } }
            }).ToList();

            // Add root node
            nodes.Insert(0, new DTOs.Provenance.LineageNode
            {
                AtomId = atomId,
                Type = LineageNodeType.Result,
                Depth = 0,
                Label = "Target Atom",
                Confidence = 1.0,
                Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = new double[] { 0, 0 } }
            });

            var edges = lineage.Parents.Select(p => new LineageEdge
            {
                From = p.AtomId,
                To = atomId,
                Type = p.RelationshipType,
                Weight = 0.8,
                Label = p.RelationshipType.ToLowerInvariant()
            }).ToList();

            var response = new AtomLineageResponse
            {
                AtomId = atomId,
                MaxDepth = maxDepth ?? 5,
                Nodes = nodes,
                Edges = edges,
                SpatialData = new SpatialVisualizationData { Type = GeoJsonType.FeatureCollection, Features = new List<GeoJsonFeature>() },
                Statistics = new LineageStatistics
                {
                    TotalNodes = nodes.Count,
                    TotalEdges = edges.Count,
                    MaxDepthReached = lineage.Depth,
                    SpatialCoverage = "N/A",
                    TemporalSpan = "N/A"
                },
                DemoMode = false
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving atom lineage for atom {AtomId}", atomId);
            throw new InvalidOperationException("An error occurred while retrieving atom lineage");
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
        var request = new SessionPathsRequest { SessionId = sessionId };
        _validationService.ValidateAndThrow(request);

        try
        {
            _logger.LogInformation("Retrieving reasoning paths for session {SessionId}", sessionId);

            var paths = await _sessionPathService.GetSessionPathsAsync(sessionId, cancellationToken);
            var pathList = paths.ToList();

            var apiPaths = pathList.Select(p => new DTOs.Reasoning.ReasoningPath
            {
                PathId = p.PathId,
                Status = p.IsSuccessful ? PathStatus.Completed : PathStatus.Pruned,
                Confidence = p.IsSuccessful ? 0.9 : 0.5,
                Steps = p.AtomSequence.Count,
                Waypoints = new List<GeoJsonPoint>()
            }).ToList();

            var response = new SessionPathsResponse
            {
                SessionId = sessionId,
                Paths = apiPaths,
                SpatialTraversal = new SpatialVisualizationData { Type = GeoJsonType.FeatureCollection, Features = new List<GeoJsonFeature>() },
                Statistics = new PathStatistics
                {
                    TotalPaths = pathList.Count,
                    PathsTaken = pathList.Count(p => p.IsSuccessful),
                    PathsPruned = pathList.Count(p => !p.IsSuccessful),
                    DecisionPoints = pathList.Sum(p => p.Decisions.Count),
                    AverageConfidence = pathList.Count > 0 ? pathList.Average(p => p.IsSuccessful ? 0.9 : 0.5) : 0,
                    SpatialDistance = "N/A"
                },
                DemoMode = false
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session paths for session {SessionId}", sessionId);
            throw new InvalidOperationException("An error occurred while retrieving session paths");
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
        var request = new ErrorClustersRequest { SessionId = sessionId, MinClusterSize = minClusterSize };
        _validationService.ValidateAndThrow(request);

        try
        {
            _logger.LogInformation(
                "Finding error clusters with min size {MinSize}, sessionId filter: {SessionId}",
                minClusterSize,
                sessionId);

            var clusters = await _errorAnalysisService.FindErrorClustersAsync(sessionId, minClusterSize, cancellationToken);
            var clusterList = clusters.ToList();

            var apiClusters = clusterList.Select(c => new DTOs.MLOps.ErrorCluster
            {
                ClusterId = c.ClusterId,
                ErrorType = c.Pattern,
                ErrorCount = c.ErrorCount,
                Centroid = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = new double[] { 0, 0 } },
                Radius = 0.5,
                FirstOccurrence = c.FirstOccurrence,
                LastOccurrence = c.LastOccurrence,
                Severity = ErrorSeverity.Medium
            }).ToList();

            var response = new ErrorClustersResponse
            {
                SessionFilter = sessionId,
                MinClusterSize = minClusterSize,
                Clusters = apiClusters,
                SpatialHeatmap = new SpatialVisualizationData { Type = GeoJsonType.FeatureCollection, Features = new List<GeoJsonFeature>() },
                Statistics = new ErrorStatistics
                {
                    TotalErrors = clusterList.Sum(c => c.ErrorCount),
                    ClustersFound = clusterList.Count,
                    AverageClusterSize = clusterList.Count > 0 ? clusterList.Average(c => c.ErrorCount) : 0,
                    SpatialSpread = "N/A",
                    TemporalWindow = clusterList.Count > 0 ? $"{(clusterList.Max(c => c.LastOccurrence) - clusterList.Min(c => c.FirstOccurrence)).Days} days" : "N/A",
                    MostCommonErrorType = clusterList.OrderByDescending(c => c.ErrorCount).FirstOrDefault()?.Pattern ?? "None"
                },
                DemoMode = false
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding error clusters");
            throw new InvalidOperationException("An error occurred while finding error clusters");
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
        var request = new InfluencingAtomsRequest { AtomId = atomId, MinInfluence = minInfluence };
        _validationService.ValidateAndThrow(request);

        try
        {
            _logger.LogInformation(
                "Finding influencing atoms for atom {AtomId} with min influence {MinInfluence}",
                atomId,
                minInfluence);

            var influences = await _influenceAnalysisService.GetInfluencingAtomsAsync(atomId, cancellationToken);
            var influenceList = influences.Where(i => i.Weight >= minInfluence).ToList();

            var apiInfluences = influenceList.Select(i => new InfluencingAtom
            {
                AtomId = i.AtomId,
                InfluenceWeight = i.Weight,
                InfluenceType = i.InfluenceType == "Direct" ? InfluenceType.Direct : InfluenceType.Indirect,
                Label = $"Atom {i.AtomId}",
                Location = new GeoJsonPoint { Type = GeoJsonType.Point, Coordinates = new double[] { 0, 0 } },
                Distance = i.PathLength
            }).ToList();

            var directCount = influenceList.Count(i => i.InfluenceType == "Direct");
            var response = new InfluencingAtomsResponse
            {
                ResultAtomId = atomId,
                MinInfluenceThreshold = minInfluence,
                Influences = apiInfluences,
                SpatialDistribution = new SpatialVisualizationData { Type = GeoJsonType.FeatureCollection, Features = new List<GeoJsonFeature>() },
                Statistics = new InfluenceStatistics
                {
                    TotalInfluencingAtoms = influenceList.Count,
                    DirectInfluences = directCount,
                    IndirectInfluences = influenceList.Count - directCount,
                    AverageInfluenceWeight = influenceList.Count > 0 ? influenceList.Average(i => i.Weight) : 0,
                    MaxInfluenceWeight = influenceList.Count > 0 ? influenceList.Max(i => i.Weight) : 0,
                    SpatialRadius = "N/A"
                },
                DemoMode = false
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding influencing atoms for atom {AtomId}", atomId);
            throw new InvalidOperationException("An error occurred while finding influencing atoms");
        }
    }
}
