using Asp.Versioning;
using Hartonomous.Api.DTOs.Common;
using Hartonomous.Api.DTOs.Provenance;
using Hartonomous.Api.DTOs.Research;
using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Research and knowledge discovery controller - semantic search capabilities.
/// Calls stored procedures via ISearchService and IProvenanceWriteService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/research")]
[Authorize(Policy = "ApiUser")]
public class ResearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IProvenanceWriteService _provenanceService;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(
        ISearchService searchService,
        IProvenanceWriteService provenanceService,
        ILogger<ResearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes semantic research query across knowledge base.
    /// Calls sp_SemanticSearch stored procedure via ISearchService.
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ResearchQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteQuery(
        [FromBody] ResearchQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Research: Executing query '{Query}'", request.Query);

        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Execute semantic search via stored procedure
        var searchResults = await _searchService.SemanticSearchAsync(
            request.Query,
            request.TopK ?? 10,
            request.TenantId ?? 0,
            cancellationToken);

        sw.Stop();

        var results = searchResults.Select(r => new ResearchResult
        {
            AtomId = r.AtomId,
            Title = r.ContentPreview?.Split('\n').FirstOrDefault() ?? "Untitled",
            Snippet = r.ContentPreview ?? string.Empty,
            RelevanceScore = r.Score,
            Source = "Hartonomous Knowledge Base",
            AtomType = r.Modality ?? "Unknown",
            Location = new GeoPoint(),
            Tags = new List<string>()
        }).ToList();

        var response = new ResearchQueryResponse
        {
            Query = request.Query,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Results = results,
            Aggregations = new QueryAggregations
            {
                TotalMatches = results.Count,
                ByAtomType = results.GroupBy(r => r.AtomType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                BySource = new Dictionary<string, int>
                {
                    ["Hartonomous Knowledge Base"] = results.Count
                },
                TopTags = new List<string>()
            },
            SpatialCoverage = new SpatialBounds(),
            DemoMode = false
        };

        return Ok(response);
    }

    /// <summary>
    /// Performs semantic similarity search.
    /// Calls sp_SemanticSearch via ISearchService.
    /// </summary>
    [HttpGet("semantic-search")]
    [ProducesResponseType(typeof(SemanticSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SemanticSearch(
        [FromQuery] string text,
        [FromQuery] int limit = 10,
        [FromQuery] double minSimilarity = 0.7,
        [FromQuery] int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Research: Semantic search for '{Text}'", text);

        if (string.IsNullOrWhiteSpace(text))
            return BadRequest("Text parameter is required");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var searchResults = await _searchService.SemanticSearchAsync(
            text,
            limit,
            tenantId,
            cancellationToken);

        sw.Stop();

        var matches = searchResults
            .Where(r => r.Score >= minSimilarity)
            .Select(r => new SemanticMatch
            {
                AtomId = r.AtomId,
                Content = r.ContentPreview ?? "",
                SimilarityScore = r.Score,
                Distance = 1.0 - r.Score,
                Method = "Cosine Similarity (sp_SemanticSearch)",
                EmbeddingDimensions = 1998 // VECTOR(1998) dimension
            }).ToList();

        var response = new SemanticSearchResponse
        {
            Query = text,
            Limit = limit,
            MinSimilarity = minSimilarity,
            ExecutionTimeMs = sw.ElapsedMilliseconds,
            Matches = matches,
            Statistics = new SearchStatistics
            {
                CandidatesEvaluated = matches.Count,
                IndexHitRate = 1.0,
                CacheHitRate = 0.0,
                AverageEmbeddingTime = sw.ElapsedMilliseconds
            },
            DemoMode = false
        };

        return Ok(response);
    }

    /// <summary>
    /// Explores knowledge graph neighborhood.
    /// Future: Neo4j Cypher queries via CLR integration, real-time graph traversal.
    /// </summary>
    [HttpGet("knowledge-graph/{atomId}")]
    [ProducesResponseType(typeof(KnowledgeGraphResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ExploreKnowledgeGraph(
        long atomId,
        [FromQuery] int depth = 2,
        [FromQuery] string? relationshipType = null)
    {
        _logger.LogInformation("Research: Exploring graph for atom {AtomId}, depth {Depth} (DEMO MODE)", 
            atomId, depth);

        var response = new KnowledgeGraphResponse
        {
            CenterAtomId = atomId,
            Depth = depth,
            Nodes = new List<GraphNode>
            {
                new()
                {
                    AtomId = atomId,
                    Label = "Spatial Semantic AI",
                    Type = "Concept",
                    Importance = 0.95,
                    Connections = 23,
                    Properties = new Dictionary<string, string>
                    {
                        ["field"] = "Artificial Intelligence",
                        ["subfield"] = "Semantic Reasoning",
                        ["maturity"] = "Emerging"
                    }
                },
                new()
                {
                    AtomId = atomId + 1,
                    Label = "Knowledge Graphs",
                    Type = "Technology",
                    Importance = 0.87,
                    Connections = 18,
                    Properties = new Dictionary<string, string>
                    {
                        ["implementation"] = "Neo4j",
                        ["query_language"] = "Cypher"
                    }
                },
                new()
                {
                    AtomId = atomId + 2,
                    Label = "SQL Server CLR",
                    Type = "Technology",
                    Importance = 0.89,
                    Connections = 15,
                    Properties = new Dictionary<string, string>
                    {
                        ["language"] = "C#",
                        ["performance"] = "Microsecond latency"
                    }
                },
                new()
                {
                    AtomId = atomId + 3,
                    Label = "Provenance Tracking",
                    Type = "Method",
                    Importance = 0.78,
                    Connections = 12,
                    Properties = new Dictionary<string, string>
                    {
                        ["purpose"] = "Explainability",
                        ["storage"] = "Graph + Temporal Tables"
                    }
                }
            },
            Relationships = new List<GraphRelationship>
            {
                new()
                {
                    From = atomId,
                    To = atomId + 1,
                    Type = "USES",
                    Weight = 0.92,
                    Properties = new Dictionary<string, string>
                    {
                        ["since"] = "2023",
                        ["confidence"] = "0.92"
                    }
                },
                new()
                {
                    From = atomId,
                    To = atomId + 2,
                    Type = "IMPLEMENTS_WITH",
                    Weight = 0.88,
                    Properties = new Dictionary<string, string>
                    {
                        ["performance_gain"] = "10x",
                        ["latency"] = "<1ms"
                    }
                },
                new()
                {
                    From = atomId,
                    To = atomId + 3,
                    Type = "ENABLES",
                    Weight = 0.85,
                    Properties = new Dictionary<string, string>
                    {
                        ["audit_level"] = "Complete",
                        ["granularity"] = "Per-atom"
                    }
                },
                new()
                {
                    From = atomId + 1,
                    To = atomId + 3,
                    Type = "PROVIDES",
                    Weight = 0.79,
                    Properties = new Dictionary<string, string>
                    {
                        ["data_structure"] = "Graph",
                        ["query_time"] = "Real-time"
                    }
                }
            },
            Communities = new List<GraphCommunity>
            {
                new()
                {
                    CommunityId = "tech_stack",
                    Label = "Technology Stack",
                    Members = new List<long> { atomId + 1, atomId + 2 },
                    Cohesion = 0.84
                },
                new()
                {
                    CommunityId = "research_methods",
                    Label = "Research Methodologies",
                    Members = new List<long> { atomId, atomId + 3 },
                    Cohesion = 0.79
                }
            },
            Statistics = new GraphStatistics
            {
                TotalNodes = 47,
                TotalRelationships = 89,
                AverageDegree = 3.8,
                Density = 0.04,
                Diameter = 5
            },
            DemoMode = true
        };

        return Ok(response);
    }
}
