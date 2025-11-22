using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Api;

/// <summary>
/// Integration tests for complete ingestion workflow.
/// Tests: Upload ? Detect Type ? Atomize ? Store ? Verify.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Ingestion")]
public class IngestionWorkflowTests : IntegrationTestBase<WebApplicationFactory<Program>>
{
    public IngestionWorkflowTests(WebApplicationFactory<Program> factory) : base(factory) { }

    #region Text File Ingestion

    [Fact]
    public async Task IngestTextFile_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var client = GetClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("Test content for ingestion"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await client.PostAsync("/api/ingestion/file", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    #endregion

    #region JSON File Ingestion

    [Fact]
    public async Task IngestJsonFile_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var client = GetClient();
        using var content = new MultipartFormDataContent();
        var jsonContent = "{\"name\":\"test\",\"value\":123}";
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", "test.json");

        // Act
        var response = await client.PostAsync("/api/ingestion/file", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task IngestFile_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var client = GetClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        content.Add(fileContent, "file", "empty.txt");

        // Act
        var response = await client.PostAsync("/api/ingestion/file", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IngestFile_NoFile_ReturnsBadRequest()
    {
        // Arrange
        var client = GetClient();
        using var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/ingestion/file", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
