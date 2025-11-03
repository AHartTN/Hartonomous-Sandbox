using Hartonomous.Core.Services;

namespace Hartonomous.UnitTests.Core;

public sealed class ModelCapabilityServiceTests
{
    private readonly ModelCapabilityService _service = new();

    [Fact]
    public void InferFromModelName_ReturnsDefaults_WhenNameMissing()
    {
        var capabilities = _service.InferFromModelName(string.Empty);

        Assert.False(capabilities.SupportsTextGeneration);
        Assert.False(capabilities.SupportsImageGeneration);
        Assert.Equal("text", capabilities.PrimaryModality);
        Assert.Equal(0, capabilities.MaxTokens);
        Assert.Equal(0, capabilities.MaxContextWindow);
    }

    [Theory]
    [InlineData("dall-e-3")]
    [InlineData("Stable-Diffusion XL")]
    public void InferFromModelName_DetectsImageModels(string modelName)
    {
        var capabilities = _service.InferFromModelName(modelName);

        Assert.True(capabilities.SupportsImageGeneration);
        Assert.Equal("image", capabilities.PrimaryModality);
        Assert.Equal(4000, capabilities.MaxTokens);
    }

    [Theory]
    [InlineData("whisper-large-v3", false)]
    [InlineData("contoso-tts", true)]
    public void InferFromModelName_DetectsAudioModels(string modelName, bool expectsTts)
    {
        var capabilities = _service.InferFromModelName(modelName);

        Assert.Equal("audio", capabilities.PrimaryModality);
        Assert.Equal(25000, capabilities.MaxTokens);
        Assert.Equal(expectsTts, capabilities.SupportsAudioGeneration);
    }

    [Theory]
    [InlineData("text-embedding-3-large")]
    [InlineData("ada-002-embedding")] 
    public void InferFromModelName_DetectsEmbeddingModels(string modelName)
    {
        var capabilities = _service.InferFromModelName(modelName);

        Assert.True(capabilities.SupportsEmbeddings);
        Assert.Equal("text", capabilities.PrimaryModality);
        Assert.Equal(8191, capabilities.MaxTokens);
    }

    [Fact]
    public void InferFromModelName_DetectsVisionEnabledGpt4()
    {
        var capabilities = _service.InferFromModelName("GPT-4 Turbo Vision 32k");

        Assert.True(capabilities.SupportsTextGeneration);
        Assert.True(capabilities.SupportsVisionAnalysis);
        Assert.True(capabilities.SupportsFunctionCalling);
        Assert.True(capabilities.SupportsStreaming);
        Assert.Equal("multimodal", capabilities.PrimaryModality);
        Assert.Equal(4096, capabilities.MaxTokens);
        Assert.Equal(32768, capabilities.MaxContextWindow);
    }

    [Theory]
    [InlineData("gpt-4-turbo", 8192)]
    [InlineData("gpt-4-32k", 32768)]
    public void InferFromModelName_DetectsGpt4Variants(string modelName, int expectedContext)
    {
        var capabilities = _service.InferFromModelName(modelName);

        Assert.True(capabilities.SupportsTextGeneration);
        Assert.True(capabilities.SupportsFunctionCalling);
        Assert.True(capabilities.SupportsStreaming);
        Assert.Equal("text", capabilities.PrimaryModality);
        Assert.Equal(4096, capabilities.MaxTokens);
        Assert.Equal(expectedContext, capabilities.MaxContextWindow);
    }

    [Theory]
    [InlineData("gpt-3.5-turbo", 4096)]
    [InlineData("gpt-3.5-turbo-16k", 16384)]
    [InlineData("gpt-35-turbo", 4096)]
    public void InferFromModelName_DetectsGpt35Variants(string modelName, int expectedContext)
    {
        var capabilities = _service.InferFromModelName(modelName);

        Assert.True(capabilities.SupportsTextGeneration);
        Assert.Equal("text", capabilities.PrimaryModality);
        Assert.Equal(4096, capabilities.MaxTokens);
        Assert.Equal(expectedContext, capabilities.MaxContextWindow);
    }

    [Fact]
    public void InferFromModelName_DefaultsToTextModel()
    {
        var capabilities = _service.InferFromModelName("contoso-generic-model");

        Assert.True(capabilities.SupportsTextGeneration);
        Assert.Equal("text", capabilities.PrimaryModality);
        Assert.Equal(2048, capabilities.MaxTokens);
        Assert.Equal(4096, capabilities.MaxContextWindow);
    }

    [Theory]
    [InlineData("gpt-4-turbo", "text", true)]
    [InlineData("dall-e-3", "image", true)]
    [InlineData("whisper-large", "audio", false)]
    [InlineData("text-embedding-3-large", "embedding", true)]
    [InlineData("gpt-4-turbo", "vision", false)]
    public void SupportsCapability_UsesMappedFlags(string modelName, string capability, bool expected)
    {
        var result = _service.SupportsCapability(modelName, capability);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPrimaryModality_DelegatesToInference()
    {
        var modality = _service.GetPrimaryModality("gpt-4 turbo vision");

        Assert.Equal("multimodal", modality);
    }
}
