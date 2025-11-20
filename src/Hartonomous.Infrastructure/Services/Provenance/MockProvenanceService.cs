using Hartonomous.Core.Interfaces.Provenance;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Provenance;

/// <summary>
/// Mock implementation of provenance query service for marketing demonstrations.
/// Returns realistic sample data without requiring Neo4j connectivity.
/// </summary>
public class MockProvenanceService : IProvenanceQueryService
{
    private readonly ILogger<MockProvenanceService> _logger;

    public MockProvenanceService(ILogger<MockProvenanceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AtomLineage> GetAtomLineageAsync(
        long atomId,
        int? maxDepth = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MockProvenanceService: Generating atom lineage for atom {AtomId} with max depth {MaxDepth}",
            atomId,
            maxDepth);

        await Task.Delay(50, cancellationToken);

        return new AtomLineage
        {
            AtomId = atomId,
            Parents = new List<AtomNode>
            {
                new()
                {
                    AtomId = atomId - 1,
                    RelationshipType = "DERIVED_FROM",
                    Metadata = "{\"type\":\"transformation\",\"confidence\":0.92}",
                    Children = new List<AtomNode>
                    {
                        new()
                        {
                            AtomId = atomId - 5,
                            RelationshipType = "INFLUENCED_BY",
                            Metadata = "{\"type\":\"source\",\"confidence\":0.85}"
                        }
                    }
                },
                new()
                {
                    AtomId = atomId - 2,
                    RelationshipType = "VALIDATED_BY",
                    Metadata = "{\"type\":\"validation\",\"confidence\":0.96}"
                }
            },
            Depth = maxDepth ?? 5,
            TotalAncestors = 14
        };
    }

    public async Task<IEnumerable<ErrorCluster>> FindErrorClustersAsync(
        long? sessionId = null,
        int minClusterSize = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MockProvenanceService: Finding error clusters (sessionId: {SessionId}, minSize: {MinSize})",
            sessionId,
            minClusterSize);

        await Task.Delay(75, cancellationToken);

        return new List<ErrorCluster>
        {
            new()
            {
                ClusterId = "cluster_semantic_001",
                ErrorCount = 23,
                Pattern = "Semantic ambiguity in contextual inference",
                AtomIds = Enumerable.Range((int)(sessionId ?? 1000), 23).Select(x => (long)x).ToList(),
                FirstOccurrence = DateTime.UtcNow.AddDays(-5),
                LastOccurrence = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                ClusterId = "cluster_confidence_002",
                ErrorCount = 17,
                Pattern = "Low confidence threshold violations",
                AtomIds = Enumerable.Range((int)(sessionId ?? 2000), 17).Select(x => (long)x).ToList(),
                FirstOccurrence = DateTime.UtcNow.AddDays(-3),
                LastOccurrence = DateTime.UtcNow.AddHours(-8)
            }
        };
    }

    public async Task<IEnumerable<ReasoningPath>> GetSessionPathsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MockProvenanceService: Retrieving reasoning paths for session {SessionId}",
            sessionId);

        await Task.Delay(60, cancellationToken);

        return new List<ReasoningPath>
        {
            new()
            {
                PathId = $"path_{sessionId}_1",
                SessionId = sessionId,
                AtomSequence = new List<long> { sessionId * 10, sessionId * 10 + 1, sessionId * 10 + 2, sessionId * 10 + 3 },
                Decisions = new List<DecisionPoint>
                {
                    new()
                    {
                        AtomId = sessionId * 10 + 1,
                        Description = "Choose semantic path over syntactic",
                        BranchesConsidered = new List<string> { "semantic", "syntactic", "hybrid" },
                        ChosenBranch = "semantic"
                    },
                    new()
                    {
                        AtomId = sessionId * 10 + 2,
                        Description = "Confidence threshold check",
                        BranchesConsidered = new List<string> { "proceed", "backtrack", "branch" },
                        ChosenBranch = "proceed"
                    }
                },
                IsSuccessful = true
            },
            new()
            {
                PathId = $"path_{sessionId}_2",
                SessionId = sessionId,
                AtomSequence = new List<long> { sessionId * 10, sessionId * 10 + 5 },
                Decisions = new List<DecisionPoint>
                {
                    new()
                    {
                        AtomId = sessionId * 10 + 5,
                        Description = "Early termination - low confidence",
                        BranchesConsidered = new List<string> { "continue", "terminate" },
                        ChosenBranch = "terminate"
                    }
                },
                IsSuccessful = false
            }
        };
    }

    public async Task<IEnumerable<AtomizationError>> GetSessionErrorsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MockProvenanceService: Retrieving errors for session {SessionId}",
            sessionId);

        await Task.Delay(45, cancellationToken);

        return new List<AtomizationError>
        {
            new()
            {
                AtomId = sessionId * 10 + 1,
                SessionId = sessionId,
                ErrorMessage = "Semantic ambiguity detected in inference context",
                ErrorType = "SemanticError",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Severity = "Warning"
            },
            new()
            {
                AtomId = sessionId * 10 + 3,
                SessionId = sessionId,
                ErrorMessage = "Low confidence threshold violation",
                ErrorType = "ConfidenceError",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Severity = "Error"
            }
        };
    }

    public async Task<IEnumerable<InfluenceRelationship>> GetInfluencesAsync(
        long atomId,
        int maxDepth = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MockProvenanceService: Finding influence relationships for atom {AtomId} with max depth {MaxDepth}",
            atomId,
            maxDepth);

        await Task.Delay(50, cancellationToken);

        return new List<InfluenceRelationship>
        {
            new()
            {
                SourceAtomId = atomId - 3,
                TargetAtomId = atomId,
                RelationshipType = "DERIVED_FROM",
                Depth = 1,
                Weight = 0.87
            },
            new()
            {
                SourceAtomId = atomId - 7,
                TargetAtomId = atomId,
                RelationshipType = "INFLUENCED_BY",
                Depth = 2,
                Weight = 0.65
            },
            new()
            {
                SourceAtomId = atomId - 15,
                TargetAtomId = atomId,
                RelationshipType = "VALIDATED_BY",
                Depth = 3,
                Weight = 0.42
            }
        };
    }

    public async Task<IEnumerable<AtomInfluence>> GetInfluencingAtomsAsync(
        long atomId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MockProvenanceService: Generating influencing atoms for atom {AtomId}",
            atomId);

        await Task.Delay(30, cancellationToken);

        return new List<AtomInfluence>
        {
            new()
            {
                AtomId = atomId - 1,
                Weight = 0.92,
                InfluenceType = "Direct",
                PathLength = 1
            },
            new()
            {
                AtomId = atomId - 3,
                Weight = 0.78,
                InfluenceType = "Direct",
                PathLength = 1
            },
            new()
            {
                AtomId = atomId - 7,
                Weight = 0.65,
                InfluenceType = "Indirect",
                PathLength = 2
            },
            new()
            {
                AtomId = atomId - 15,
                Weight = 0.43,
                InfluenceType = "Indirect",
                PathLength = 3
            }
        };
    }
}
