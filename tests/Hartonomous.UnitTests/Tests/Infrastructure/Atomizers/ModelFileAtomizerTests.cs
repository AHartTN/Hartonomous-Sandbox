using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for ModelFileAtomizer covering ONNX, PyTorch, TensorFlow, and other ML model formats.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "AIModel")]
public class ModelFileAtomizerTests : UnitTestBase
{
    public ModelFileAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/x-onnx", ".onnx", true)]
    [InlineData("application/x-pytorch", ".pth", true)]
    [InlineData("application/x-tensorflow", ".pb", true)]
    [InlineData("application/octet-stream", ".h5", true)]
    [InlineData("application/octet-stream", ".tflite", true)]
    [InlineData("application/x-safetensors", ".safetensors", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousFormats_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new ModelFileAtomizer();

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ONNX Tests

    [Fact]
    public async Task AtomizeAsync_OnnxModel_ExtractsStructure()
    {
        // Arrange
        var atomizer = new ModelFileAtomizer();
        var onnxData = CreateTestContent(1000);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("model.onnx")
            .WithContentType("application/x-onnx")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(onnxData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.DetectedFormat.Should().Contain("ONNX");
    }

    #endregion

    #region PyTorch Tests

    [Fact]
    public async Task AtomizeAsync_PyTorchModel_DetectsFormat()
    {
        // Arrange
        var atomizer = new ModelFileAtomizer();
        var torchData = CreatePyTorchTestData();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("model.pth")
            .WithContentType("application/x-pytorch")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(torchData, metadata);

        // Assert
        result.ProcessingInfo.DetectedFormat.Should().Contain("PYTORCH");
    }

    #endregion

    #region TensorFlow Tests

    [Fact]
    public async Task AtomizeAsync_TensorFlowModel_ExtractsOps()
    {
        // Arrange
        var atomizer = new ModelFileAtomizer();
        var tfData = CreateTestContent(2000);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("model.pb")
            .WithContentType("application/x-tensorflow")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(tfData, metadata);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region SafeTensors Tests

    [Fact]
    public async Task AtomizeAsync_SafeTensorsModel_ParsesHeader()
    {
        // Arrange
        var atomizer = new ModelFileAtomizer();
        var safetensorsData = CreateSafeTensorsTestData();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("model.safetensors")
            .WithContentType("application/x-safetensors")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(safetensorsData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "safetensors-metadata");
    }

    #endregion

    #region Keras Tests

    [Fact]
    public async Task AtomizeAsync_KerasH5Model_DetectsFormat()
    {
        // Arrange
        var atomizer = new ModelFileAtomizer();
        var h5Data = CreateH5TestData();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("model.h5")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(h5Data, metadata);

        // Assert
        result.ProcessingInfo.DetectedFormat.Should().Contain("H5");
    }

    #endregion

    #region Helper Methods

    private byte[] CreatePyTorchTestData()
    {
        var data = new List<byte> { 0x80 }; // Pickle header
        data.AddRange(System.Text.Encoding.ASCII.GetBytes("torch"));
        return data.ToArray();
    }

    private byte[] CreateSafeTensorsTestData()
    {
        var headerSize = 100L;
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(headerSize));
        data.AddRange(System.Text.Encoding.UTF8.GetBytes("{\"dtype\":\"F32\"}"));
        return data.ToArray();
    }

    private byte[] CreateH5TestData()
    {
        var data = new List<byte> { 0x89, 0x48, 0x44, 0x46 }; // HDF5 signature
        return data.ToArray();
    }

    #endregion
}
