using FluentAssertions;
using Hartonomous.Core.Services;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Core.Services;

/// <summary>
/// Tests for ProvenanceService - provenance tracking and lineage management.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Service")]
public class ProvenanceServiceTests : UnitTestBase
{
    public ProvenanceServiceTests(ITestOutputHelper output) : base(output) { }

    #region Link Provenance Tests

    [Fact]
    public async Task LinkProvenanceAsync_ValidAtoms_CreatesLink()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateProvenanceService(context);

        // Act
        await service.LinkProvenanceAsync(sourceAtomId: 1, derivedAtomId: 2, "derived_from");

        // Assert
        var link = await context.ProvenanceLinks.FindAsync(1);
        link.Should().NotBeNull();
    }

    [Fact]
    public async Task LinkProvenanceAsync_NullType_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateProvenanceService(context);

        // Act
        Func<Task> act = async () => await service.LinkProvenanceAsync(1, 2, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Query Lineage Tests

    [Fact]
    public async Task QueryLineageAsync_ValidAtom_ReturnsAncestors()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateProvenanceService(context);

        // Act
        var ancestors = await service.QueryLineageAsync(atomId: 1, direction: "ancestors", maxDepth: 10);

        // Assert
        ancestors.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryLineageAsync_InvalidDirection_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateProvenanceService(context);

        // Act
        Func<Task> act = async () => await service.QueryLineageAsync(1, "invalid", 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Find Impacted Tests

    [Fact]
    public async Task FindImpactedAtomsAsync_ValidAtom_ReturnsImpacted()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateProvenanceService(context);

        // Act
        var impacted = await service.FindImpactedAtomsAsync(sourceAtomId: 1, maxDepth: 5);

        // Assert
        impacted.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private IProvenanceService CreateProvenanceService(HartonomousDbContext context)
    {
        return new ProvenanceService(context, CreateLogger<ProvenanceService>());
    }

    #endregion
}
