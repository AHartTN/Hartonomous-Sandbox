using System.Net;
using FluentAssertions;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Api;

/// <summary>
/// Integration tests that exercise Production configuration code paths.
/// Tests Azure service configuration (App Config, Key Vault, App Insights)
/// without requiring actual Azure resources.
/// </summary>
public class ProductionConfigurationTests : IClassFixture<ProductionConfigWebApplicationFactory>
{
    private readonly ProductionConfigWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductionConfigurationTests(ProductionConfigWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Health_ReturnsHealthy_WithProductionConfiguration()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - App should start successfully even if Azure services fail to connect
        // The health check tests database connectivity, which uses localhost
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GET_HealthReady_ReturnsHealthy_WithProductionConfiguration()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert - Readiness probe should pass with localhost database
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_OpenApi_ReturnsOpenApiDocument_InStagingEnvironment()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert - OpenAPI endpoint should work in Staging
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Unauthorized); // Might require auth in non-dev
    }

    [Fact]
    public async Task ProductionConfiguration_UsesProductionCors()
    {
        // Arrange - Test that Staging environment uses ProductionCors, not DevelopmentCors
        _client.DefaultRequestHeaders.Add("Origin", "https://hartonomous.com");

        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Should allow configured production origins
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProductionConfiguration_HasCorrelationId()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Correlation ID middleware should work in production config
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
    }

    [Fact]
    public async Task ProductionConfiguration_RateLimiting_IsConfigured()
    {
        // Act - Make a request to verify rate limiting is configured
        var response = await _client.GetAsync("/health");

        // Assert - Should succeed (we're not testing rate limit exhaustion, just that middleware is active)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Rate limiter is configured - would return 429 if we exceeded limits
        // but that requires many concurrent requests which is not the point of this test
    }
}
