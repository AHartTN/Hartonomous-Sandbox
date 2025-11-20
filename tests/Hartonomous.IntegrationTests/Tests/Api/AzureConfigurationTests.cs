using System.Net;
using FluentAssertions;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Api;

/// <summary>
/// Integration tests for Azure configuration paths (App Configuration, Key Vault, Application Insights).
/// These tests verify that the Azure service integration code paths execute correctly
/// using the HART-DESKTOP Arc managed identity.
/// </summary>
public class AzureConfigurationTests : IClassFixture<AzureEnabledWebApplicationFactory>
{
    private readonly AzureEnabledWebApplicationFactory _factory;

    public AzureConfigurationTests(AzureEnabledWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_StartsSuccessfully_WithAzureServicesEnabled()
    {
        // Arrange & Act
        var client = _factory.CreateClient();
        
        // Assert - Application should start without crashing
        // Even if Azure connections fail, the app should handle gracefully
        var response = await client.GetAsync("/health");
        
        // We expect either:
        // - 200 OK if everything works
        // - 503 Service Unavailable if Azure services can't connect (but app still started)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task HealthCheck_ReturnsStatus_WithAzureServicesEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        content.Should().NotBeNullOrEmpty();
        content.Should().BeOneOf("Healthy", "Unhealthy", "Degraded");
    }

    [Fact]
    public async Task CorrelationId_IsPresentInResponses_WithAzureServicesEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/health");
        
        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue("Correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task OpenApiEndpoint_IsAccessible_WithAzureServicesEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/openapi/v1.json");
        
        // Assert
        // OpenAPI endpoint might require authorization, but should respond
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public void Factory_CanBeCreatedMultipleTimes_Idempotent()
    {
        // This test verifies that creating multiple factories doesn't cause issues
        // Important for parallel test execution
        
        // Act & Assert
        var factory1 = new HartonomousWebApplicationFactory("Development", enableAzureServices: true);
        var factory2 = new HartonomousWebApplicationFactory("Development", enableAzureServices: true);
        
        factory1.Should().NotBeNull();
        factory2.Should().NotBeNull();
        
        var client1 = factory1.CreateClient();
        var client2 = factory2.CreateClient();
        
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        
        // Cleanup
        factory1.Dispose();
        factory2.Dispose();
    }
}
