using Hartonomous.Api.DTOs.Graph;
using Hartonomous.Core.Interfaces;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Diagnostics;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Controller for graph query operations using Neo4j.
/// Handles Cypher queries, relationship traversal, and concept exploration.
/// </summary>
[ApiController]
[Route("api/v1/graph")]
public class GraphQueryController : ApiControllerBase
{
    private readonly ILogger<GraphQueryController> _logger;
    private readonly IDriver _neo4jDriver;
    private readonly IInferenceService _inferenceService;
    private readonly IEmbeddingService _embeddingService;

    public GraphQueryController(
        ILogger<GraphQueryController> logger,
        IDriver neo4jDriver,
        IInferenceService inferenceService,
        IEmbeddingService embeddingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
    }

    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<GraphQueryResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<GraphQueryResponse>), 400)]
    public async Task<IActionResult> ExecuteQuery(
        [FromBody] GraphQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CypherQuery))
        {
            return BadRequest(Failure<GraphQueryResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "CypherQuery is required", "CypherQuery cannot be empty")
            }));
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await using var session = _neo4jDriver.AsyncSession();
            var cursor = await session.RunAsync(request.CypherQuery, request.Parameters ?? new Dictionary<string, object>());
            var records = await cursor.ToListAsync(cancellationToken);

            var results = records.Select(record =>
            {
                var dict = new Dictionary<string, object>();
                foreach (var key in record.Keys)
                {
                    dict[key] = ConvertNeo4jValue(record[key]);
                }
                return dict;
            }).ToList();

            _logger.LogInformation("Executed Cypher query: {Query}, returned {Count} records",
                request.CypherQuery, results.Count);

            stopwatch.Stop();

            return Ok(Success(new GraphQueryResponse
            {
                Results = results,
                ResultCount = results.Count,
                ExecutionTime = stopwatch.Elapsed,
                Query = request.CypherQuery
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph query failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Graph query failed", ex.Message);
            return StatusCode(500, Failure<GraphQueryResponse>(new[] { error }));
        }
    }

    [HttpPost("related")]
    [ProducesResponseType(typeof(ApiResponse<FindRelatedAtomsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<FindRelatedAtomsResponse>), 400)]
    public async Task<IActionResult> FindRelatedAtoms(
        [FromBody] FindRelatedAtomsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(Failure<FindRelatedAtomsResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "Request body is required", "FindRelatedAtomsRequest cannot be null")
            }));
        }

        try
        {
            // Build Cypher query for finding related atoms
            var relationshipFilter = !string.IsNullOrWhiteSpace(request.RelationshipType)
                ? $":{request.RelationshipType}"
                : "";

            var similarityFilter = request.MinSimilarity.HasValue
                ? $" AND r.similarity >= {request.MinSimilarity.Value}"
                : "";

            var cypherQuery = $@"
                MATCH path = (start:Atom {{atomId: $atomId}})-[r{relationshipFilter}*1..{request.MaxDepth}]-(related:Atom)
                WHERE 1=1 {similarityFilter}
                RETURN DISTINCT
                    related.atomId AS atomId,
                    related.modality AS modality,
                    related.canonicalText AS canonicalText,
                    type(relationships(path)[0]) AS relationshipType,
                    COALESCE(relationships(path)[0].similarity, 0.0) AS similarity,
                    length(path) AS depth,
                    [n IN nodes(path) | n.atomId] AS pathNodes
                LIMIT {request.Limit}";

            _logger.LogInformation("Finding related atoms for AtomId {AtomId} with depth {MaxDepth}",
                request.AtomId, request.MaxDepth);

            await using var session = _neo4jDriver.AsyncSession();
            var parameters = new { atomId = request.AtomId };
            var cursor = await session.RunAsync(cypherQuery, parameters);
            var records = await cursor.ToListAsync(cancellationToken);

            var relatedAtoms = records.Select(record => new RelatedAtomEntry
            {
                AtomId = record["atomId"].As<long>(),
                Modality = record["modality"].As<string>(),
                CanonicalText = record["canonicalText"].As<string?>(),
                RelationshipType = record["relationshipType"].As<string>(),
                Similarity = record["similarity"].As<double>(),
                Depth = record["depth"].As<int>(),
                PathDescription = record["pathDescription"].As<List<string>>()
            }).ToList();

            return Ok(Success(new FindRelatedAtomsResponse
            {
                SourceAtomId = request.AtomId,
                RelatedAtoms = relatedAtoms,
                TotalPaths = relatedAtoms.Count
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find related atoms");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Failed to find related atoms", ex.Message);
            return StatusCode(500, Failure<FindRelatedAtomsResponse>(new[] { error }));
        }
    }

    [HttpPost("traverse")]
    [ProducesResponseType(typeof(ApiResponse<TraverseGraphResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<TraverseGraphResponse>), 400)]
    public async Task<IActionResult> TraverseGraph(
        [FromBody] TraverseGraphRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.AllowedRelationships == null || request.AllowedRelationships.Count == 0)
        {
            return BadRequest(Failure<TraverseGraphResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest,
                    "At least one allowed relationship type is required", "AllowedRelationships cannot be empty")
            }));
        }

        try
        {
            var relationshipTypes = string.Join("|", request.AllowedRelationships.Select(r => $":{r}"));

            string cypherQuery;
            if (request.EndAtomId.HasValue)
            {
                // Find path between two specific atoms
                cypherQuery = request.TraversalStrategy.ToLower() switch
                {
                    "shortest_path" => $@"
                        MATCH path = shortestPath((start:Atom {{atomId: $startId}})-[{relationshipTypes}*1..{request.MaxDepth}]-(end:Atom {{atomId: $endId}}))
                        RETURN path",

                    "all_paths" => $@"
                        MATCH path = (start:Atom {{atomId: $startId}})-[{relationshipTypes}*1..{request.MaxDepth}]-(end:Atom {{atomId: $endId}})
                        RETURN path
                        LIMIT 100",

                    _ => throw new ArgumentException($"Unknown traversal strategy: {request.TraversalStrategy}")
                };
            }
            else
            {
                // Explore from start atom
                cypherQuery = $@"
                    MATCH path = (start:Atom {{atomId: $startId}})-[{relationshipTypes}*1..{request.MaxDepth}]-(n:Atom)
                    RETURN path
                    LIMIT 100";
            }

            _logger.LogInformation("Traversing graph from {StartId} to {EndId} using {Strategy}",
                request.StartAtomId, request.EndAtomId, request.TraversalStrategy);

            await using var session = _neo4jDriver.AsyncSession();
            var parameters = new
            {
                startId = request.StartAtomId,
                endId = request.EndAtomId
            };
            var cursor = await session.RunAsync(cypherQuery, parameters);
            var records = await cursor.ToListAsync(cancellationToken);

            var paths = records.Select(record =>
            {
                var path = record["path"].As<IPath>();
                return new GraphPath
                {
                    Nodes = path.Nodes.Select(n => new GraphNode
                    {
                        AtomId = n.Properties.GetValueOrDefault("atomId", 0L).As<long>(),
                        Modality = n.Properties.GetValueOrDefault("modality", "unknown").As<string>(),
                        CanonicalText = n.Properties.ContainsKey("canonicalText") ? n.Properties["canonicalText"].As<string?>() : null,
                        Properties = n.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                    }).ToList(),
                    Relationships = path.Relationships.Select(r =>
                    {
                        // Get the actual nodes to extract atomId from properties
                        var startNode = path.Nodes.FirstOrDefault(n => n.ElementId == r.StartNodeElementId);
                        var endNode = path.Nodes.FirstOrDefault(n => n.ElementId == r.EndNodeElementId);

                        return new GraphRelationship
                        {
                            Type = r.Type,
                            FromAtomId = startNode?.Properties.GetValueOrDefault("atomId", 0L).As<long>() ?? 0L,
                            ToAtomId = endNode?.Properties.GetValueOrDefault("atomId", 0L).As<long>() ?? 0L,
                            Weight = r.Properties.ContainsKey("weight") ? r.Properties["weight"].As<double?>() : null,
                            Properties = r.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                        };
                    }).ToList(),
                    PathLength = path.Relationships.Count,
                    TotalWeight = path.Relationships
                        .Where(r => r.Properties.ContainsKey("weight"))
                        .Sum(r => r.Properties["weight"].As<double>())
                };
            }).ToList();

            return Ok(Success(new TraverseGraphResponse
            {
                StartAtomId = request.StartAtomId,
                EndAtomId = request.EndAtomId,
                Paths = paths,
                TotalPathsFound = paths.Count
            }));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid traversal request");
            var error = ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidFieldValue, "Invalid traversal strategy", ex.Message);
            return BadRequest(Failure<TraverseGraphResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph traversal failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Graph traversal failed", ex.Message);
            return StatusCode(500, Failure<TraverseGraphResponse>(new[] { error }));
        }
    }

    [HttpPost("explore")]
    [ProducesResponseType(typeof(ApiResponse<ExploreConceptResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ExploreConceptResponse>), 400)]
    public async Task<IActionResult> ExploreConcept(
        [FromBody] ExploreConceptRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ConceptText))
        {
            return BadRequest(Failure<ExploreConceptResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "ConceptText is required", "ConceptText cannot be empty")
            }));
        }

        try
        {
            _logger.LogInformation("Exploring concept: {Concept}", request.ConceptText);

            // Step 1: Embed concept text to get query vector
            var topK = request.TopK > 0 ? request.TopK : 50;
            var conceptEmbedding = await _inferenceService.SemanticSearchAsync(
                await GetEmbeddingForTextAsync(request.ConceptText, cancellationToken),
                topK: topK,
                cancellationToken
            );

            // Step 2: Get Neo4j graph neighborhood for discovered atoms
            var atomIds = conceptEmbedding.Select(e => e.Embedding.AtomEmbeddingId).ToList();

            await using var session = _neo4jDriver.AsyncSession();

            // Query Neo4j for graph structure around semantic matches
            var minSimilarity = request.MinSimilarity ?? 0.7;
            var graphQuery = @"
                UNWIND $atomIds AS atomId
                MATCH (n:Atom {atomId: atomId})
                OPTIONAL MATCH (n)-[r]-(neighbor:Atom)
                WHERE neighbor.similarity >= $minSimilarity
                RETURN
                    n.atomId AS atomId,
                    n.modality AS modality,
                    n.content AS content,
                    collect(DISTINCT {
                        relType: type(r),
                        neighborId: neighbor.atomId,
                        neighborModality: neighbor.modality,
                        neighborContent: neighbor.content,
                        weight: r.weight
                    }) AS relationships
                LIMIT $limit";

            var cursor = await session.RunAsync(graphQuery, new
            {
                atomIds = atomIds.Select(id => (long)id).ToArray(),
                minSimilarity = minSimilarity,
                limit = topK
            });

            var records = await cursor.ToListAsync(cancellationToken);

            var nodes = new List<ConceptNode>();
            var relationships = new List<ConceptRelationship>();
            var modalityBreakdown = new Dictionary<string, int>();

            foreach (var record in records)
            {
                var atomId = record["atomId"].As<long>();
                var modality = record["modality"].As<string>() ?? "unknown";
                var content = record["content"].As<string>() ?? "";

                nodes.Add(new ConceptNode
                {
                    AtomId = atomId,
                    Modality = modality,
                    CanonicalText = content.Length > 100 ? content[..100] : content,
                    Similarity = conceptEmbedding.FirstOrDefault(e => e.Embedding.AtomEmbeddingId == atomId)?.CosineDistance ?? 0.0
                });

                modalityBreakdown[modality] = modalityBreakdown.GetValueOrDefault(modality, 0) + 1;

                var rels = record["relationships"].As<List<Dictionary<string, object>>>();
                foreach (var rel in rels)
                {
                    if (rel["neighborId"] is long neighborId && rel["relType"] is string relType)
                    {
                        relationships.Add(new ConceptRelationship
                        {
                            FromAtomId = atomId,
                            ToAtomId = neighborId,
                            Type = relType,
                            Strength = rel.ContainsKey("weight") ? Convert.ToDouble(rel["weight"]) : 1.0
                        });
                    }
                }
            }

            return Ok(Success(new ExploreConceptResponse
            {
                ConceptText = request.ConceptText,
                Nodes = nodes,
                Relationships = relationships,
                ModalityBreakdown = modalityBreakdown
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Concept exploration failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Concept exploration failed", ex.Message);
            return StatusCode(500, Failure<ExploreConceptResponse>(new[] { error }));
        }
    }

    private async Task<float[]> GetEmbeddingForTextAsync(string text, CancellationToken cancellationToken)
    {
        return await _embeddingService.EmbedTextAsync(text, cancellationToken);
    }

    private static object ConvertNeo4jValue(object value)
    {
        return value switch
        {
            INode node => new Dictionary<string, object>
            {
                ["id"] = node.ElementId,
                ["labels"] = node.Labels.ToList(),
                ["properties"] = node.Properties
            },
            IRelationship rel => new Dictionary<string, object>
            {
                ["id"] = rel.ElementId,
                ["type"] = rel.Type,
                ["startNodeId"] = rel.StartNodeElementId,
                ["endNodeId"] = rel.EndNodeElementId,
                ["properties"] = rel.Properties
            },
            IPath path => new Dictionary<string, object>
            {
                ["nodes"] = path.Nodes.Select(n => ConvertNeo4jValue(n)).ToList(),
                ["relationships"] = path.Relationships.Select(r => ConvertNeo4jValue(r)).ToList()
            },
            IList<object> list => list.Select(ConvertNeo4jValue).ToList(),
            IDictionary<string, object> dict => dict.ToDictionary(kvp => kvp.Key, kvp => ConvertNeo4jValue(kvp.Value)),
            _ => value
        };
    }
}
