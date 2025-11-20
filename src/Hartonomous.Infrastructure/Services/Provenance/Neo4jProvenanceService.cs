using System.Diagnostics;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Provenance;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.Services.Provenance;

/// <summary>
/// Neo4j implementation of provenance query service for READ-ONLY analytical Cypher queries.
/// Tracks atom lineage, error clustering, and reasoning session paths through the provenance graph.
/// </summary>
public sealed class Neo4jProvenanceService : IProvenanceQueryService
{
    private readonly ILogger<Neo4jProvenanceService> _logger;
    private readonly IDriver _driver;
    private readonly string _database;
    private readonly TelemetryClient? _telemetry;
    private readonly string _neo4jEndpoint;

    public Neo4jProvenanceService(
        ILogger<Neo4jProvenanceService> logger,
        IDriver driver,
        IOptions<Neo4jOptions> options,
        TelemetryClient? telemetry = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _telemetry = telemetry;
        
        var neo4jOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _database = neo4jOptions.Database;
        _neo4jEndpoint = neo4jOptions.Uri ?? "unknown";
        
        _logger.LogInformation("Neo4j provenance service initialized. Database: {Database}", _database);
    }

    public async Task<AtomLineage> GetAtomLineageAsync(
        long atomId,
        int? maxDepth = null,
        CancellationToken cancellationToken = default)
    {
        var depth = maxDepth ?? 10;
        _logger.LogDebug("Querying atom lineage for {AtomId}, max depth: {MaxDepth}", atomId, depth);

        var query = """
            MATCH path = (atom:Atom {atomId: $atomId})-[:DERIVED_FROM*0..{maxDepth}]->(ancestor:Atom)
            RETURN 
                atom.atomId as atomId,
                collect(DISTINCT ancestor.atomId) as ancestors,
                length(path) as depth,
                relationships(path) as derivations
            ORDER BY depth DESC
            LIMIT 1
            """;

        var dependency = new DependencyTelemetry
        {
            Name = "Neo4j.GetAtomLineage",
            Type = "Neo4j",
            Target = _neo4jEndpoint,
            Data = $"MATCH atom lineage for atomId: {atomId}, maxDepth: {depth}"
        };
        dependency.Properties["AtomId"] = atomId.ToString();
        dependency.Properties["MaxDepth"] = depth.ToString();
        dependency.Properties["Database"] = _database;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_database)
                .WithDefaultAccessMode(AccessMode.Read)); // READ-ONLY

            var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new { atomId, maxDepth = depth });
            var record = await cursor.SingleAsync();

            var parentNodes = new List<AtomNode>();
            var ancestors = record["ancestors"].As<List<long>>();
            
            foreach (var ancestorId in ancestors)
            {
                parentNodes.Add(new AtomNode
                {
                    AtomId = ancestorId,
                    RelationshipType = "DERIVED_FROM"
                });
            }

            var resultDepth = record["depth"].As<int>();

            return new AtomLineage
            {
                AtomId = atomId,
                Parents = parentNodes,
                Depth = resultDepth,
                TotalAncestors = ancestors.Count
            };
        });

            _logger.LogDebug("Lineage query completed. Found {AncestorCount} ancestors at depth {Depth}", 
                result.TotalAncestors, result.Depth);

            dependency.Success = true;
            dependency.Metrics["AncestorCount"] = result.TotalAncestors;
            dependency.Metrics["Depth"] = result.Depth;
            return result;
        }
        catch (Exception ex)
        {
            dependency.Success = false;
            _logger.LogError(ex, "Failed to query atom lineage for {AtomId}", atomId);
            throw;
        }
        finally
        {
            dependency.Duration = sw.Elapsed;
            _telemetry?.TrackDependency(dependency);
        }
    }

    public async Task<IEnumerable<ErrorCluster>> FindErrorClustersAsync(
        long? sessionId = null,
        int minClusterSize = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding error clusters for session {SessionId}, min size: {MinSize}", 
            sessionId, minClusterSize);

        var query = """
            MATCH (error:Error)-[:OCCURRED_IN]->(session:Session)
            WHERE $sessionId IS NULL OR session.sessionId = $sessionId
            WITH error, session, error.errorType as errorType
            MATCH (error)-[:RELATED_TO*1..2]-(relatedError:Error)
            WITH errorType, collect(DISTINCT error) as errors, collect(DISTINCT session) as sessions
            WHERE size(errors) >= $minClusterSize
            RETURN 
                errorType,
                size(errors) as errorCount,
                [e IN errors | e.errorId] as errorIds,
                min([e IN errors | e.timestamp]) as firstOccurrence,
                max([e IN errors | e.timestamp]) as lastOccurrence
            ORDER BY errorCount DESC
            """;

        var dependency = new DependencyTelemetry
        {
            Name = "Neo4j.FindErrorClusters",
            Type = "Neo4j",
            Target = _neo4jEndpoint,
            Data = $"Find error clusters for sessionId: {sessionId?.ToString() ?? "all"}, minSize: {minClusterSize}"
        };
        dependency.Properties["SessionId"] = sessionId?.ToString() ?? "all";
        dependency.Properties["MinClusterSize"] = minClusterSize.ToString();
        dependency.Properties["Database"] = _database;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_database)
                .WithDefaultAccessMode(AccessMode.Read));

            var clusters = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new 
            { 
                sessionId,
                minClusterSize 
            });

            var results = new List<ErrorCluster>();

            await foreach (var record in cursor)
            {
                var pattern = record["errorType"].As<string>();
                var errorCount = record["errorCount"].As<int>();
                var errorIds = record["errorIds"].As<List<long>>();
                var firstOccurrence = DateTime.Parse(record["firstOccurrence"].As<string>());
                var lastOccurrence = DateTime.Parse(record["lastOccurrence"].As<string>());

                results.Add(new ErrorCluster
                {
                    ClusterId = Guid.NewGuid().ToString(),
                    ErrorCount = errorCount,
                    Pattern = pattern,
                    AtomIds = errorIds,
                    FirstOccurrence = firstOccurrence,
                    LastOccurrence = lastOccurrence
                });
            }

            return results;
        });

            _logger.LogInformation("Error cluster analysis completed. Found {ClusterCount} clusters.", clusters.Count);

            dependency.Success = true;
            dependency.Metrics["ClusterCount"] = clusters.Count;
            return clusters;
        }
        catch (Exception ex)
        {
            dependency.Success = false;
            _logger.LogError(ex, "Failed to find error clusters for sessionId {SessionId}", sessionId);
            throw;
        }
        finally
        {
            dependency.Duration = sw.Elapsed;
            _telemetry?.TrackDependency(dependency);
        }
    }

    public async Task<IEnumerable<ReasoningPath>> GetSessionPathsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying reasoning paths for session {SessionId}", sessionId);

        var query = """
            MATCH path = (session:Session {sessionId: $sessionId})-[:EXECUTED]->(reasoning:ReasoningStep)-[:PRODUCED]->(atom:Atom)
            WITH path, reasoning, atom
            ORDER BY reasoning.stepNumber
            RETURN 
                collect(atom.atomId) as atomSequence,
                collect({
                    atomId: atom.atomId,
                    description: reasoning.operation,
                    chosenBranch: reasoning.branch
                }) as decisions,
                session.isSuccessful as isSuccessful
            """;

        var dependency = new DependencyTelemetry
        {
            Name = "Neo4j.GetSessionPaths",
            Type = "Neo4j",
            Target = _neo4jEndpoint,
            Data = $"Get reasoning paths for sessionId: {sessionId}"
        };
        dependency.Properties["SessionId"] = sessionId.ToString();
        dependency.Properties["Database"] = _database;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_database)
                .WithDefaultAccessMode(AccessMode.Read));

            var paths = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new { sessionId });

            var results = new List<ReasoningPath>();

            await foreach (var record in cursor)
            {
                var atomSequence = record["atomSequence"].As<List<long>>();
                var decisionsData = record["decisions"].As<List<Dictionary<string, object>>>();
                var isSuccessful = record["isSuccessful"].As<bool>();

                var decisions = decisionsData.Select(d => new DecisionPoint
                {
                    AtomId = Convert.ToInt64(d["atomId"]),
                    Description = d["description"].ToString() ?? "",
                    ChosenBranch = d["chosenBranch"]?.ToString()
                }).ToList();

                results.Add(new ReasoningPath
                {
                    PathId = Guid.NewGuid().ToString(),
                    SessionId = sessionId,
                    AtomSequence = atomSequence,
                    Decisions = decisions,
                    IsSuccessful = isSuccessful
                });
            }

            return results;
        });

            _logger.LogDebug("Session path query completed. Found {PathCount} reasoning paths.", paths.Count);

            dependency.Success = true;
            dependency.Metrics["PathCount"] = paths.Count;
            return paths;
        }
        catch (Exception ex)
        {
            dependency.Success = false;
            _logger.LogError(ex, "Failed to get session paths for sessionId {SessionId}", sessionId);
            throw;
        }
        finally
        {
            dependency.Duration = sw.Elapsed;
            _telemetry?.TrackDependency(dependency);
        }
    }

    public async Task<IEnumerable<AtomizationError>> GetSessionErrorsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying errors for session {SessionId}", sessionId);

        var query = """
            MATCH (error:Error)-[:OCCURRED_IN]->(session:Session {sessionId: $sessionId})
            RETURN 
                error.atomId as atomId,
                session.sessionId as sessionId,
                error.message as errorMessage,
                error.errorType as errorType,
                error.timestamp as timestamp,
                error.severity as severity
            ORDER BY error.timestamp DESC
            """;

        var dependency = new DependencyTelemetry
        {
            Name = "Neo4j.GetSessionErrors",
            Type = "Neo4j",
            Target = _neo4jEndpoint,
            Data = $"Get errors for sessionId: {sessionId}"
        };
        dependency.Properties["SessionId"] = sessionId.ToString();
        dependency.Properties["Database"] = _database;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_database)
                .WithDefaultAccessMode(AccessMode.Read));

            var errors = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, new { sessionId });

                var results = new List<AtomizationError>();

                await foreach (var record in cursor)
                {
                    var atomId = record["atomId"].As<long>();
                    var sessionIdResult = record["sessionId"].As<long>();
                    var errorMessage = record["errorMessage"].As<string>();
                    var errorType = record["errorType"].As<string>();
                    var timestamp = record["timestamp"].As<DateTime>();
                    var severity = record["severity"]?.As<string>();

                    results.Add(new AtomizationError
                    {
                        AtomId = atomId,
                        SessionId = sessionIdResult,
                        ErrorMessage = errorMessage,
                        ErrorType = errorType,
                        Timestamp = timestamp,
                        Severity = severity
                    });
                }

                return results;
            });

            _logger.LogDebug("Found {ErrorCount} errors for session.", errors.Count);

            dependency.Success = true;
            dependency.Metrics["ErrorCount"] = errors.Count;
            return errors;
        }
        catch (Exception ex)
        {
            dependency.Success = false;
            _logger.LogError(ex, "Failed to get errors for sessionId {SessionId}", sessionId);
            throw;
        }
        finally
        {
            dependency.Duration = sw.Elapsed;
            _telemetry?.TrackDependency(dependency);
        }
    }

    public async Task<IEnumerable<InfluenceRelationship>> GetInfluencesAsync(
        long atomId,
        int maxDepth = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding influence relationships for atom {AtomId} with max depth {MaxDepth}", atomId, maxDepth);

        var query = """
            MATCH path = (source:Atom)-[r:INFLUENCES|DERIVED_FROM*1..{maxDepth}]->(target:Atom {atomId: $atomId})
            WITH source, target, path, length(path) as depth, relationships(path)[0] as relationship
            RETURN 
                source.atomId as sourceAtomId,
                target.atomId as targetAtomId,
                type(relationship) as relationshipType,
                depth,
                1.0 / depth as weight
            ORDER BY weight DESC
            """;

        var dependency = new DependencyTelemetry
        {
            Name = "Neo4j.GetInfluences",
            Type = "Neo4j",
            Target = _neo4jEndpoint,
            Data = $"Find influence relationships for atomId: {atomId}, maxDepth: {maxDepth}"
        };
        dependency.Properties["AtomId"] = atomId.ToString();
        dependency.Properties["MaxDepth"] = maxDepth.ToString();
        dependency.Properties["Database"] = _database;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_database)
                .WithDefaultAccessMode(AccessMode.Read));

            var influences = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, new { atomId, maxDepth });

                var results = new List<InfluenceRelationship>();

                await foreach (var record in cursor)
                {
                    var sourceAtomId = record["sourceAtomId"].As<long>();
                    var targetAtomId = record["targetAtomId"].As<long>();
                    var relationshipType = record["relationshipType"].As<string>();
                    var depth = record["depth"].As<int>();
                    var weight = record["weight"].As<double>();

                    results.Add(new InfluenceRelationship
                    {
                        SourceAtomId = sourceAtomId,
                        TargetAtomId = targetAtomId,
                        RelationshipType = relationshipType,
                        Depth = depth,
                        Weight = weight
                    });
                }

                return results;
            });

            _logger.LogDebug("Found {InfluenceCount} influence relationships.", influences.Count);

            dependency.Success = true;
            dependency.Metrics["InfluenceCount"] = influences.Count;
            return influences;
        }
        catch (Exception ex)
        {
            dependency.Success = false;
            _logger.LogError(ex, "Failed to get influence relationships for atomId {AtomId}", atomId);
            throw;
        }
        finally
        {
            dependency.Duration = sw.Elapsed;
            _telemetry?.TrackDependency(dependency);
        }
    }

    public async Task<IEnumerable<AtomInfluence>> GetInfluencingAtomsAsync(
        long atomId,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var dependency = new DependencyTelemetry
        {
            Name = "Neo4j.GetInfluencingAtoms",
            Target = _neo4jEndpoint,
            Type = "Neo4j",
            Data = $"atomId: {atomId}"
        };

        try
        {
            _logger.LogDebug("Querying influencing atoms for {AtomId}", atomId);

            var session = _driver.AsyncSession(o => o.WithDatabase(_database));
            await using (session.ConfigureAwait(false))
            {
                var results = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync("""
                        MATCH (influencing:Atom)-[r:INFLUENCES]->(atom:Atom {atomId: $atomId})
                        RETURN influencing.atomId as atomId, 
                               r.weight as weight, 
                               r.influenceType as influenceType,
                               length(shortestPath((influencing)-[*]->(atom))) as pathLength
                        ORDER BY r.weight DESC
                        """,
                        new { atomId });

                    var influences = new List<AtomInfluence>();
                    await foreach (var record in cursor.ConfigureAwait(false))
                    {
                        influences.Add(new AtomInfluence
                        {
                            AtomId = record["atomId"].As<long>(),
                            Weight = record["weight"].As<double>(),
                            InfluenceType = record["influenceType"].As<string>(),
                            PathLength = record["pathLength"].As<int>()
                        });
                    }

                    return influences;
                });

                _logger.LogDebug("Found {InfluenceCount} influencing atoms.", results.Count());

                dependency.Success = true;
                dependency.Metrics["InfluenceCount"] = results.Count();
                return results;
            }
        }
        catch (Exception ex)
        {
            dependency.Success = false;
            _logger.LogError(ex, "Failed to get influencing atoms for atomId {AtomId}", atomId);
            throw;
        }
        finally
        {
            dependency.Duration = sw.Elapsed;
            _telemetry?.TrackDependency(dependency);
        }
    }
}
