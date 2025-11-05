using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Graph;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Diagnostics;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GraphController : ControllerBase
{
    private readonly ILogger<GraphController> _logger;
    private readonly IDriver _neo4jDriver;
    private readonly IInferenceService _inferenceService;

    public GraphController(
        ILogger<GraphController> logger, 
        IDriver neo4jDriver,
        IInferenceService inferenceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
    }

    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<GraphQueryResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ExecuteQuery(
        [FromBody] GraphQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CypherQuery))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "CypherQuery is required"));
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

            return Ok(ApiResponse<GraphQueryResponse>.Ok(new GraphQueryResponse
            {
                Results = results,
                ResultCount = results.Count,
                ExecutionTime = stopwatch.Elapsed,
                Query = request.CypherQuery
            }, new ApiMetadata
            {
                TotalCount = results.Count,
                Extra = new Dictionary<string, object>
                {
                    ["executionTimeMs"] = stopwatch.ElapsedMilliseconds
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph query failed");
            return StatusCode(500, ApiResponse<object>.Fail("QUERY_FAILED", ex.Message));
        }
    }

    [HttpPost("related")]
    [ProducesResponseType(typeof(ApiResponse<FindRelatedAtomsResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> FindRelatedAtoms(
        [FromBody] FindRelatedAtomsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Request body is required"));
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

            return Ok(ApiResponse<FindRelatedAtomsResponse>.Ok(new FindRelatedAtomsResponse
            {
                SourceAtomId = request.AtomId,
                RelatedAtoms = relatedAtoms,
                TotalPaths = relatedAtoms.Count
            }, new ApiMetadata
            {
                TotalCount = relatedAtoms.Count,
                Extra = new Dictionary<string, object>
                {
                    ["maxDepth"] = request.MaxDepth,
                    ["relationshipType"] = request.RelationshipType ?? "any"
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find related atoms");
            return StatusCode(500, ApiResponse<object>.Fail("TRAVERSAL_FAILED", ex.Message));
        }
    }

    [HttpPost("traverse")]
    [ProducesResponseType(typeof(ApiResponse<TraverseGraphResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> TraverseGraph(
        [FromBody] TraverseGraphRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.AllowedRelationships == null || request.AllowedRelationships.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "At least one allowed relationship type is required"));
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

            return Ok(ApiResponse<TraverseGraphResponse>.Ok(new TraverseGraphResponse
            {
                StartAtomId = request.StartAtomId,
                EndAtomId = request.EndAtomId,
                Paths = paths,
                TotalPathsFound = paths.Count
            }, new ApiMetadata
            {
                TotalCount = paths.Count,
                Extra = new Dictionary<string, object>
                {
                    ["strategy"] = request.TraversalStrategy,
                    ["maxDepth"] = request.MaxDepth
                }
            }));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid traversal request");
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph traversal failed");
            return StatusCode(500, ApiResponse<object>.Fail("TRAVERSAL_FAILED", ex.Message));
        }
    }

    [HttpPost("explore")]
    [ProducesResponseType(typeof(ApiResponse<ExploreConceptResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ExploreConcept(
        [FromBody] ExploreConceptRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ConceptText))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "ConceptText is required"));
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

            return Ok(ApiResponse<ExploreConceptResponse>.Ok(new ExploreConceptResponse
            {
                ConceptText = request.ConceptText,
                Nodes = nodes,
                Relationships = relationships,
                ModalityBreakdown = modalityBreakdown
            }, new ApiMetadata
            {
                TotalCount = nodes.Count,
                Extra = new Dictionary<string, object>
                {
                    ["conceptSimilarityThreshold"] = request.MinSimilarity ?? 0.7,
                    ["relationshipCount"] = relationships.Count
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Concept exploration failed");
            return StatusCode(500, ApiResponse<object>.Fail("EXPLORATION_FAILED", ex.Message));
        }
    }

    private async Task<float[]> GetEmbeddingForTextAsync(string text, CancellationToken cancellationToken)
    {
        // Call semantic search with a dummy vector to trigger embedding generation via sp_TextToEmbedding
        // In production, this should be a direct call to IEmbeddingService.EmbedTextAsync
        // For now, return normalized random vector based on text hash for deterministic results
        var embedding = new float[768];
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }
        
        // Normalize
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
        }
        
        await Task.CompletedTask;
        return embedding;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<GetGraphStatsResponse>), 200)]
    public async Task<IActionResult> GetGraphStats(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving graph statistics");

            await using var session = _neo4jDriver.AsyncSession();
            
            // Get total counts and modality breakdown
            var countQuery = @"
                MATCH (n:Atom)
                WITH count(n) AS totalNodes
                MATCH ()-[r]->()
                RETURN totalNodes, count(r) AS totalRels";
            
            var countCursor = await session.RunAsync(countQuery);
            var countRecord = await countCursor.SingleAsync();
            var totalNodes = countRecord["totalNodes"].As<long>();
            var totalRels = countRecord["totalRels"].As<long>();
            
            // Get modality breakdown
            var modalityQuery = "MATCH (n:Atom) RETURN n.modality AS modality, count(*) AS count";
            var modalityCursor = await session.RunAsync(modalityQuery);
            var modalityRecords = await modalityCursor.ToListAsync(cancellationToken);
            var nodesByModality = modalityRecords.ToDictionary(
                r => r["modality"].As<string>() ?? "unknown",
                r => r["count"].As<long>()
            );
            
            // Get relationship type breakdown
            var relTypeQuery = "MATCH ()-[r]->() RETURN type(r) AS relType, count(*) AS count";
            var relTypeCursor = await session.RunAsync(relTypeQuery);
            var relTypeRecords = await relTypeCursor.ToListAsync(cancellationToken);
            var relationshipsByType = relTypeRecords.ToDictionary(
                r => r["relType"].As<string>(),
                r => r["count"].As<long>()
            );
            
            // Get degree statistics
            var degreeQuery = @"
                MATCH (n:Atom)
                WITH n, size((n)-[]-()) AS degree
                RETURN avg(degree) AS avgDegree, max(degree) AS maxDegree";
            var degreeCursor = await session.RunAsync(degreeQuery);
            var degreeRecord = await degreeCursor.SingleAsync();
            var avgDegree = degreeRecord["avgDegree"].As<double>();
            var maxDegree = degreeRecord["maxDegree"].As<int>();
            
            // Get isolated node count
            var isolatedQuery = "MATCH (n:Atom) WHERE NOT (n)-[]-() RETURN count(n) AS isolatedCount";
            var isolatedCursor = await session.RunAsync(isolatedQuery);
            var isolatedRecord = await isolatedCursor.SingleAsync();
            var isolatedCount = isolatedRecord["isolatedCount"].As<long>();

            var response = new GetGraphStatsResponse
            {
                TotalNodes = totalNodes,
                TotalRelationships = totalRels,
                NodesByModality = nodesByModality,
                RelationshipsByType = relationshipsByType,
                AverageDegree = avgDegree,
                MaxDegree = maxDegree,
                IsolatedNodes = isolatedCount
            };

            return Ok(ApiResponse<GetGraphStatsResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve graph stats");
            return StatusCode(500, ApiResponse<object>.Fail("STATS_FAILED", ex.Message));
        }
    }

    [HttpPost("relationship")]
    [ProducesResponseType(typeof(ApiResponse<CreateRelationshipResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateRelationship(
        [FromBody] CreateRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RelationshipType))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "RelationshipType is required"));
        }

        if (request.FromAtomId == request.ToAtomId)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Cannot create self-relationship"));
        }

        try
        {
            // Cypher query to create relationship
            var propertiesJson = request.Properties != null 
                ? System.Text.Json.JsonSerializer.Serialize(request.Properties)
                : "{}";

            var cypherQuery = $@"
                MATCH (from:Atom {{atomId: $fromId}})
                MATCH (to:Atom {{atomId: $toId}})
                MERGE (from)-[r:{request.RelationshipType}]->(to)
                ON CREATE SET 
                    r.weight = $weight,
                    r.createdAt = datetime(),
                    r.properties = $properties
                RETURN r";

            _logger.LogInformation("Creating relationship: {FromId} -{Type}-> {ToId}",
                request.FromAtomId, request.RelationshipType, request.ToAtomId);

            await using var session = _neo4jDriver.AsyncSession();
            var parameters = new
            {
                fromId = request.FromAtomId,
                toId = request.ToAtomId,
                weight = request.Weight ?? 1.0,
                properties = propertiesJson
            };
            
            var cursor = await session.RunAsync(cypherQuery, parameters);
            var summary = await cursor.ConsumeAsync();
            var success = summary.Counters.RelationshipsCreated > 0 || summary.Counters.PropertiesSet > 0;

            return Ok(ApiResponse<CreateRelationshipResponse>.Ok(new CreateRelationshipResponse
            {
                FromAtomId = request.FromAtomId,
                ToAtomId = request.ToAtomId,
                RelationshipType = request.RelationshipType,
                Success = success,
                Message = success ? "Relationship created successfully" : "Failed to create relationship"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create relationship");
            return StatusCode(500, ApiResponse<object>.Fail("CREATION_FAILED", ex.Message));
        }
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
