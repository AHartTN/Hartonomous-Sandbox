using FluentAssertions;
using Hartonomous.Api.Controllers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Api.Controllers;

/// <summary>
/// Tests for ProvenanceController - provenance tracking and lineage queries.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Controller")]
public class ProvenanceControllerTests : UnitTestBase
{
    public ProvenanceControllerTests(ITestOutputHelper output) : base(output) { }

    #region Link Provenance Tests

    [Fact]
    public async Task LinkProvenance_ValidAtoms_ReturnsOk()
    {
        // Arrange
        var controller = CreateProvenanceController();
        var sourceAtomId = 1;
        var derivedAtomId = 2;

        // Act
        var result = await controller.LinkProvenance(sourceAtomId, derivedAtomId);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task LinkProvenance_InvalidSourceId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateProvenanceController();

        // Act
        var result = await controller.LinkProvenance(-1, 2);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Query Lineage Tests

    [Fact]
    public async Task QueryLineage_ValidAtomId_ReturnsLineage()
    {
        // Arrange
        var controller = CreateProvenanceController();
        var atomId = 1;

        // Act
        var result = await controller.QueryLineage(atomId, "ancestors", maxDepth: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task QueryLineage_InvalidDirection_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateProvenanceController();

        // Act
        var result = await controller.QueryLineage(1, "invalid", maxDepth: 10);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Find Impacted Tests

    [Fact]
    public async Task FindImpactedAtoms_ValidAtomId_ReturnsImpacted()
    {
        // Arrange
        var controller = CreateProvenanceController();

        // Act
        var result = await controller.FindImpactedAtoms(1, maxDepth: 5);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Helper Methods

    private ProvenanceController CreateProvenanceController()
    {
        return new ProvenanceController(
            CreateMockProvenanceService(),
            CreateLogger<ProvenanceController>());
    }

    private object CreateMockProvenanceService()
    {
        return new object(); // Mock service
    }

    #endregion
}
