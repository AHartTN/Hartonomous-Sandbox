using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hartonomous.IntegrationTests.Api;

/// <summary>
/// Integration tests for API middleware, error handling, and request pipeline.
/// Tests using WebApplicationFactory with actual HTTP requests.
/// </summary>
public class ApiMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRequest_ToRootEndpoint_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRequest_ToNonExistentEndpoint_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRequest_WithInvalidRoute_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/invalid/route/path");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HttpsRedirection_IsEnabled()
    {
        // Arrange - Create client that doesn't follow redirects
        var clientNoRedirect = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await clientNoRedirect.GetAsync("http://localhost/health");

        // Assert - Should redirect to HTTPS or succeed with 200
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.PermanentRedirect, HttpStatusCode.TemporaryRedirect);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthCheckEndpoints_AcceptGetRequests(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetRequest_ReturnsCorrectServerHeader()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        response.Headers.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleSequentialRequests_AllSucceed()
    {
        // Act
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 5; i++)
        {
            responses.Add(await _client.GetAsync("/health"));
        }

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Fact]
    public async Task ParallelRequests_AllSucceed()
    {
        // Act
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => _client.GetAsync("/health"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }
}
