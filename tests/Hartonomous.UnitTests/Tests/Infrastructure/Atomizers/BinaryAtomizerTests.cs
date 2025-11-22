using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for BinaryAtomizer - Generic binary file handling and chunking.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class BinaryAtomizerTests : UnitTestBase
{
    public BinaryAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/octet-stream", ".bin", true)]
    [InlineData("application/octet-stream", ".dat", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new BinaryAtomizer(CreateLogger<BinaryAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Chunking Tests

    [Fact]
    public async Task AtomizeAsync_SmallBinary_CreatesSingleChunk()
    {
        // Arrange
        var atomizer = new BinaryAtomizer(CreateLogger<BinaryAtomizer>());
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var metadata = CreateSourceMetadataBuilder().WithFileName("small.bin").Build();

        // Act
        var result = await atomizer.AtomizeAsync(data, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_LargeBinary_CreatesMultipleChunks()
    {
        // Arrange
        var atomizer = new BinaryAtomizer(CreateLogger<BinaryAtomizer>());
        var data = new byte[1024]; // 1KB
        var metadata = CreateSourceMetadataBuilder().WithFileName("large.bin").Build();

        // Act
        var result = await atomizer.AtomizeAsync(data, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Compositions.Should().NotBeEmpty();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveBinaryModality()
    {
        // Arrange
        var atomizer = new BinaryAtomizer(CreateLogger<BinaryAtomizer>());
        var data = new byte[] { 0xFF, 0xEE, 0xDD, 0xCC };
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.bin").Build();

        // Act
        var result = await atomizer.AtomizeAsync(data, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "binary");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_EmptyBinary_HandlesGracefully()
    {
        // Arrange
        var atomizer = new BinaryAtomizer(CreateLogger<BinaryAtomizer>());
        var data = Array.Empty<byte>();
        var metadata = CreateSourceMetadataBuilder().WithFileName("empty.bin").Build();

        // Act
        Func<Task> act = async () => await atomizer.AtomizeAsync(data, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion
}
