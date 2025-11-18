using Hartonomous.Api.DTOs.Graph;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Controller for graph analytics and statistics operations.
/// Handles graph statistics, relationship analysis, and graph metrics.
/// </summary>
[ApiController]
[Route("api/v1/graph")]
public class GraphAnalyticsController : ApiControllerBase
{
    private readonly ILogger<GraphAnalyticsController> _logger;
    private readonly IDriver _neo4jDriver;

    public GraphAnalyticsController(
        ILogger<GraphAnalyticsController> logger,
        IDriver neo4jDriver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<GraphStatsResponse>), 200)]
    public async Task<IActionResult> GetGraphStats(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving graph statistics");

            await using var session = _neo4jDriver.AsyncSession();

            // Get node counts by modality
            var nodeStatsQuery = @"
                MATCH (n:Atom)
                RETURN
                    count(n) AS totalNodes,
                    collect(DISTINCT n.modality) AS modalities,
                    collect(DISTINCT {modality: n.modality, count: count(n)}) AS modalityCounts";

            var nodeStatsCursor = await session.RunAsync(nodeStatsQuery);
            var nodeStatsRecord = await nodeStatsCursor.SingleAsync(cancellationToken);

            // Get relationship statistics
            var relationshipStatsQuery = @"
                MATCH ()-[r]-()
                RETURN
                    count(r) AS totalRelationships,
                    collect(DISTINCT type(r)) AS relationshipTypes,
                    collect(DISTINCT {type: type(r), count: count(r)}) AS relationshipTypeCounts";

            var relationshipStatsCursor = await session.RunAsync(relationshipStatsQuery);
            var relationshipStatsRecord = await relationshipStatsCursor.SingleAsync(cancellationToken);

            // Get graph density and connectivity metrics
            var densityQuery = @"
                MATCH (n:Atom)
                OPTIONAL MATCH (n)-[r]-()
                WITH count(DISTINCT n) AS nodeCount, count(DISTINCT r) AS relationshipCount
                RETURN
                    nodeCount,
                    relationshipCount,
                    CASE WHEN nodeCount > 1 THEN toFloat(relationshipCount) / (nodeCount * (nodeCount - 1)) ELSE 0 END AS density";

            var densityCursor = await session.RunAsync(densityQuery);
            var densityRecord = await densityCursor.SingleAsync(cancellationToken);

            // Get component analysis
            var componentQuery = @"
                CALL gds.graph.project('tempGraph', 'Atom', {SIMILAR_TO: {orientation: 'UNDIRECTED'}})
                YIELD graphName
                CALL gds.wcc.stats('tempGraph')
                YIELD componentCount, componentDistribution
                CALL gds.graph.drop('tempGraph')
                YIELD graphName
                RETURN componentCount, componentDistribution";

            var componentCursor = await session.RunAsync(componentQuery);
            var componentRecords = await componentCursor.ToListAsync(cancellationToken);

            var stats = new GraphStatsResponse
            {
                TotalNodes = nodeStatsRecord["totalNodes"].As<long>(),
                TotalRelationships = relationshipStatsRecord["totalRelationships"].As<long>(),
                Modalities = nodeStatsRecord["modalities"].As<List<string>>(),
                ModalityCounts = nodeStatsRecord["modalityCounts"]
                    .As<List<Dictionary<string, object>>>()
                    .ToDictionary(
                        d => d["modality"].As<string>(),
                        d => d["count"].As<long>()
                    ),
                RelationshipTypes = relationshipStatsRecord["relationshipTypes"].As<List<string>>(),
                RelationshipTypeCounts = relationshipStatsRecord["relationshipTypeCounts"]
                    .As<List<Dictionary<string, object>>>()
                    .ToDictionary(
                        d => d["type"].As<string>(),
                        d => d["count"].As<long>()
                    ),
                Density = densityRecord["density"].As<double>(),
                ConnectedComponents = componentRecords.Any()
                    ? componentRecords.First()["componentCount"].As<long>()
                    : 0L,
                ComponentDistribution = componentRecords.Any()
                    ? componentRecords.First()["componentDistribution"].As<Dictionary<string, object>>()
                    : new Dictionary<string, object>(),
                AverageDegree = densityRecord["nodeCount"].As<long>() > 0
                    ? (double)densityRecord["relationshipCount"].As<long>() * 2 / densityRecord["nodeCount"].As<long>()
                    : 0.0
            };

            _logger.LogInformation("Retrieved graph stats: {Nodes} nodes, {Relationships} relationships",
                stats.TotalNodes, stats.TotalRelationships);

            return Ok(Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve graph statistics");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Failed to retrieve graph statistics", ex.Message);
            return StatusCode(500, Failure<GraphStatsResponse>(new[] { error }));
        }
    }

    [HttpPost("relationship-analysis")]
    [ProducesResponseType(typeof(ApiResponse<RelationshipAnalysisResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<RelationshipAnalysisResponse>), 400)]
    public async Task<IActionResult> AnalyzeRelationships(
        [FromBody] RelationshipAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(Failure<RelationshipAnalysisResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "Request body is required", "RelationshipAnalysisRequest cannot be null")
            }));
        }

        try
        {
            _logger.LogInformation("Analyzing relationships for modality {Modality}", request.ModalityFilter);

            await using var session = _neo4jDriver.AsyncSession();

            // Build query with optional modality filter
            var modalityFilter = !string.IsNullOrWhiteSpace(request.ModalityFilter)
                ? $"WHERE n1.modality = '{request.ModalityFilter}' AND n2.modality = '{request.ModalityFilter}'"
                : "";

            var analysisQuery = $@"
                MATCH (n1:Atom)-[r]->(n2:Atom)
                {modalityFilter}
                RETURN
                    type(r) AS relationshipType,
                    count(r) AS count,
                    avg(r.weight) AS avgWeight,
                    min(r.weight) AS minWeight,
                    max(r.weight) AS maxWeight,
                    stDev(r.weight) AS weightStdDev,
                    collect(DISTINCT n1.modality) AS sourceModalities,
                    collect(DISTINCT n2.modality) AS targetModalities
                ORDER BY count DESC
                LIMIT {request.TopRelationships ?? 20}";

            var cursor = await session.RunAsync(analysisQuery);
            var records = await cursor.ToListAsync(cancellationToken);

            var relationshipStats = records.Select(record => new RelationshipStats
            {
                RelationshipType = record["relationshipType"].As<string>(),
                Count = record["count"].As<long>(),
                AverageWeight = record["avgWeight"].As<double?>(),
                MinWeight = record["minWeight"].As<double?>(),
                MaxWeight = record["maxWeight"].As<double?>(),
                WeightStdDev = record["weightStdDev"].As<double?>(),
                SourceModalities = record["sourceModalities"].As<List<string>>(),
                TargetModalities = record["targetModalities"].As<List<string>>()
            }).ToList();

            // Calculate cross-modality relationships if no modality filter
            var crossModalityStats = new List<CrossModalityStats>();
            if (string.IsNullOrWhiteSpace(request.ModalityFilter))
            {
                var crossModalityQuery = @"
                    MATCH (n1:Atom)-[r]->(n2:Atom)
                    WHERE n1.modality <> n2.modality
                    RETURN
                        n1.modality AS sourceModality,
                        n2.modality AS targetModality,
                        type(r) AS relationshipType,
                        count(r) AS count
                    ORDER BY count DESC
                    LIMIT 20";

                var crossModalityCursor = await session.RunAsync(crossModalityQuery);
                var crossModalityRecords = await crossModalityCursor.ToListAsync(cancellationToken);

                crossModalityStats = crossModalityRecords.Select(record => new CrossModalityStats
                {
                    SourceModality = record["sourceModality"].As<string>(),
                    TargetModality = record["targetModality"].As<string>(),
                    RelationshipType = record["relationshipType"].As<string>(),
                    Count = record["count"].As<long>()
                }).ToList();
            }

            return Ok(Success(new RelationshipAnalysisResponse
            {
                ModalityFilter = request.ModalityFilter,
                RelationshipStats = relationshipStats,
                CrossModalityStats = crossModalityStats,
                TotalRelationshipsAnalyzed = relationshipStats.Sum(s => s.Count)
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Relationship analysis failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Relationship analysis failed", ex.Message);
            return StatusCode(500, Failure<RelationshipAnalysisResponse>(new[] { error }));
        }
    }

    [HttpPost("centrality")]
    [ProducesResponseType(typeof(ApiResponse<CentralityAnalysisResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<CentralityAnalysisResponse>), 400)]
    public async Task<IActionResult> AnalyzeCentrality(
        [FromBody] CentralityAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(Failure<CentralityAnalysisResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "Request body is required", "CentralityAnalysisRequest cannot be null")
            }));
        }

        try
        {
            _logger.LogInformation("Analyzing centrality using {Algorithm}", request.Algorithm);

            await using var session = _neo4jDriver.AsyncSession();

            // Project graph for analysis
            var projectQuery = @"
                CALL gds.graph.project(
                    'centralityGraph',
                    'Atom',
                    {SIMILAR_TO: {orientation: 'UNDIRECTED', properties: 'weight'}}
                )";

            await session.RunAsync(projectQuery);

            // Run centrality algorithm
            string centralityQuery = request.Algorithm.ToLower() switch
            {
                "degree" => @"
                    CALL gds.degree.stream('centralityGraph')
                    YIELD nodeId, score
                    RETURN gds.util.asNode(nodeId).atomId AS atomId, score
                    ORDER BY score DESC
                    LIMIT $limit",

                "betweenness" => @"
                    CALL gds.betweenness.stream('centralityGraph')
                    YIELD nodeId, score
                    RETURN gds.util.asNode(nodeId).atomId AS atomId, score
                    ORDER BY score DESC
                    LIMIT $limit",

                "closeness" => @"
                    CALL gds.closeness.stream('centralityGraph')
                    YIELD nodeId, score
                    RETURN gds.util.asNode(nodeId).atomId AS atomId, score
                    ORDER BY score DESC
                    LIMIT $limit",

                "eigenvector" => @"
                    CALL gds.eigenvector.stream('centralityGraph')
                    YIELD nodeId, score
                    RETURN gds.util.asNode(nodeId).atomId AS atomId, score
                    ORDER BY score DESC
                    LIMIT $limit",

                _ => throw new ArgumentException($"Unsupported centrality algorithm: {request.Algorithm}")
            };

            var centralityCursor = await session.RunAsync(centralityQuery, new { limit = request.TopNodes ?? 100 });
            var centralityRecords = await centralityCursor.ToListAsync(cancellationToken);

            // Clean up graph projection
            await session.RunAsync("CALL gds.graph.drop('centralityGraph')");

            var centralityScores = centralityRecords.Select(record => new CentralityScore
            {
                AtomId = record["atomId"].As<long>(),
                Score = record["score"].As<double>(),
                Rank = centralityRecords.IndexOf(record) + 1
            }).ToList();

            // Get additional node information
            if (centralityScores.Any())
            {
                var atomIds = centralityScores.Select(s => s.AtomId).ToArray();
                var nodeInfoQuery = @"
                    MATCH (n:Atom)
                    WHERE n.atomId IN $atomIds
                    RETURN n.atomId AS atomId, n.modality AS modality, n.canonicalText AS canonicalText";

                var nodeInfoCursor = await session.RunAsync(nodeInfoQuery, new { atomIds });
                var nodeInfoRecords = await nodeInfoCursor.ToListAsync(cancellationToken);

                var nodeInfoMap = nodeInfoRecords.ToDictionary(
                    r => r["atomId"].As<long>(),
                    r => new
                    {
                        Modality = r["modality"].As<string>(),
                        CanonicalText = r["canonicalText"].As<string?>()
                    }
                );

                foreach (var score in centralityScores)
                {
                    if (nodeInfoMap.TryGetValue(score.AtomId, out var info))
                    {
                        score.Modality = info.Modality;
                        score.CanonicalText = info.CanonicalText;
                    }
                }
            }

            return Ok(Success(new CentralityAnalysisResponse
            {
                Algorithm = request.Algorithm,
                CentralityScores = centralityScores,
                TotalNodesAnalyzed = centralityScores.Count
            }));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid centrality algorithm");
            var error = ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidFieldValue, "Invalid centrality algorithm", ex.Message);
            return BadRequest(Failure<CentralityAnalysisResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Centrality analysis failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Centrality analysis failed", ex.Message);
            return StatusCode(500, Failure<CentralityAnalysisResponse>(new[] { error }));
        }
    }

    [HttpPost("create-relationship")]
    [ProducesResponseType(typeof(ApiResponse<CreateRelationshipResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<CreateRelationshipResponse>), 400)]
    public async Task<IActionResult> CreateRelationship(
        [FromBody] CreateRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.FromAtomId == 0 || request.ToAtomId == 0)
        {
            return BadRequest(Failure<CreateRelationshipResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest,
                    "FromAtomId and ToAtomId are required", "Both atom IDs must be provided and non-zero")
            }));
        }

        if (string.IsNullOrWhiteSpace(request.RelationshipType))
        {
            return BadRequest(Failure<CreateRelationshipResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest,
                    "RelationshipType is required", "RelationshipType cannot be empty")
            }));
        }

        try
        {
            _logger.LogInformation("Creating relationship {Type} between atoms {From} and {To}",
                request.RelationshipType, request.FromAtomId, request.ToAtomId);

            await using var session = _neo4jDriver.AsyncSession();

            // Verify both atoms exist
            var verifyQuery = @"
                MATCH (from:Atom {atomId: $fromId}), (to:Atom {atomId: $toId})
                RETURN count(from) > 0 AND count(to) > 0 AS atomsExist";

            var verifyCursor = await session.RunAsync(verifyQuery, new
            {
                fromId = request.FromAtomId,
                toId = request.ToAtomId
            });

            var verifyRecord = await verifyCursor.SingleAsync(cancellationToken);
            if (!verifyRecord["atomsExist"].As<bool>())
            {
                var error = ErrorDetailFactory.Create(ErrorCodes.NotFound.Resource,
                    "One or both atoms not found", $"Atoms {request.FromAtomId} and/or {request.ToAtomId} do not exist");
                return NotFound(Failure<CreateRelationshipResponse>(new[] { error }));
            }

            // Create the relationship
            var createQuery = $@"
                MATCH (from:Atom {{atomId: $fromId}}), (to:Atom {{atomId: $toId}})
                CREATE (from)-[r:{request.RelationshipType}]->(to)
                SET r.createdAt = datetime()
                {(request.Weight.HasValue ? "SET r.weight = $weight" : "")}
                {(request.Properties != null && request.Properties.Any() ?
                    string.Join(" ", request.Properties.Select(p => $"SET r.{p.Key} = ${p.Key}")) : "")}
                RETURN r";

            var parameters = new Dictionary<string, object>
            {
                ["fromId"] = request.FromAtomId,
                ["toId"] = request.ToAtomId
            };

            if (request.Weight.HasValue)
                parameters["weight"] = request.Weight.Value;

            if (request.Properties != null)
            {
                foreach (var prop in request.Properties)
                {
                    parameters[prop.Key] = prop.Value;
                }
            }

            var createCursor = await session.RunAsync(createQuery, parameters);
            var createRecord = await createCursor.SingleAsync(cancellationToken);

            var relationship = createRecord["r"].As<IRelationship>();

            return Ok(Success(new CreateRelationshipResponse
            {
                FromAtomId = request.FromAtomId,
                ToAtomId = request.ToAtomId,
                RelationshipType = request.RelationshipType,
                Success = true
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create relationship");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Failed to create relationship", ex.Message);
            return StatusCode(500, Failure<CreateRelationshipResponse>(new[] { error }));
        }
    }
}
