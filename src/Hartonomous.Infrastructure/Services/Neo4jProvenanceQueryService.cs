using Hartonomous.Core.Interfaces.Provenance;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Neo4j-backed implementation of provenance query services.
/// Provides lineage, session paths, error analysis, and influence tracking via Cypher queries.
/// </summary>
public class Neo4jProvenanceQueryService : IProvenanceQueryService
{
    private readonly IDriver? _driver;
    private readonly ILogger<Neo4jProvenanceQueryService> _logger;

    public Neo4jProvenanceQueryService(
        IDriver? driver,
        ILogger<Neo4jProvenanceQueryService> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AtomLineage> GetAtomLineageAsync(
        long atomId,
        int? maxDepth = null,
        CancellationToken cancellationToken = default)
    {
        var depth = maxDepth ?? 5;
        _logger.LogInformation("Fetching lineage for atom {AtomId} with max depth {Depth}", atomId, depth);

        if (_driver == null)
        {
            _logger.LogWarning("Neo4j driver not configured, returning empty lineage");
            return new AtomLineage
            {
                AtomId = atomId,
                Parents = new List<AtomNode>(),
                Depth = 0,
                TotalAncestors = 0
            };
        }

        await using var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                // Recursive query to get ancestor chain up to maxDepth
                var query = @"
                    MATCH path = (a:Atom {atomId: $atomId})-[:DERIVED_FROM|INFLUENCED_BY*1.." + depth + @"]->(ancestor:Atom)
                    WITH a, ancestor, relationships(path) as rels, length(path) as pathLength
                    ORDER BY pathLength
                    RETURN ancestor.atomId as ancestorId,
                           ancestor.canonicalText as label,
                           type(last(rels)) as relType,
                           pathLength as depth
                    LIMIT 100";

                var cursor = await tx.RunAsync(query, new { atomId });
                var records = await cursor.ToListAsync();

                var parents = new List<AtomNode>();
                var totalAncestors = 0;
                var maxDepthReached = 0;

                foreach (var record in records)
                {
                    totalAncestors++;
                    var d = record["depth"].As<int>();
                    if (d > maxDepthReached) maxDepthReached = d;

                    if (d == 1) // Direct parents only in Parents list
                    {
                        parents.Add(new AtomNode
                        {
                            AtomId = record["ancestorId"].As<long>(),
                            RelationshipType = record["relType"].As<string>() ?? "DERIVED_FROM",
                            Metadata = record["label"].As<string>()
                        });
                    }
                }

                return new AtomLineage
                {
                    AtomId = atomId,
                    Parents = parents,
                    Depth = maxDepthReached,
                    TotalAncestors = totalAncestors
                };
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lineage for atom {AtomId}", atomId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReasoningPath>> GetSessionPathsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching reasoning paths for session {SessionId}", sessionId);

        if (_driver == null)
        {
            _logger.LogWarning("Neo4j driver not configured, returning empty paths");
            return Array.Empty<ReasoningPath>();
        }

        await using var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Session {sessionId: $sessionId})-[:HAS_PATH]->(p:ReasoningPath)
                    OPTIONAL MATCH (p)-[:CONTAINS]->(a:Atom)
                    OPTIONAL MATCH (p)-[:HAS_DECISION]->(d:DecisionPoint)
                    WITH p, collect(DISTINCT a.atomId) as atoms, collect(DISTINCT {
                        atomId: d.atomId,
                        description: d.description,
                        chosen: d.chosenBranch
                    }) as decisions
                    RETURN p.pathId as pathId,
                           p.isSuccessful as isSuccessful,
                           atoms, decisions";

                var cursor = await tx.RunAsync(query, new { sessionId });
                var records = await cursor.ToListAsync();

                var paths = new List<ReasoningPath>();
                foreach (var record in records)
                {
                    var decisions = new List<DecisionPoint>();
                    var decisionData = record["decisions"].As<List<IDictionary<string, object>>>();
                    if (decisionData != null)
                    {
                        foreach (var d in decisionData)
                        {
                            if (d.TryGetValue("atomId", out var aid) && aid != null)
                            {
                                d.TryGetValue("description", out var desc);
                                d.TryGetValue("chosen", out var chosen);
                                decisions.Add(new DecisionPoint
                                {
                                    AtomId = Convert.ToInt64(aid),
                                    Description = desc?.ToString() ?? "",
                                    ChosenBranch = chosen?.ToString()
                                });
                            }
                        }
                    }

                    paths.Add(new ReasoningPath
                    {
                        PathId = record["pathId"].As<string>() ?? Guid.NewGuid().ToString(),
                        SessionId = sessionId,
                        AtomSequence = record["atoms"].As<List<long>>() ?? new List<long>(),
                        Decisions = decisions,
                        IsSuccessful = record["isSuccessful"].As<bool?>() ?? false
                    });
                }

                return paths;
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session paths for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AtomizationError>> GetSessionErrorsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching errors for session {SessionId}", sessionId);

        if (_driver == null)
        {
            return Array.Empty<AtomizationError>();
        }

