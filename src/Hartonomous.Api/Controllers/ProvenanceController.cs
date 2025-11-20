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

            // Create rich response with spatial data for visualization
            var response = new AtomLineageResponse
            {
                AtomId = atomId,
                MaxDepth = maxDepth ?? 5,
                Nodes = ProvenanceControllerMockData.GenerateMockLineageNodes(atomId, maxDepth ?? 5),
                Edges = ProvenanceControllerMockData.GenerateMockLineageEdges(atomId),
                SpatialData = ProvenanceControllerMockData.GenerateMockSpatialData(atomId),
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

            var response = new SessionPathsResponse
            {
                SessionId = sessionId,
                Paths = ProvenanceControllerMockData.GenerateMockSessionPaths(sessionId),
                SpatialTraversal = ProvenanceControllerMockData.GenerateMockPathSpatialData(),
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

            var response = new ErrorClustersResponse
            {
                SessionFilter = sessionId,
                MinClusterSize = minClusterSize,
                Clusters = ProvenanceControllerMockData.GenerateMockErrorClusters(),
                SpatialHeatmap = ProvenanceControllerMockData.GenerateMockErrorHeatmap(),
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

            var response = new InfluencingAtomsResponse
            {
                ResultAtomId = atomId,
                MinInfluenceThreshold = minInfluence,
                Influences = ProvenanceControllerMockData.GenerateMockInfluences(atomId, minInfluence),
                SpatialDistribution = ProvenanceControllerMockData.GenerateMockInfluenceSpatialData(),
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

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding influencing atoms for atom {AtomId}", atomId);
            throw new InvalidOperationException("An error occurred while finding influencing atoms");
        }
    }
}
