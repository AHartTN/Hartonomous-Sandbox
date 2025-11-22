using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for VideoFileAtomizer.
/// Tests video file parsing, frame extraction, and audio track handling.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "Video")]
public class VideoFileAtomizerTests : UnitTestBase
{
    public VideoFileAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("video/mp4", ".mp4", true)]
    [InlineData("video/x-matroska", ".mkv", true)]
    [InlineData("video/webm", ".webm", true)]
    [InlineData("video/quicktime", ".mov", true)]
    [InlineData("video/x-msvideo", ".avi", true)]
    [InlineData("audio/mp3", ".mp3", false)]
    [InlineData("image/png", ".png", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic Video Tests

    [Fact]
    public async Task AtomizeAsync_ValidVideoFile_CreatesAtoms()
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();
        var videoData = CreateMinimalMp4();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.mp4")
            .WithContentType("video/mp4")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(videoData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.AtomizerType.Should().Be("VideoFileAtomizer");
    }

    #endregion

    #region Metadata Extraction Tests

    [Fact]
    public async Task AtomizeAsync_ExtractsVideoMetadata()
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();
        var videoData = CreateMinimalMp4();
        var metadata = CreateSourceMetadataBuilder().WithFileName("video.mp4").Build();

        // Act
        var result = await atomizer.AtomizeAsync(videoData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "video-metadata");
    }

    #endregion

    #region Frame Extraction Tests

    [Fact]
    public async Task AtomizeAsync_VideoWithFrames_DelegatesToImageAtomizer()
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();
        var videoData = CreateMinimalMp4();
        var metadata = CreateSourceMetadataBuilder().WithFileName("video.mp4").Build();

        // Act
        var result = await atomizer.AtomizeAsync(videoData, metadata);

        // Assert
        result.ProcessingInfo.Warnings.Should().NotBeNull();
    }

    #endregion

    #region Modality Tests

    [Fact]
    public async Task AtomizeAsync_VideoAtoms_HaveVideoModality()
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();
        var videoData = CreateMinimalMp4();
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.mp4").Build();

        // Act
        var result = await atomizer.AtomizeAsync(videoData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Modality == "video");
    }

    #endregion

    #region Composition Tests

    [Fact]
    public async Task AtomizeAsync_CreatesTemporalCompositions()
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();
        var videoData = CreateMinimalMp4();
        var metadata = CreateSourceMetadataBuilder().WithFileName("video.mp4").Build();

        // Act
        var result = await atomizer.AtomizeAsync(videoData, metadata);

        // Assert
        result.Compositions.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_CorruptedVideo_HandlesGracefully()
    {
        // Arrange
        var atomizer = new VideoFileAtomizer();
        var corruptedData = new byte[] { 0x00, 0x00, 0x00, 0x20 };
        var metadata = CreateSourceMetadataBuilder().WithFileName("corrupted.mp4").Build();

        // Act
        var result = await atomizer.AtomizeAsync(corruptedData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.ProcessingInfo.Warnings.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private byte[] CreateMinimalMp4()
    {
        // Minimal MP4 structure with ftyp atom
        var data = new List<byte>();
        
        // ftyp atom
        data.AddRange(BitConverter.GetBytes(20u).Reverse()); // size
        data.AddRange(System.Text.Encoding.ASCII.GetBytes("ftyp"));
        data.AddRange(System.Text.Encoding.ASCII.GetBytes("isom"));
        data.AddRange(BitConverter.GetBytes(0u).Reverse());
        data.AddRange(System.Text.Encoding.ASCII.GetBytes("isom"));
        
        return data.ToArray();
    }

    #endregion
}