        await using var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Session {sessionId: $sessionId})-[:HAS_ERROR]->(e:Error)
                    RETURN e.atomId as atomId,
                           e.message as message,
                           e.errorType as errorType,
                           e.timestamp as timestamp,
                           e.severity as severity
                    ORDER BY e.timestamp DESC
                    LIMIT 100";

                var cursor = await tx.RunAsync(query, new { sessionId });
                var records = await cursor.ToListAsync();

                return records.Select(r => new AtomizationError
                {
                    AtomId = r["atomId"].As<long>(),
                    SessionId = sessionId,
                    ErrorMessage = r["message"].As<string>() ?? "Unknown error",
                    ErrorType = r["errorType"].As<string>() ?? "Unknown",
                    Timestamp = r["timestamp"].As<DateTime?>() ?? DateTime.UtcNow,
                    Severity = r["severity"].As<string>()
                }).ToList();
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session errors for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ErrorCluster>> FindErrorClustersAsync(
        long? sessionId = null,
        int minClusterSize = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding error clusters with min size {MinSize}", minClusterSize);

        if (_driver == null)
        {
            return Array.Empty<ErrorCluster>();
        }

        await using var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var sessionFilter = sessionId.HasValue
                    ? "WHERE s.sessionId = $sessionId"
                    : "";

                var query = $@"
                    MATCH (s:Session)-[:HAS_ERROR]->(e:Error)
                    {sessionFilter}
                    WITH e.errorType as errorType, collect(e) as errors
                    WHERE size(errors) >= $minClusterSize
                    RETURN errorType as pattern,
                           size(errors) as errorCount,
                           [err IN errors | err.atomId] as atomIds,
                           min([err IN errors | err.timestamp]) as firstOccurrence,
                           max([err IN errors | err.timestamp]) as lastOccurrence";

                var cursor = await tx.RunAsync(query, new { sessionId, minClusterSize });
                var records = await cursor.ToListAsync();

                var clusters = new List<ErrorCluster>();
                var index = 0;
                foreach (var r in records)
                {
                    clusters.Add(new ErrorCluster
                    {
                        ClusterId = $"cluster_{++index}",
                        Pattern = r["pattern"].As<string>() ?? "Unknown",
                        ErrorCount = r["errorCount"].As<int>(),
                        AtomIds = r["atomIds"].As<List<long>>() ?? new List<long>(),
                        FirstOccurrence = r["firstOccurrence"].As<DateTime?>() ?? DateTime.UtcNow.AddDays(-7),
                        LastOccurrence = r["lastOccurrence"].As<DateTime?>() ?? DateTime.UtcNow
                    });
                }

                return clusters;
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding error clusters");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InfluenceRelationship>> GetInfluencesAsync(
        long atomId,
        int maxDepth = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching influences for atom {AtomId} with max depth {Depth}", atomId, maxDepth);

        if (_driver == null)
        {
            return Array.Empty<InfluenceRelationship>();
        }

        await using var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH path = (source:Atom)-[r:INFLUENCED_BY|DERIVED_FROM*1.." + maxDepth + @"]->(target:Atom {atomId: $atomId})
                    WITH source, target, relationships(path) as rels, length(path) as depth
                    RETURN source.atomId as sourceId,
                           target.atomId as targetId,
                           type(last(rels)) as relType,
                           depth,
                           CASE WHEN size(rels) > 0 THEN coalesce(last(rels).weight, 1.0) ELSE 1.0 END as weight
                    ORDER BY depth, weight DESC
                    LIMIT 100";

                var cursor = await tx.RunAsync(query, new { atomId });
                var records = await cursor.ToListAsync();

                return records.Select(r => new InfluenceRelationship
                {
                    SourceAtomId = r["sourceId"].As<long>(),
                    TargetAtomId = r["targetId"].As<long>(),
                    RelationshipType = r["relType"].As<string>() ?? "INFLUENCED_BY",
                    Depth = r["depth"].As<int>(),
                    Weight = r["weight"].As<double>()
                }).ToList();
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching influences for atom {AtomId}", atomId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AtomInfluence>> GetInfluencingAtomsAsync(
        long atomId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching influencing atoms for atom {AtomId}", atomId);

        if (_driver == null)
        {
            return Array.Empty<AtomInfluence>();
        }

        await using var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (target:Atom {atomId: $atomId})
                    OPTIONAL MATCH (direct:Atom)-[r1:INFLUENCED_BY|DERIVED_FROM]->(target)
                    OPTIONAL MATCH (indirect:Atom)-[:INFLUENCED_BY|DERIVED_FROM*2..5]->(target)
                    WITH target,
                         collect(DISTINCT {atomId: direct.atomId, weight: coalesce(r1.weight, 1.0), type: 'Direct', pathLen: 1}) as directInfluences,
                         collect(DISTINCT {atomId: indirect.atomId, type: 'Indirect', pathLen: 2}) as indirectInfluences
                    UNWIND (directInfluences + indirectInfluences) as influence
                    WHERE influence.atomId IS NOT NULL
                    RETURN DISTINCT influence.atomId as atomId,
                           coalesce(influence.weight, 0.5) as weight,
                           influence.type as influenceType,
                           influence.pathLen as pathLength
                    ORDER BY weight DESC
                    LIMIT 50";

                var cursor = await tx.RunAsync(query, new { atomId });
                var records = await cursor.ToListAsync();

                return records.Select(r => new AtomInfluence
                {
                    AtomId = r["atomId"].As<long>(),
                    Weight = r["weight"].As<double>(),
                    InfluenceType = r["influenceType"].As<string>() ?? "Indirect",
                    PathLength = r["pathLength"].As<int>()
                }).ToList();
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching influencing atoms for atom {AtomId}", atomId);
            throw;
        }
    }
}
