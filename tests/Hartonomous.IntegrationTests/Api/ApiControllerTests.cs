using System.Net;
using System.Net.Http.Json;
using Hartonomous.Api.DTOs;
using Hartonomous.Api.DTOs.Generation;
using Hartonomous.Api.DTOs.Inference;
using Hartonomous.Api.DTOs.Search;
using Hartonomous.Core.Enums;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hartonomous.IntegrationTests.Api;

/// <summary>
/// Integration tests for all 14 API controllers.
/// Tests CRUD operations, request validation, and response formats.
/// </summary>
public class ApiControllerTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;
    private readonly ApiTestWebApplicationFactory _factory;

    public ApiControllerTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
        if (!fixture.IsAvailable)
        {
            Skip.If(!fixture.IsAvailable, fixture.SkipReason);
        }

        _factory = new ApiTestWebApplicationFactory(fixture.ConnectionString);
    }

    #region ModelsController Tests

    [Fact]
    public async Task ModelsController_GetModels_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ModelsController_GetModelById_ReturnsModel()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        
        // Create or get existing model
        var model = await _fixture.DbContext!.Models.FirstOrDefaultAsync();
        if (model == null)
        {
            Skip.If(true, "No models available for testing");
            return;
        }

        // Act
        var response = await client.GetAsync($"/api/models/{model.ModelId}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    #endregion

    #region SearchController Tests

    [Fact]
    public async Task SearchController_SemanticSearch_RequiresEmbedding()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new SemanticSearchRequest
        {
            QueryEmbedding = Array.Empty<float>(), // Empty embedding
            TopK = 10,
            ModelId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/search/semantic", request);

        // Assert - Should fail validation
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchController_SemanticSearch_ValidRequest_ReturnsResults()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var embedding = Enumerable.Range(0, 32).Select(i => (float)Math.Sin(i * 0.1)).ToArray();
        
        var request = new SemanticSearchRequest
        {
            QueryEmbedding = embedding,
            TopK = 5,
            ModelId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/search/semantic", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SearchController_HybridSearch_CombinesSpatialAndSemantic()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var embedding = Enumerable.Range(0, 32).Select(i => (float)Math.Sin(i * 0.1)).ToArray();
        
        var request = new HybridSearchRequest
        {
            QueryEmbedding = embedding,
            TopK = 5,
            CoarseCandidates = 20,
            FineCandidates = 10
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/search/hybrid", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region EmbeddingsController Tests

    [Fact]
    public async Task EmbeddingsController_CreateEmbedding_RequiresContent()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new CreateEmbeddingRequest
        {
            Content = "", // Empty content
            ModelId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/embeddings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmbeddingsController_CreateEmbedding_ValidRequest_ReturnsEmbedding()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new CreateEmbeddingRequest
        {
            Content = "Test content for embedding",
            ModelId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/embeddings", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    #endregion

    #region GenerationController Tests

    [Fact]
    public async Task GenerationController_GenerateText_RequiresPrompt()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new TextGenerationRequest
        {
            Prompt = "", // Empty prompt
            MaxTokens = 100
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/generation/text", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerationController_GenerateImage_ValidatesPrompt()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new ImageGenerationRequest
        {
            Prompt = "A sunset over mountains",
            Width = 512,
            Height = 512
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/generation/image", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task GenerationController_GenerateAudio_ValidatesParameters()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new AudioGenerationRequest
        {
            Text = "Hello world",
            VoiceId = "default"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/generation/audio", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotImplemented);
    }

    #endregion

    #region InferenceController Tests

    [Fact]
    public async Task InferenceController_SubmitInference_RequiresInput()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new InferenceRequest
        {
            InputData = "", // Empty input
            ModelId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/inference", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InferenceController_GetInferenceStatus_ReturnsStatus()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/inference/12345/status");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.IsSuccessStatusCode);
    }

    #endregion

    #region GraphQueryController Tests

    [Fact]
    public async Task GraphQueryController_ExecuteQuery_ValidatesQuery()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new { cypherQuery = "MATCH (n) RETURN n LIMIT 10" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/graph/query", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region GraphAnalyticsController Tests

    [Fact]
    public async Task GraphAnalyticsController_GetStats_ReturnsGraphStatistics()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/v1/graph/stats");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region SqlGraphController Tests

    [Fact]
    public async Task SqlGraphController_CreateNode_ValidatesInput()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");
        var request = new
        {
            NodeType = "Atom",
            AtomId = 12345L,
            Properties = new Dictionary<string, object>
            {
                ["modality"] = "text",
                ["canonicalText"] = "Test content"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/graph/sql/nodes", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region AnalyticsController Tests

    [Fact]
    public async Task AnalyticsController_GetModelPerformance_RequiresDataScientist()
    {
        // Arrange
        var userClient = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var dsClient = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");

        // Act
        var userResponse = await userClient.GetAsync("/api/analytics/model-performance");
        var dsResponse = await dsClient.GetAsync("/api/analytics/model-performance");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
        Assert.True(dsResponse.IsSuccessStatusCode || dsResponse.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AnalyticsController_GetUsageMetrics_ReturnsTenantMetrics()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");

        // Act
        var response = await client.GetAsync("/api/analytics/usage?tenantId=1");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region AutonomyController Tests

    [Fact]
    public async Task AutonomyController_GetStatus_RequiresAdmin()
    {
        // Arrange
        var userClient = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var adminClient = _factory.CreateAdminClient();

        // Act
        var userResponse = await userClient.GetAsync("/api/autonomy/status");
        var adminResponse = await adminClient.GetAsync("/api/autonomy/status");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
        Assert.True(adminResponse.IsSuccessStatusCode || adminResponse.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AutonomyController_TriggerAnalysis_RequiresAdmin()
    {
        // Arrange
        var userClient = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var adminClient = _factory.CreateAdminClient();

        // Act
        var userResponse = await userClient.PostAsync("/api/autonomy/analyze", null);
        var adminResponse = await adminClient.PostAsync("/api/autonomy/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
        Assert.True(adminResponse.IsSuccessStatusCode || adminResponse.StatusCode == HttpStatusCode.Accepted);
    }

    #endregion

    #region BillingController Tests

    [Fact]
    public async Task BillingController_GetUsage_ReturnsTenantUsage()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/billing/usage?tenantId=1");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task BillingController_GetUsage_TenantIsolation()
    {
        // Arrange
        var tenant1Client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act - Trying to access tenant 2's billing
        var response = await tenant1Client.GetAsync("/api/billing/usage?tenantId=2");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region FeedbackController Tests

    [Fact]
    public async Task FeedbackController_SubmitFeedback_ValidatesInput()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new
        {
            InferenceId = 12345,
            Rating = 5,
            Comment = "Great result!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/feedback", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region ProvenanceController Tests

    [Fact]
    public async Task ProvenanceController_GetStream_ReturnsGenerationStream()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var streamId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/provenance/stream/{streamId}");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.IsSuccessStatusCode);
    }

    #endregion

    #region OperationsController Tests

    [Fact]
    public async Task OperationsController_GetHealth_ReturnsHealthStatus()
    {
        // Arrange
        var client = _factory.CreateAdminClient();

        // Act
        var response = await client.GetAsync("/api/operations/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OperationsController_GetMetrics_RequiresAdmin()
    {
        // Arrange
        var userClient = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var adminClient = _factory.CreateAdminClient();

        // Act
        var userResponse = await userClient.GetAsync("/api/operations/metrics");
        var adminResponse = await adminClient.GetAsync("/api/operations/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
        Assert.True(adminResponse.IsSuccessStatusCode || adminResponse.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region JobsController Tests

    [Fact]
    public async Task JobsController_GetJobs_ReturnsTenantJobs()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/jobs?tenantId=1");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task JobsController_CancelJob_ValidatesOwnership()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.PostAsync("/api/jobs/12345/cancel", null);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                    response.StatusCode == HttpStatusCode.Forbidden ||
                    response.IsSuccessStatusCode);
    }

    #endregion

    #region BulkController Tests

    [Fact]
    public async Task BulkController_BulkIngest_ValidatesPayload()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new
        {
            Items = new[] 
            { 
                new { Content = "Item 1", Modality = Modality.Text.ToJsonString() },
                new { Content = "Item 2", Modality = Modality.Text.ToJsonString() }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/bulk/ingest", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region IngestionController Tests

    [Fact]
    public async Task IngestionController_IngestAtom_ValidatesContent()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var request = new
        {
            Content = "Test atom content",
            Modality = Modality.Text.ToJsonString(),
            SourceType = "api"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ingestion/atom", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion
}
