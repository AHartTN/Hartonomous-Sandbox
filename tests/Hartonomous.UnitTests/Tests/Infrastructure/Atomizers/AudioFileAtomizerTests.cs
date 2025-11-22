using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for AudioFileAtomizer.
/// Tests audio file parsing, PCM extraction, and sample atomization.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "Audio")]
public class AudioFileAtomizerTests : UnitTestBase
{
    public AudioFileAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("audio/mpeg", ".mp3", true)]
    [InlineData("audio/flac", ".flac", true)]
    [InlineData("audio/wav", ".wav", true)]
    [InlineData("audio/ogg", ".ogg", true)]
    [InlineData("audio/aac", ".aac", true)]
    [InlineData("audio/m4a", ".m4a", true)]
    [InlineData("video/mp4", ".mp4", false)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new AudioFileAtomizer();

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic Audio Tests

    [Fact]
    public async Task AtomizeAsync_ValidAudioFile_CreatesAtoms()
    {
        // Arrange
        var atomizer = new AudioFileAtomizer();
        var audioData = CreateWavFile(44100, 2, 1.0);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.wav")
            .WithContentType("audio/wav")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(audioData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.AtomizerType.Should().Be("AudioFileAtomizer");
    }

    #endregion

    #region Metadata Extraction Tests

    [Fact]
    public async Task AtomizeAsync_ExtractsAudioMetadata()
    {
        // Arrange
        var atomizer = new AudioFileAtomizer();
        var audioData = CreateWavFile(44100, 2, 1.0);
        var metadata = CreateSourceMetadataBuilder().WithFileName("audio.wav").Build();

        // Act
        var result = await atomizer.AtomizeAsync(audioData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "audio-metadata");
        result.Atoms.Should().Contain(a => a.Metadata?.Contains("sampleRate") == true);
    }

    #endregion

    #region Sample Buffer Tests

    [Fact]
    public async Task AtomizeAsync_CreatesAudioBuffers()
    {
        // Arrange
        var atomizer = new AudioFileAtomizer();
        var audioData = CreateWavFile(44100, 2, 2.0); // 2 seconds
        var metadata = CreateSourceMetadataBuilder().WithFileName("audio.wav").Build();

        // Act
        var result = await atomizer.AtomizeAsync(audioData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "buffer-id");
    }

    #endregion

    #region Modality Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveAudioModality()
    {
        // Arrange
        var atomizer = new AudioFileAtomizer();
        var audioData = CreateWavFile(44100, 2, 1.0);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.wav").Build();

        // Act
        var result = await atomizer.AtomizeAsync(audioData, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "audio");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_CorruptedAudio_HandlesGracefully()
    {
        // Arrange
        var atomizer = new AudioFileAtomizer();
        var corruptedData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var metadata = CreateSourceMetadataBuilder().WithFileName("corrupted.wav").Build();

        // Act
        var result = await atomizer.AtomizeAsync(corruptedData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.ProcessingInfo.Warnings.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private byte[] CreateWavFile(int sampleRate, int channels, double durationSeconds)
    {
        var bytesPerSample = 2; // 16-bit
        var sampleCount = (int)(sampleRate * channels * durationSeconds);
        var dataSize = sampleCount * bytesPerSample;
        
        using var ms = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(ms);
        
        // WAV header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bytesPerSample);
        writer.Write((short)(channels * bytesPerSample));
        writer.Write((short)(bytesPerSample * 8));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        
        // Audio data (silence)
        writer.Write(new byte[dataSize]);
        
        return ms.ToArray();
    }

    #endregion
}
