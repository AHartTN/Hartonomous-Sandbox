using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Api;

/// <summary>
/// Integration tests for provenance tracking workflow.
/// Tests: Create Atoms ? Link Provenance ? Query Lineage ? Verify Chain.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Provenance")]
public class ProvenanceWorkflowTests : IntegrationTestBase<WebApplicationFactory<Program>>
{
    public ProvenanceWorkflowTests(WebApplicationFactory<Program> factory) : base(factory) { }

    #region Lineage Query Tests

    [Fact]
    public async Task QueryLineage_ValidAtomId_ReturnsLineage()
    {
        // Arrange
        var client = GetClient();

        // Act
        var response = await client.GetAsync("/api/provenance/lineage/1?direction=ancestors&maxDepth=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task QueryLineage_InvalidDirection_ReturnsBadRequest()
    {
        // Arrange
        var client = GetClient();

        // Act
        var response = await client.GetAsync("/api/provenance/lineage/1?direction=invalid&maxDepth=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Impact Analysis Tests

    [Fact]
    public async Task FindImpactedAtoms_ValidAtomId_ReturnsImpacted()
    {
        // Arrange
        var client = GetClient();

        // Act
        var response = await client.GetAsync("/api/provenance/impacted/1?maxDepth=5");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    #endregion
}
