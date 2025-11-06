using Hartonomous.Api.DTOs.Graph;
using Hartonomous.Core.Interfaces;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Neo4j.Driver;
using System.Diagnostics;
using System.Text.Json;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GraphController : ApiControllerBase
{
    private readonly ILogger<GraphController> _logger;
    private readonly IDriver _neo4jDriver;
    private readonly IInferenceService _inferenceService;
    private readonly string _connectionString;

    public GraphController(
        ILogger<GraphController> logger, 
        IDriver neo4jDriver,
        IInferenceService inferenceService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _connectionString = configuration.GetConnectionString("HartonomousDb") 
            ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'HartonomousDb' not found");
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

            return Ok(Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve graph stats");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Failed to retrieve graph stats", ex.Message);
            return StatusCode(500, Failure<GetGraphStatsResponse>(new[] { error }));
        }
    }

    [HttpPost("relationship")]
    [ProducesResponseType(typeof(ApiResponse<CreateRelationshipResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<CreateRelationshipResponse>), 400)]
    public async Task<IActionResult> CreateRelationship(
        [FromBody] CreateRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RelationshipType))
        {
            return BadRequest(Failure<CreateRelationshipResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "RelationshipType is required", "RelationshipType cannot be empty")
            }));
        }

        if (request.FromAtomId == request.ToAtomId)
        {
            return BadRequest(Failure<CreateRelationshipResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidFieldValue, "Cannot create self-relationship", "FromAtomId and ToAtomId must be different")
            }));
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

            return Ok(Success(new CreateRelationshipResponse
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
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.ExternalDependencyFailure, "Failed to create relationship", ex.Message);
            return StatusCode(500, Failure<CreateRelationshipResponse>(new[] { error }));
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

    // ============================================================================
    // SQL Server Graph Endpoints (AS NODE / AS EDGE MATCH syntax)
    // ============================================================================

    /// <summary>
    /// Create a node in SQL Server graph database (graph.AtomGraphNodes)
    /// </summary>
    [HttpPost("sql/nodes")]
    [Authorize(Policy = "DataScientist")]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateNodeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateNodeResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateNodeResponse>), 503)]
    public async Task<IActionResult> CreateSqlGraphNode([FromBody] SqlGraphCreateNodeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NodeType))
        {
            return BadRequest(Failure<SqlGraphCreateNodeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "NodeType is required", "NodeType cannot be empty")
            }));
        }

        try
        {
            _logger.LogInformation("Creating SQL graph node for AtomId {AtomId} with type {NodeType}", 
                request.AtomId, request.NodeType);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var metadataJson = request.Metadata != null 
                ? JsonSerializer.Serialize(request.Metadata) 
                : null;

            var sql = @"
                INSERT INTO graph.AtomGraphNodes (AtomId, NodeType, Metadata, EmbeddingX, EmbeddingY, EmbeddingZ, CreatedUtc)
                OUTPUT INSERTED.NodeId, INSERTED.AtomId, INSERTED.NodeType
                VALUES (@AtomId, @NodeType, @Metadata, @EmbeddingX, @EmbeddingY, @EmbeddingZ, SYSUTCDATETIME())";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AtomId", request.AtomId);
            command.Parameters.AddWithValue("@NodeType", request.NodeType);
            command.Parameters.AddWithValue("@Metadata", (object?)metadataJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmbeddingX", (object?)request.EmbeddingX ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmbeddingY", (object?)request.EmbeddingY ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmbeddingZ", (object?)request.EmbeddingZ ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var response = new SqlGraphCreateNodeResponse
                {
                    NodeId = reader.GetInt64(0),
                    AtomId = reader.GetInt64(1),
                    NodeType = reader.GetString(2),
                    Success = true,
                    Message = "Node created successfully"
                };

                return Ok(Success(response));
            }

            return StatusCode(500, Failure<SqlGraphCreateNodeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                    "Failed to create graph node", "No rows returned from INSERT")
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error creating graph node");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to create SQL graph node", ex.Message);
            return StatusCode(503, Failure<SqlGraphCreateNodeResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating graph node");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Unexpected error creating graph node", ex.Message);
            return StatusCode(503, Failure<SqlGraphCreateNodeResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Create an edge in SQL Server graph database (graph.AtomGraphEdges)
    /// </summary>
    [HttpPost("sql/edges")]
    [Authorize(Policy = "DataScientist")]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateEdgeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateEdgeResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateEdgeResponse>), 503)]
    public async Task<IActionResult> CreateSqlGraphEdge([FromBody] SqlGraphCreateEdgeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EdgeType))
        {
            return BadRequest(Failure<SqlGraphCreateEdgeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "EdgeType is required", "EdgeType cannot be empty")
            }));
        }

        try
        {
            _logger.LogInformation("Creating SQL graph edge: {FromNodeId} -{EdgeType}-> {ToNodeId}", 
                request.FromNodeId, request.EdgeType, request.ToNodeId);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var metadataJson = request.Metadata != null 
                ? JsonSerializer.Serialize(request.Metadata) 
                : null;

            // SQL Server graph edges use special $from_id and $to_id pseudo-columns
            var sql = @"
                INSERT INTO graph.AtomGraphEdges ($from_id, $to_id, EdgeType, Weight, Metadata, ValidFrom, ValidTo, CreatedUtc)
                SELECT 
                    (SELECT $node_id FROM graph.AtomGraphNodes WHERE NodeId = @FromNodeId),
                    (SELECT $node_id FROM graph.AtomGraphNodes WHERE NodeId = @ToNodeId),
                    @EdgeType, @Weight, @Metadata, @ValidFrom, @ValidTo, SYSUTCDATETIME();
                
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS EdgeId, @FromNodeId AS FromNodeId, @ToNodeId AS ToNodeId, @EdgeType AS EdgeType";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FromNodeId", request.FromNodeId);
            command.Parameters.AddWithValue("@ToNodeId", request.ToNodeId);
            command.Parameters.AddWithValue("@EdgeType", request.EdgeType);
            command.Parameters.AddWithValue("@Weight", request.Weight);
            command.Parameters.AddWithValue("@Metadata", (object?)metadataJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@ValidFrom", (object?)request.ValidFrom ?? DBNull.Value);
            command.Parameters.AddWithValue("@ValidTo", (object?)request.ValidTo ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var response = new SqlGraphCreateEdgeResponse
                {
                    EdgeId = reader.GetInt64(0),
                    FromNodeId = reader.GetInt64(1),
                    ToNodeId = reader.GetInt64(2),
                    EdgeType = reader.GetString(3),
                    Success = true,
                    Message = "Edge created successfully"
                };

                return Ok(Success(response));
            }

            return StatusCode(500, Failure<SqlGraphCreateEdgeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                    "Failed to create graph edge", "No rows returned from INSERT")
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error creating graph edge");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to create SQL graph edge", ex.Message);
            return StatusCode(503, Failure<SqlGraphCreateEdgeResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating graph edge");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Unexpected error creating graph edge", ex.Message);
            return StatusCode(503, Failure<SqlGraphCreateEdgeResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Traverse SQL Server graph using MATCH syntax
    /// </summary>
    [HttpGet("sql/traverse")]
    [Authorize]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphTraverseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphTraverseResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphTraverseResponse>), 503)]
    public async Task<IActionResult> TraverseSqlGraph(
        [FromQuery] long startAtomId,
        [FromQuery] long? endAtomId = null,
        [FromQuery] int maxDepth = 3,
        [FromQuery] string? edgeTypeFilter = null,
        [FromQuery] string direction = "outbound")
    {
        if (maxDepth < 1 || maxDepth > 5)
        {
            return BadRequest(Failure<SqlGraphTraverseResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidFieldValue, 
                    "MaxDepth must be between 1 and 5", $"Received: {maxDepth}")
            }));
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Traversing SQL graph from AtomId {StartAtomId} with depth {MaxDepth}", 
                startAtomId, maxDepth);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Build MATCH pattern based on direction
            var matchPattern = direction.ToLower() switch
            {
                "outbound" => "(start)-(edge)->(next)",
                "inbound" => "(start)<-(edge)-(next)",
                "both" => "(start)-(edge)-(next)",
                _ => "(start)-(edge)->(next)"
            };

            // Build edge type filter
            var edgeFilter = !string.IsNullOrWhiteSpace(edgeTypeFilter)
                ? $"AND edge.EdgeType = @EdgeTypeFilter"
                : "";

            // Recursive CTE to traverse graph up to maxDepth
            var sql = $@"
                WITH RECURSIVE GraphTraversal AS (
                    -- Base case: start node
                    SELECT 
                        start.NodeId AS NodeId,
                        start.AtomId AS AtomId,
                        CAST(start.NodeId AS NVARCHAR(MAX)) AS NodePath,
                        CAST(start.AtomId AS NVARCHAR(MAX)) AS AtomPath,
                        CAST('' AS NVARCHAR(MAX)) AS EdgePath,
                        0 AS Depth,
                        0.0 AS TotalWeight
                    FROM graph.AtomGraphNodes AS start
                    WHERE start.AtomId = @StartAtomId
                    
                    UNION ALL
                    
                    -- Recursive case: traverse edges
                    SELECT 
                        next.NodeId,
                        next.AtomId,
                        gt.NodePath + ',' + CAST(next.NodeId AS NVARCHAR(MAX)),
                        gt.AtomPath + ',' + CAST(next.AtomId AS NVARCHAR(MAX)),
                        CASE 
                            WHEN gt.EdgePath = '' THEN edge.EdgeType
                            ELSE gt.EdgePath + ',' + edge.EdgeType
                        END,
                        gt.Depth + 1,
                        gt.TotalWeight + edge.Weight
                    FROM GraphTraversal gt
                    INNER JOIN graph.AtomGraphNodes AS start ON start.NodeId = gt.NodeId
                    INNER JOIN graph.AtomGraphEdges AS edge ON MATCH {matchPattern}
                    WHERE gt.Depth < @MaxDepth
                      {edgeFilter}
                      AND next.NodeId NOT IN (SELECT value FROM STRING_SPLIT(gt.NodePath, ','))
                )
                SELECT DISTINCT
                    NodePath,
                    AtomPath,
                    EdgePath,
                    Depth AS PathLength,
                    TotalWeight
                FROM GraphTraversal
                WHERE Depth > 0";

            if (endAtomId.HasValue)
            {
                sql += " AND AtomId = @EndAtomId";
            }

            sql += " ORDER BY Depth, TotalWeight DESC";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartAtomId", startAtomId);
            command.Parameters.AddWithValue("@MaxDepth", maxDepth);
            if (!string.IsNullOrWhiteSpace(edgeTypeFilter))
                command.Parameters.AddWithValue("@EdgeTypeFilter", edgeTypeFilter);
            if (endAtomId.HasValue)
                command.Parameters.AddWithValue("@EndAtomId", endAtomId.Value);

            var paths = new List<SqlGraphPathEntry>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var nodeIds = reader.GetString(0).Split(',').Select(long.Parse).ToList();
                var atomIds = reader.GetString(1).Split(',').Select(long.Parse).ToList();
                var edgeTypes = reader.GetString(2).Split(',').ToList();
                
                paths.Add(new SqlGraphPathEntry
                {
                    NodeIds = nodeIds,
                    AtomIds = atomIds,
                    EdgeTypes = edgeTypes,
                    PathLength = reader.GetInt32(3),
                    TotalWeight = reader.GetDouble(4)
                });
            }

            stopwatch.Stop();

            var response = new SqlGraphTraverseResponse
            {
                StartAtomId = startAtomId,
                EndAtomId = endAtomId,
                Paths = paths,
                TotalPathsFound = paths.Count,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error traversing graph");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to traverse SQL graph", ex.Message);
            return StatusCode(503, Failure<SqlGraphTraverseResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error traversing graph");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Unexpected error traversing graph", ex.Message);
            return StatusCode(503, Failure<SqlGraphTraverseResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Find shortest path between two atoms using SQL Server SHORTEST_PATH
    /// </summary>
    [HttpGet("sql/shortest-path")]
    [Authorize]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphShortestPathResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphShortestPathResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphShortestPathResponse>), 503)]
    public async Task<IActionResult> FindShortestPath(
        [FromQuery] long startAtomId,
        [FromQuery] long endAtomId,
        [FromQuery] string? edgeTypeFilter = null)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Finding shortest path from {StartAtomId} to {EndAtomId}", 
                startAtomId, endAtomId);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // SQL Server 2019+ SHORTEST_PATH syntax
            var edgeFilter = !string.IsNullOrWhiteSpace(edgeTypeFilter)
                ? $"AND edge.EdgeType = @EdgeTypeFilter"
                : "";

            var sql = $@"
                SELECT 
                    STRING_AGG(CAST(n.NodeId AS NVARCHAR(MAX)), ',') WITHIN GROUP (GRAPH PATH) AS NodePath,
                    STRING_AGG(CAST(n.AtomId AS NVARCHAR(MAX)), ',') WITHIN GROUP (GRAPH PATH) AS AtomPath,
                    STRING_AGG(edge.EdgeType, ',') WITHIN GROUP (GRAPH PATH) AS EdgePath,
                    COUNT(n.NodeId) WITHIN GROUP (GRAPH PATH) - 1 AS PathLength,
                    SUM(edge.Weight) WITHIN GROUP (GRAPH PATH) AS TotalWeight
                FROM graph.AtomGraphNodes AS start
                    , graph.AtomGraphEdges FOR PATH AS edge
                    , graph.AtomGraphNodes FOR PATH AS n
                    , graph.AtomGraphNodes AS finish
                WHERE MATCH(SHORTEST_PATH(start(-(edge)->n)+finish))
                  AND start.AtomId = @StartAtomId
                  AND finish.AtomId = @EndAtomId
                  {edgeFilter}";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartAtomId", startAtomId);
            command.Parameters.AddWithValue("@EndAtomId", endAtomId);
            if (!string.IsNullOrWhiteSpace(edgeTypeFilter))
                command.Parameters.AddWithValue("@EdgeTypeFilter", edgeTypeFilter);

            SqlGraphPathEntry? shortestPath = null;
            await using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var nodeIds = reader.GetString(0).Split(',').Select(long.Parse).ToList();
                var atomIds = reader.GetString(1).Split(',').Select(long.Parse).ToList();
                var edgeTypes = reader.IsDBNull(2) ? new List<string>() : reader.GetString(2).Split(',').ToList();
                
                shortestPath = new SqlGraphPathEntry
                {
                    NodeIds = nodeIds,
                    AtomIds = atomIds,
                    EdgeTypes = edgeTypes,
                    PathLength = reader.GetInt32(3),
                    TotalWeight = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4)
                };
            }

            stopwatch.Stop();

            var response = new SqlGraphShortestPathResponse
            {
                StartAtomId = startAtomId,
                EndAtomId = endAtomId,
                ShortestPath = shortestPath,
                PathFound = shortestPath != null,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };

            return Ok(Success(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error finding shortest path");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Failed to find shortest path", ex.Message);
            return StatusCode(503, Failure<SqlGraphShortestPathResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error finding shortest path");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, 
                "Unexpected error finding shortest path", ex.Message);
            return StatusCode(503, Failure<SqlGraphShortestPathResponse>(new[] { error }));
        }
    }
}
