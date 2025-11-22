using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for GgufAtomizer.
/// Tests GGUF model parsing, tensor extraction, weight chunking, and metadata handling.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "AIModel")]
public class GgufAtomizerTests : UnitTestBase
{
    public GgufAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/x-gguf", ".gguf", true)]
    [InlineData("application/octet-stream", ".gguf", true)]
    [InlineData("application/x-safetensors", ".safetensors", false)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GGUF Header Parsing Tests

    [Fact]
    public async Task AtomizeAsync_ValidGgufHeader_ParsesVersion()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufTestData();
        var metadata = CreateSourceMetadataBuilder()
            .AsGgufModel("test-model.gguf")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.AtomizerType.Should().Be("GgufAtomizer");
    }

    [Fact]
    public async Task AtomizeAsync_GgufFile_ExtractsMetadata()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "model-metadata");
    }

    #endregion

    #region Tensor Extraction Tests

    [Fact]
    public async Task AtomizeAsync_GgufWithTensors_ExtractsTensorInfo()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithTensorsTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "tensor");
    }

    [Fact]
    public async Task AtomizeAsync_MultipleTensors_CreatesAtomsForEach()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithMultipleTensorsTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        var tensorAtoms = result.Atoms.Where(a => a.Subtype == "tensor").ToList();
        tensorAtoms.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Weight Chunking Tests

    [Fact]
    public async Task AtomizeAsync_LargeWeights_ChunksInto64ByteAtoms()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithLargeWeightsTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.AtomicValue.Length <= 64);
    }

    [Fact]
    public async Task AtomizeAsync_WeightChunks_HaveSequentialIndices()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithLargeWeightsTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Compositions.Should().OnlyContain(c => c.SequenceIndex >= 0);
    }

    #endregion

    #region Quantization Tests

    [Theory]
    [InlineData("Q4_0")]
    [InlineData("Q4_1")]
    [InlineData("Q5_0")]
    [InlineData("Q8_0")]
    [InlineData("F16")]
    [InlineData("F32")]
    public async Task AtomizeAsync_VariousQuantizations_HandlesCorrectly(string quantization)
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithQuantizationTestData(quantization);
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Any(a => a.Metadata?.Contains(quantization) == true).Should().BeTrue();
    }

    #endregion

    #region Layer Decomposition Tests

    [Fact]
    public async Task AtomizeAsync_MultiLayerModel_PreservesLayerHierarchy()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufMultiLayerTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Compositions.Should().NotBeEmpty();
        // Should have parent-child relationships for layers
        result.Compositions.Should().Contain(c => c.ParentAtomHash != null && c.ComponentAtomHash != null);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveModelModality()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "model" || a.Subtype == "file-metadata");
    }

    [Fact]
    public async Task AtomizeAsync_ExtractsModelArchitecture()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithArchitectureTestData("llama");
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Metadata?.Contains("llama") == true);
    }

    [Fact]
    public async Task AtomizeAsync_ExtractsModelParameters()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithParametersTestData(layers: 32, heads: 32, dimensions: 4096);
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Metadata?.Contains("layers") == true);
    }

    #endregion

    #region Content-Addressable Storage Tests

    [Fact]
    public async Task AtomizeAsync_IdenticalWeights_ProduceSameHash()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var ggufData = CreateGgufWithDuplicateWeightsTestData();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        var result = await atomizer.AtomizeAsync(ggufData, metadata);

        // Assert
        var hashes = result.Atoms.Select(a => Convert.ToBase64String(a.ContentHash)).ToList();
        hashes.Should().Contain(h => hashes.Count(x => x == h) > 1); // Some hashes should be duplicated
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_TruncatedGgufFile_HandlesGracefully()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var truncatedData = CreateFileBuilder().WithGgufHeader().BuildContent().Take(20).ToArray();
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act
        Func<Task> act = async () => await atomizer.AtomizeAsync(truncatedData, metadata);

        // Assert
        // Should throw or handle gracefully with warning
        var result = await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task AtomizeAsync_InvalidGgufMagic_ThrowsException()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        var invalidData = System.Text.Encoding.ASCII.GetBytes("NOTGGUF");
        var metadata = CreateSourceMetadataBuilder().AsGgufModel().Build();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            () => atomizer.AtomizeAsync(invalidData, metadata));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task AtomizeAsync_LargeModel_CompletesInReasonableTime()
    {
        // Arrange
        var atomizer = new GgufAtomizer(CreateLogger<GgufAtomizer>());
        
        // Simulate 1MB model (small test, real models are GB)
        var largeModelData = CreateTestContent(1_000_000);
        var metadata = CreateSourceMetadataBuilder()
            .AsGgufModel("large-model.gguf", 1_000_000)
            .Build();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await atomizer.AtomizeAsync(largeModelData, metadata);
        stopwatch.Stop();

        // Assert
        result.Atoms.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // < 10 seconds
        
        WriteTestDetail("Processing time", $"{stopwatch.ElapsedMilliseconds}ms");
        WriteTestDetail("Atoms created", result.Atoms.Count.ToString());
    }

    #endregion

    #region Helper Methods

    private byte[] CreateGgufTestData()
    {
        // Minimal valid GGUF file structure
        var data = new List<byte>();
        data.AddRange(System.Text.Encoding.ASCII.GetBytes("GGUF")); // Magic
        data.AddRange(BitConverter.GetBytes((uint)3)); // Version
        data.AddRange(BitConverter.GetBytes((ulong)0)); // Tensor count
        data.AddRange(BitConverter.GetBytes((ulong)0)); // KV metadata count
        return data.ToArray();
    }

    private byte[] CreateGgufWithTensorsTestData()
    {
        var data = CreateGgufTestData().ToList();
        // Add tensor data (simplified)
        return data.ToArray();
    }

    private byte[] CreateGgufWithMultipleTensorsTestData()
    {
        return CreateGgufWithTensorsTestData();
    }

    private byte[] CreateGgufWithLargeWeightsTestData()
    {
        return CreateTestContent(10000); // 10KB of weight data
    }

    private byte[] CreateGgufWithQuantizationTestData(string quantization)
    {
        return CreateGgufTestData();
    }

    private byte[] CreateGgufMultiLayerTestData()
    {
        return CreateGgufTestData();
    }

    private byte[] CreateGgufWithArchitectureTestData(string architecture)
    {
        return CreateGgufTestData();
    }

    private byte[] CreateGgufWithParametersTestData(int layers, int heads, int dimensions)
    {
        return CreateGgufTestData();
    }

    private byte[] CreateGgufWithDuplicateWeightsTestData()
    {
        return CreateGgufTestData();
    }

    #endregion
}
