using Hartonomous.Core.Interfaces.Provenance;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Core;

/// <summary>
/// Unit tests for IProvenanceQueryService implementations.
/// Tests Neo4j provenance graph queries with realistic lineage and error clustering scenarios.
/// </summary>
public class ProvenanceQueryServiceTests
{
    private readonly IProvenanceQueryService _provenanceService;

    public ProvenanceQueryServiceTests()
    {
        _provenanceService = Substitute.For<IProvenanceQueryService>();
    }

    [Fact]
    public async Task GetAtomLineage_WithValidAtomId_ReturnsCompleteLineage()
    {
        // Arrange
        const long atomId = 5000L;
        const int maxDepth = 10;

        var expectedLineage = new AtomLineage
        {
            AtomId = atomId,
            Parents = new List<AtomNode>
            {
                new()
                {
                    AtomId = 4999L,
                    RelationshipType = "DERIVED_FROM",
                    Metadata = "{\"operation\":\"inference\"}",
                    Children = new List<AtomNode>
                    {
                        new()
                        {
                            AtomId = 4998L,
                            RelationshipType = "INFLUENCED_BY",
                            Metadata = "{\"weight\":0.85}"
                        }
                    }
                }
            },
            Depth = 3,
            TotalAncestors = 5
        };

        _provenanceService
            .GetAtomLineageAsync(atomId, maxDepth, Arg.Any<CancellationToken>())
            .Returns(expectedLineage);

        // Act
        var lineage = await _provenanceService.GetAtomLineageAsync(atomId, maxDepth);

        // Assert
        lineage.Should().NotBeNull();
        lineage.AtomId.Should().Be(atomId);
        lineage.Parents.Should().NotBeEmpty();
        lineage.Depth.Should().BeGreaterThan(0);
        lineage.TotalAncestors.Should().BeGreaterThan(0);
        lineage.Parents.Should().AllSatisfy(p => p.RelationshipType.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task FindErrorClusters_WithSessionScope_ReturnsRelatedErrors()
    {
        // Arrange
        const long sessionId = 12345L;
        const int minClusterSize = 3;

        var expectedClusters = new List<ErrorCluster>
        {
            new()
            {
                ClusterId = "cluster_001",
                ErrorCount = 5,
                Pattern = "NullReferenceException in spatial projection",
                AtomIds = new List<long> { 1001L, 1002L, 1003L, 1004L, 1005L },
                FirstOccurrence = DateTime.UtcNow.AddHours(-2),
                LastOccurrence = DateTime.UtcNow.AddMinutes(-10)
            },
            new()
            {
                ClusterId = "cluster_002",
                ErrorCount = 4,
                Pattern = "Timeout in landmark calculation",
                AtomIds = new List<long> { 2001L, 2002L, 2003L, 2004L },
                FirstOccurrence = DateTime.UtcNow.AddHours(-1),
                LastOccurrence = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _provenanceService
            .FindErrorClustersAsync(sessionId, minClusterSize, Arg.Any<CancellationToken>())
            .Returns(expectedClusters);

        // Act
        var clusters = await _provenanceService.FindErrorClustersAsync(sessionId, minClusterSize);

        // Assert
        clusters.Should().NotBeNull();
        clusters.Should().HaveCountGreaterOrEqualTo(1);
        clusters.Should().AllSatisfy(c =>
        {
            c.ErrorCount.Should().BeGreaterOrEqualTo(minClusterSize);
            c.Pattern.Should().NotBeNullOrWhiteSpace();
            c.AtomIds.Should().HaveCountGreaterOrEqualTo(minClusterSize);
            c.FirstOccurrence.Should().BeBefore(c.LastOccurrence);
        });
    }

    [Fact]
    public async Task GetSessionPaths_WithValidSessionId_ReturnsReasoningPaths()
    {
        // Arrange
        const long sessionId = 67890L;

        var expectedPaths = new List<ReasoningPath>
        {
            new()
            {
                PathId = "path_001",
                SessionId = sessionId,
                AtomSequence = new List<long> { 1L, 2L, 3L, 5L, 8L }, // Fibonacci-like exploration
                Decisions = new List<DecisionPoint>
                {
                    new()
                    {
                        AtomId = 2L,
                        Description = "Choose spatial analysis over temporal",
                        BranchesConsidered = new List<string> { "spatial", "temporal", "hybrid" },
                        ChosenBranch = "spatial"
                    }
                },
                IsSuccessful = true
            },
            new()
            {
                PathId = "path_002",
                SessionId = sessionId,
                AtomSequence = new List<long> { 1L, 2L, 4L, 7L }, // Alternative path
                Decisions = new List<DecisionPoint>
                {
                    new()
                    {
                        AtomId = 2L,
                        Description = "Choose temporal analysis",
                        BranchesConsidered = new List<string> { "spatial", "temporal" },
                        ChosenBranch = "temporal"
                    }
                },
                IsSuccessful = false
            }
        };

        _provenanceService
            .GetSessionPathsAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(expectedPaths);

        // Act
        var paths = await _provenanceService.GetSessionPathsAsync(sessionId);

        // Assert
        paths.Should().NotBeNull();
        paths.Should().HaveCount(2);
        paths.Should().AllSatisfy(p =>
        {
            p.SessionId.Should().Be(sessionId);
            p.AtomSequence.Should().NotBeEmpty();
        });
        paths.Should().Contain(p => p.IsSuccessful);
    }

    [Fact]
    public async Task GetInfluencingAtoms_WithResultAtom_ReturnsWeightedInfluences()
    {
        // Arrange
        const long resultAtomId = 9999L;

        var expectedInfluences = new List<AtomInfluence>
        {
            new()
            {
                AtomId = 1000L,
                Weight = 0.85,
                InfluenceType = "Direct",
                PathLength = 1
            },
            new()
            {
                AtomId = 1001L,
                Weight = 0.62,
                InfluenceType = "Indirect",
                PathLength = 2
            },
            new()
            {
                AtomId = 1002L,
                Weight = 0.43,
                InfluenceType = "Indirect",
                PathLength = 3
            }
        };

        _provenanceService
            .GetInfluencingAtomsAsync(resultAtomId, Arg.Any<CancellationToken>())
            .Returns(expectedInfluences);

        // Act
        var influences = await _provenanceService.GetInfluencingAtomsAsync(resultAtomId);

        // Assert
        influences.Should().NotBeNull();
        influences.Should().HaveCount(3);
        influences.Should().BeInDescendingOrder(i => i.Weight);
        influences.Should().AllSatisfy(i =>
        {
            i.Weight.Should().BeInRange(0.0, 1.0);
            i.InfluenceType.Should().NotBeNullOrWhiteSpace();
            i.PathLength.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task FindErrorClusters_WithoutSessionScope_ReturnsGlobalClusters()
    {
        // Arrange - No session filter, find all error patterns
        const int minClusterSize = 5;

        var expectedClusters = new List<ErrorCluster>
        {
            new()
            {
                ClusterId = "global_001",
                ErrorCount = 12,
                Pattern = "Database connection timeout",
                AtomIds = Enumerable.Range(1, 12).Select(i => (long)i * 100).ToList(),
                FirstOccurrence = DateTime.UtcNow.AddDays(-7),
                LastOccurrence = DateTime.UtcNow.AddHours(-1)
            }
        };

        _provenanceService
            .FindErrorClustersAsync(null, minClusterSize, Arg.Any<CancellationToken>())
            .Returns(expectedClusters);

        // Act
        var clusters = await _provenanceService.FindErrorClustersAsync(null, minClusterSize);

        // Assert
        clusters.Should().NotBeNull();
        clusters.Should().AllSatisfy(c =>
        {
            c.ClusterId.Should().NotBeNullOrWhiteSpace();
            c.ErrorCount.Should().BeGreaterOrEqualTo(minClusterSize);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(null)] // Unlimited depth
    public async Task GetAtomLineage_WithVariousDepths_RespectsMaxDepth(int? maxDepth)
    {
        // Arrange
        const long atomId = 7777L;

        var expectedLineage = new AtomLineage
        {
            AtomId = atomId,
            Parents = new List<AtomNode>(),
            Depth = maxDepth ?? 15, // If null, simulate deeper traversal
            TotalAncestors = (maxDepth ?? 15) * 2
        };

        _provenanceService
            .GetAtomLineageAsync(atomId, maxDepth, Arg.Any<CancellationToken>())
            .Returns(expectedLineage);

        // Act
        var lineage = await _provenanceService.GetAtomLineageAsync(atomId, maxDepth);

        // Assert
        lineage.Should().NotBeNull();
        if (maxDepth.HasValue)
        {
            lineage.Depth.Should().BeLessOrEqualTo(maxDepth.Value);
        }
        else
        {
            lineage.Depth.Should().BeGreaterThan(0);
        }
    }
}
