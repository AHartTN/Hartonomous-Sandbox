using FluentAssertions;
using Hartonomous.Api.Controllers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Api.Controllers;

/// <summary>
/// Tests for ReasoningController - semantic reasoning and similarity queries.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Controller")]
public class ReasoningControllerTests : UnitTestBase
{
    public ReasoningControllerTests(ITestOutputHelper output) : base(output) { }

    #region Semantic Search Tests

    [Fact]
    public async Task SemanticSearch_ValidQuery_ReturnsResults()
    {
        // Arrange
        var controller = CreateReasoningController();
        var query = "test search query";

        // Act
        var result = await controller.SemanticSearch(query, topK: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SemanticSearch_EmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateReasoningController();

        // Act
        var result = await controller.SemanticSearch("", topK: 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Find Similar Tests

    [Fact]
    public async Task FindSimilar_ValidAtomId_ReturnsSimilar()
    {
        // Arrange
        var controller = CreateReasoningController();

        // Act
        var result = await controller.FindSimilar(1, threshold: 0.7f, maxResults: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task FindSimilar_InvalidAtomId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateReasoningController();

        // Act
        var result = await controller.FindSimilar(-1, threshold: 0.7f, maxResults: 10);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Spatial Search Tests

    [Fact]
    public async Task SpatialSearch_ValidCoordinates_ReturnsResults()
    {
        // Arrange
        var controller = CreateReasoningController();

        // Act
        var result = await controller.SpatialSearch(x: 0.5, y: 0.5, z: 0.5, radius: 1.0, maxResults: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Helper Methods

    private ReasoningController CreateReasoningController()
    {
        return new ReasoningController(
            CreateMockReasoningService(),
            CreateLogger<ReasoningController>());
    }

    private object CreateMockReasoningService()
    {
        return new object(); // Mock service
    }

    #endregion
}
