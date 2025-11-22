using FluentAssertions;
using Hartonomous.Core.Services;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Core.Services;

/// <summary>
/// Tests for ReasoningService - semantic reasoning and similarity search.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Service")]
public class ReasoningServiceTests : UnitTestBase
{
    public ReasoningServiceTests(ITestOutputHelper output) : base(output) { }

    #region Semantic Search Tests

    [Fact]
    public async Task SemanticSearchAsync_ValidQuery_ReturnsResults()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateReasoningService(context);

        // Act
        var results = await service.SemanticSearchAsync("test query", topK: 10, tenantId: 1);

        // Assert
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task SemanticSearchAsync_EmptyQuery_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateReasoningService(context);

        // Act
        Func<Task> act = async () => await service.SemanticSearchAsync("", topK: 10, tenantId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Find Similar Tests

    [Fact]
    public async Task FindSimilarAsync_ValidAtom_ReturnsSimilar()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateReasoningService(context);

        // Act
        var similar = await service.FindSimilarAsync(atomId: 1, threshold: 0.7f, maxResults: 10, tenantId: 1);

        // Assert
        similar.Should().NotBeNull();
    }

    [Fact]
    public async Task FindSimilarAsync_InvalidThreshold_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateReasoningService(context);

        // Act
        Func<Task> act = async () => await service.FindSimilarAsync(1, threshold: 1.5f, maxResults: 10, tenantId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Spatial Search Tests

    [Fact]
    public async Task SpatialSearchAsync_ValidCoordinates_ReturnsResults()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateReasoningService(context);

        // Act
        var results = await service.SpatialSearchAsync(x: 0.5, y: 0.5, z: 0.5, radius: 1.0, maxResults: 10, tenantId: 1);

        // Assert
        results.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private IReasoningService CreateReasoningService(HartonomousDbContext context)
    {
        return new ReasoningService(context, CreateLogger<ReasoningService>());
    }

    #endregion
}
