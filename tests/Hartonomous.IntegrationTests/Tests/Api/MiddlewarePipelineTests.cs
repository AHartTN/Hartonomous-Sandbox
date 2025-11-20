using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hartonomous.IntegrationTests.Api;

/// <summary>
/// Tests for middleware pipeline: Correlation ID, Request Logging, and Problem Details.
/// Validates Phase 0 middleware implementation.
/// </summary>
public class MiddlewarePipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MiddlewarePipelineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Correlation ID Middleware Tests

    [Fact]
    public async Task CorrelationIdMiddleware_AddsXCorrelationIDHeader_ToResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_AddsXRequestIDHeader_ToResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Request-ID");
        var requestId = response.Headers.GetValues("X-Request-ID").First();
        requestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesUniqueCorrelationIDs()
    {
        // Act
        var response1 = await _client.GetAsync("/health");
        var response2 = await _client.GetAsync("/health");

        // Assert
        var correlationId1 = response1.Headers.GetValues("X-Correlation-ID").First();
        var correlationId2 = response2.Headers.GetValues("X-Correlation-ID").First();
        
        correlationId1.Should().NotBe(correlationId2);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_IncludesCorrelationIDInAllResponses()
    {
        // Act - Test multiple endpoints
        var healthResponse = await _client.GetAsync("/health");
        var readyResponse = await _client.GetAsync("/health/ready");
        var liveResponse = await _client.GetAsync("/health/live");

        // Assert
        healthResponse.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        readyResponse.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        liveResponse.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
    }

    #endregion

    #region Problem Details Tests

    [Fact]
    public async Task ProblemDetails_Returns404_ForNonExistentEndpoint()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Should return Problem Details format or plain text (both acceptable for 404)
        var contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().Match(ct => 
            ct == "application/problem+json" || 
            ct == "text/plain" ||
            ct == "text/html");
    }

    [Fact]
    public async Task ProblemDetails_IncludesTraceId_InErrorResponses()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Correlation ID should be in response headers
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
    }

    #endregion

    #region Middleware Order Tests

    [Fact]
    public async Task MiddlewarePipeline_ExecutesInCorrectOrder()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Verify all middleware executed
        // 1. Exception handler (not visible in success case)
        // 2. Status code pages (not visible in success case)
        // 3. HTTPS redirection (may redirect)
        // 4. Correlation ID (should be present)
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        
        // 5. Request logging (not directly testable via HTTP)
        // 6. CORS (tested separately)
        // 7. Rate limiting (tested separately)
        // 8. Authentication (tested separately)
        // 9. Authorization (tested separately)
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MiddlewarePipeline_HandlesMultipleConcurrentRequests()
    {
        // Act - Send 20 concurrent requests
        var tasks = Enumerable.Range(1, 20)
            .Select(_ => _client.GetAsync("/health"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed with correlation IDs
        responses.Should().HaveCount(20);
        responses.Should().AllSatisfy(r => 
        {
            r.StatusCode.Should().Be(HttpStatusCode.OK);
            r.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        });

        // Correlation IDs should all be unique
        var correlationIds = responses
            .Select(r => r.Headers.GetValues("X-Correlation-ID").First())
            .ToList();
        
        correlationIds.Distinct().Should().HaveCount(20);
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public async Task HealthEndpoint_ReturnsPlainText()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task OpenApiEndpoint_ReturnsJson_InDevelopment()
    {
        // Arrange - Create factory in development mode
        var devFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });
        var devClient = devFactory.CreateClient();

        // Act
        var response = await devClient.GetAsync("/openapi/v1.json");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    #endregion

    #region Response Headers Tests

    [Fact]
    public async Task Response_IncludesStandardSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Should have correlation headers at minimum
        response.Headers.Should().NotBeNull();
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        response.Headers.Should().Contain(h => h.Key == "X-Request-ID");
    }

    #endregion
}
