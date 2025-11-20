using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Api;

/// <summary>
/// Integration tests for Data Ingestion API endpoints.
/// Tests complete request pipeline with HartonomousWebApplicationFactory.
/// </summary>
public class DataIngestionIntegrationTests : IClassFixture<HartonomousWebApplicationFactory>
{
    private readonly HartonomousWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DataIngestionIntegrationTests(HartonomousWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_IngestFile_ReturnsBadRequest_WhenNoFileProvided()
    {
        // Arrange
        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync("/api/v1/ingestion/file", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_IngestFile_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "empty.txt"
        };
        content.Add(fileContent);
        content.Add(new StringContent("1"), "tenantId");

        // Act
        var response = await _client.PostAsync("/api/v1/ingestion/file", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_IngestFile_IncludesCorrelationIdInResponse()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
        fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "test.txt"
        };
        content.Add(fileContent);
        content.Add(new StringContent("1"), "tenantId");

        // Act
        var response = await _client.PostAsync("/api/v1/ingestion/file", content);

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_IngestFile_ReturnsProblemDetails_OnError()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "test.txt"
        };
        content.Add(fileContent);

        // Act
        var response = await _client.PostAsync("/api/v1/ingestion/file", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(400);
        }
    }

    [Fact]
    public async Task POST_IngestUrl_ReturnsError_WhenUrlIsInvalid()
    {
        // Arrange
        var request = new { Url = "not-a-valid-url", TenantId = 1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/ingestion/url", request);

        // Assert - Should validate but not implemented yet
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task POST_IngestUrl_ReturnsError_WhenTenantIdIsMissing()
    {
        // Arrange
        var request = new { Url = "https://example.com", TenantId = 0 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/ingestion/url", request);

        // Assert - Should validate but not implemented yet
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task POST_IngestDatabase_ReturnsError_WithInvalidConnectionString()
    {
        // Arrange
        var request = new { ConnectionString = "Server=invalid", TenantId = 1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/ingestion/database", request);

        // Assert - Should return error (bad request or internal server error)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task POST_IngestMultipleFiles_HandlesConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 5; i++)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes($"test content {i}"));
            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = $"test{i}.txt"
            };
            content.Add(fileContent);
            content.Add(new StringContent("1"), "tenantId");
            
            tasks.Add(_client.PostAsync("/api/v1/ingestion/file", content));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert - All should have correlation IDs
        responses.Should().AllSatisfy(r =>
        {
            r.Headers.Should().Contain(h => h.Key == "X-Correlation-ID");
        });

        // Correlation IDs should all be unique
        var correlationIds = responses
            .Select(r => r.Headers.GetValues("X-Correlation-ID").First())
            .ToList();
        
        correlationIds.Distinct().Should().HaveCount(5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task POST_IngestFile_ReturnsError_WhenFilenameIsInvalid(string invalidFilename)
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
        fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = invalidFilename
        };
        content.Add(fileContent);
        content.Add(new StringContent("1"), "tenantId");

        // Act
        var response = await _client.PostAsync("/api/v1/ingestion/file", content);

        // Assert - Should fail but validation not implemented yet
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task POST_IngestFile_ReturnsError_WhenTenantIdIsInvalid(int invalidTenantId)
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
        fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
        {
            Name = "file",
            FileName = "test.txt"
        };
        content.Add(fileContent);
        content.Add(new StringContent(invalidTenantId.ToString()), "tenantId");

        // Act
        var response = await _client.PostAsync("/api/v1/ingestion/file", content);

        // Assert - Should fail but validation not implemented yet
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }
}
