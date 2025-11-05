using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Core;

public sealed class InferenceMetadataServiceTests
{
    private readonly IModelRepository _mockRepository;
    private readonly ILogger<InferenceMetadataService> _mockLogger;
    private readonly InferenceMetadataService _service;

    public InferenceMetadataServiceTests()
    {
        _mockRepository = Substitute.For<IModelRepository>();
        _mockLogger = Substitute.For<ILogger<InferenceMetadataService>>();
        _service = new InferenceMetadataService(_mockRepository, _mockLogger);
    }

    [Fact]
    public void DetermineReasoningMode_WithMultiStep_ReturnsChainOfThought()
    {
        // Act
        var result = _service.DetermineReasoningMode("Any task", requiresMultiStep: true);

        // Assert
        Assert.Equal("chain-of-thought", result);
    }

    [Fact]
    public void DetermineReasoningMode_WithAnalyticalKeywords_ReturnsAnalytical()
    {
        // Act
        var result = _service.DetermineReasoningMode("Analyze this dataset and explain patterns", requiresMultiStep: false);

        // Assert
        Assert.Equal("analytical", result);
    }

    [Fact]
    public void DetermineReasoningMode_WithCreativeKeywords_ReturnsCreative()
    {
        // Act
        var result = _service.DetermineReasoningMode("Generate a creative story about AI", requiresMultiStep: false);

        // Assert
        Assert.Equal("creative", result);
    }

    [Fact]
    public void DetermineReasoningMode_WithNoSpecialKeywords_ReturnsDirect()
    {
        // Act
        var result = _service.DetermineReasoningMode("What is the capital of France?", requiresMultiStep: false);

        // Assert
        Assert.Equal("direct", result);
    }

    [Fact]
    public void CalculateComplexity_WithHighTokenCount_ReturnsHighComplexity()
    {
        // Act
        var result = _service.CalculateComplexity(
            inputTokenCount: 9000,
            requiresMultiModal: true,
            requiresToolUse: true);

        // Assert
        Assert.Equal(9, result); // 1 + 4 (tokens > 8000) + 2 (multimodal) + 2 (tool use) = 9
    }

    [Fact]
    public void CalculateComplexity_WithLowTokenCount_ReturnsLowComplexity()
    {
        // Act
        var result = _service.CalculateComplexity(
            inputTokenCount: 500,
            requiresMultiModal: false,
            requiresToolUse: false);

        // Assert
        Assert.Equal(1, result); // Base complexity only
    }

    [Fact]
    public void DetermineSla_WithCriticalPriority_ReturnsRealtime()
    {
        // Act
        var result = _service.DetermineSla("critical", complexity: 5);

        // Assert
        Assert.Equal("realtime", result);
    }

    [Fact]
    public void DetermineSla_WithHighPriorityLowComplexity_ReturnsRealtime()
    {
        // Act
        var result = _service.DetermineSla("high", complexity: 3);

        // Assert
        Assert.Equal("realtime", result);
    }

    [Fact]
    public void DetermineSla_WithHighPriorityHighComplexity_ReturnsExpedited()
    {
        // Act
        var result = _service.DetermineSla("high", complexity: 7);

        // Assert
        Assert.Equal("expedited", result);
    }

    [Fact]
    public void DetermineSla_WithLowPriority_ReturnsStandard()
    {
        // Act
        var result = _service.DetermineSla("low", complexity: 2);

        // Assert
        Assert.Equal("standard", result);
    }

    [Fact]
    public async Task EstimateResponseTimeAsync_WithValidModelMetrics_ReturnsCalculatedTime()
    {
        // Arrange
        var model = new Model
        {
            ModelName = "gpt-4",
            ModelType = "transformer",
            Metadata = new ModelMetadata
            {
                PerformanceMetrics = "{\"AvgLatencyMs\": 200, \"TokensPerSecond\": 50}"
            }
        };

        _mockRepository.GetByNameAsync("gpt-4", Arg.Any<CancellationToken>()).Returns(model);

        // Act
        var result = await _service.EstimateResponseTimeAsync("gpt-4", complexity: 5);

        // Assert
        // baseLatency (200) + (complexity (5) * (baseLatency / 10)) = 200 + 100 = 300
        Assert.Equal(300, result);
    }

    [Fact]
    public async Task EstimateResponseTimeAsync_WithNoModelMetrics_ReturnsDefaultEstimate()
    {
        // Arrange
        var model = new Model
        {
            ModelName = "test-model",
            ModelType = "transformer",
            Metadata = new ModelMetadata()
        };

        _mockRepository.GetByNameAsync("test-model", Arg.Any<CancellationToken>()).Returns(model);

        // Act
        var result = await _service.EstimateResponseTimeAsync("test-model", complexity: 4);

        // Assert
        Assert.Equal(20, result); // complexity * 5 = 4 * 5 = 20
    }

    [Fact]
    public async Task EstimateResponseTimeAsync_WithNullModelName_ReturnsDefaultEstimate()
    {
        // Act
        var result = await _service.EstimateResponseTimeAsync(null!, complexity: 3);

        // Assert
        Assert.Equal(15, result); // complexity * 5 = 3 * 5 = 15
    }

    [Fact]
    public async Task EstimateResponseTimeAsync_WithModelNotFound_ReturnsDefaultEstimate()
    {
        // Arrange
        _mockRepository.GetByNameAsync("unknown-model", Arg.Any<CancellationToken>()).Returns((Model?)null);

        // Act
        var result = await _service.EstimateResponseTimeAsync("unknown-model", complexity: 6);

        // Assert
        Assert.Equal(30, result); // complexity * 5 = 6 * 5 = 30
    }
}
