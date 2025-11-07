using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hartonomous.UnitTests.Core;

public sealed class ModelCapabilityServiceTests
{
    private readonly IModelRepository _mockRepository;
    private readonly ILogger<ModelCapabilityService> _mockLogger;
    private readonly ModelCapabilityService _service;

    public ModelCapabilityServiceTests()
    {
        _mockRepository = Substitute.For<IModelRepository>();
        _mockLogger = Substitute.For<ILogger<ModelCapabilityService>>();
        _service = new ModelCapabilityService(_mockRepository, _mockLogger);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_WithValidModel_ReturnsCapabilitiesFromMetadata()
    {
        // Arrange
        var modelName = "test-llm";
        var model = new Model
        {
            ModelId = 1,
            ModelName = modelName,
            ModelType = "transformer",
            Metadata = new ModelMetadata
            {
                SupportedTasks = "[\"text_generation\", \"text_embedding\"]",
                SupportedModalities = "[\"text\"]",
                MaxInputLength = 8192,
                MaxOutputLength = 4096,
                EmbeddingDimension = 1536
            }
        };

        _mockRepository.GetByNameAsync(modelName, Arg.Any<CancellationToken>())
            .Returns(model);

        // Act
        var result = await _service.GetCapabilitiesAsync(modelName);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SupportsTask(TaskType.TextGeneration));
        Assert.True(result.SupportsTask(TaskType.TextEmbedding));
        Assert.True(result.SupportsModality(Modality.Text));
        Assert.Equal(4096, result.MaxTokens);
        Assert.Equal(8192, result.MaxContextWindow);
        Assert.Equal(1536, result.EmbeddingDimension);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_WithNullModelName_ReturnsDefaultCapabilities()
    {
        // Act
        var result = await _service.GetCapabilitiesAsync(null!);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SupportsTask(TaskType.TextGeneration));
        Assert.Equal(2048, result.MaxTokens);
        Assert.Equal(4096, result.MaxContextWindow);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_WithModelNotFound_ReturnsDefaultCapabilities()
    {
        // Arrange
        _mockRepository.GetByNameAsync("nonexistent-model", Arg.Any<CancellationToken>())
            .Returns((Model?)null);

        // Act
        var result = await _service.GetCapabilitiesAsync("nonexistent-model");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SupportsTask(TaskType.TextGeneration));
    }

    [Fact]
    public async Task SupportsCapabilityAsync_WithSupportedTaskType_ReturnsTrue()
    {
        // Arrange
        var model = new Model
        {
            ModelId = 1,
            ModelName = "test-model",
            ModelType = "diffusion",
            Metadata = new ModelMetadata
            {
                SupportedTasks = "[\"image_generation\"]",
                SupportedModalities = "[\"image\"]"
            }
        };

        _mockRepository.GetByNameAsync("test-model", Arg.Any<CancellationToken>())
            .Returns(model);

        // Act
        var result = await _service.SupportsCapabilityAsync("test-model", "image_generation");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SupportsCapabilityAsync_WithUnsupportedCapability_ReturnsFalse()
    {
        // Arrange
        var model = new Model
        {
            ModelId = 1,
            ModelName = "test-model",
            ModelType = "transformer",
            Metadata = new ModelMetadata
            {
                SupportedTasks = "[\"text_generation\"]",
                SupportedModalities = "[\"text\"]"
            }
        };

        _mockRepository.GetByNameAsync("test-model", Arg.Any<CancellationToken>())
            .Returns(model);

        // Act
        var result = await _service.SupportsCapabilityAsync("test-model", "image_generation");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetPrimaryModalityAsync_ReturnsCorrectModality()
    {
        // Arrange
        var model = new Model
        {
            ModelId = 1,
            ModelName = "vision-model",
            ModelType = "cnn",
            Metadata = new ModelMetadata
            {
                SupportedTasks = "[\"object-detection\"]",
                SupportedModalities = "[\"image\"]"  // Single modality to avoid priority logic
            }
        };

        _mockRepository.GetByNameAsync("vision-model", Arg.Any<CancellationToken>())
            .Returns(model);

        // Act
        var result = await _service.GetPrimaryModalityAsync("vision-model");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("image", result.ToLowerInvariant());
    }
}

