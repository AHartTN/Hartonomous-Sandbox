using FluentAssertions;
using Hartonomous.Core.Services;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Core.Services;

/// <summary>
/// Tests for SpatialSearchService - 3D spatial search and navigation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Service")]
[Trait("Category", "Spatial")]
public class SpatialSearchServiceTests : UnitTestBase
{
    public SpatialSearchServiceTests(ITestOutputHelper output) : base(output) { }

    #region Spatial Search Tests

    [Fact]
    public async Task SearchByCoordinatesAsync_ValidCoordinates_ReturnsNearbyAtoms()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateSpatialSearchService(context);

        // Act
        var results = await service.SearchByCoordinatesAsync(
            x: 0.5, 
            y: 0.5, 
            z: 0.5, 
            radius: 1.0, 
            maxResults: 10, 
            tenantId: 1);

        // Assert
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchByCoordinatesAsync_NegativeRadius_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateSpatialSearchService(context);

        // Act
        Func<Task> act = async () => await service.SearchByCoordinatesAsync(
            x: 0, y: 0, z: 0, radius: -1.0, maxResults: 10, tenantId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Nearest Neighbors Tests

    [Fact]
    public async Task FindNearestNeighborsAsync_ValidAtomId_ReturnsNeighbors()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateSpatialSearchService(context);

        // Act
        var results = await service.FindNearestNeighborsAsync(
            atomId: 1, 
            k: 10, 
            tenantId: 1);

        // Assert
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task FindNearestNeighborsAsync_InvalidK_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateSpatialSearchService(context);

        // Act
        Func<Task> act = async () => await service.FindNearestNeighborsAsync(
            atomId: 1, k: 0, tenantId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Distance Calculation Tests

    [Fact]
    public void CalculateEuclideanDistance_SamePoint_ReturnsZero()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateSpatialSearchService(context);
        var point = new[] { 1.0, 2.0, 3.0 };

        // Act
        var distance = service.CalculateEuclideanDistance(point, point);

        // Assert
        distance.Should().Be(0.0);
    }

    [Fact]
    public void CalculateEuclideanDistance_DifferentPoints_ReturnsCorrectDistance()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateSpatialSearchService(context);
        var point1 = new[] { 0.0, 0.0, 0.0 };
        var point2 = new[] { 1.0, 0.0, 0.0 };

        // Act
        var distance = service.CalculateEuclideanDistance(point1, point2);

        // Assert
        distance.Should().BeApproximately(1.0, 0.0001);
    }

    #endregion

    #region Helper Methods

    private ISpatialSearchService CreateSpatialSearchService(HartonomousDbContext context)
    {
        return new SpatialSearchService(
            context,
            CreateLogger<SpatialSearchService>());
    }

    #endregion
}
