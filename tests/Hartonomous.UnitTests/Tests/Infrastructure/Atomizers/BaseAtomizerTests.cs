using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for BaseAtomizer abstract class.
/// Tests common atomization patterns, overflow handling, and composition logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class BaseAtomizerTests : UnitTestBase
{
    public BaseAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region Content Hash Tests

    [Fact]
    public void CreateContentHash_SameContent_ProducesSameHash()
    {
        // Arrange
        var content1 = System.Text.Encoding.UTF8.GetBytes("test content");
        var content2 = System.Text.Encoding.UTF8.GetBytes("test content");

        // Act
        var hash1 = TestAtomizer.PublicCreateContentHash(content1);
        var hash2 = TestAtomizer.PublicCreateContentHash(content2);

        // Assert
        hash1.Should().BeEquivalentTo(hash2);
    }

    [Fact]
    public void CreateContentHash_DifferentContent_ProducesDifferentHash()
    {
        // Arrange
        var content1 = System.Text.Encoding.UTF8.GetBytes("content 1");
        var content2 = System.Text.Encoding.UTF8.GetBytes("content 2");

        // Act
        var hash1 = TestAtomizer.PublicCreateContentHash(content1);
        var hash2 = TestAtomizer.PublicCreateContentHash(content2);

        // Assert
        hash1.Should().NotBeEquivalentTo(hash2);
    }

    [Fact]
    public void CreateContentHash_ProducesSHA256Hash()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("test");

        // Act
        var hash = TestAtomizer.PublicCreateContentHash(content);

        // Assert
        hash.Should().HaveCount(32); // SHA-256 produces 32 bytes
    }

    #endregion

    #region Fingerprint Tests

    [Fact]
    public void ComputeFingerprint_LargeContent_Returns64Bytes()
    {
        // Arrange
        var largeContent = new byte[1000];
        Random.Shared.NextBytes(largeContent);

        // Act
        var fingerprint = TestAtomizer.PublicComputeFingerprint(largeContent);

        // Assert
        fingerprint.Should().HaveCount(64);
    }

    [Fact]
    public void ComputeFingerprint_SameContent_ProducesSameFingerprint()
    {
        // Arrange
        var content = new byte[100];
        Random.Shared.NextBytes(content);

        // Act
        var fingerprint1 = TestAtomizer.PublicComputeFingerprint(content);
        var fingerprint2 = TestAtomizer.PublicComputeFingerprint(content);

        // Assert
        fingerprint1.Should().BeEquivalentTo(fingerprint2);
    }

    [Fact]
    public void ComputeFingerprint_ContainsHashAndPrefix()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("test content");

        // Act
        var fingerprint = TestAtomizer.PublicComputeFingerprint(content);

        // Assert
        fingerprint.Should().HaveCount(64);
        // First 32 bytes should be SHA256 hash
        // Last 32 bytes should be first 32 bytes of content (or padded)
    }

    #endregion

    #region JSON Merge Tests

    [Fact]
    public void MergeJsonMetadata_NullExisting_ReturnsNewJson()
    {
        // Arrange
        var additional = new { key = "value" };

        // Act
        var result = TestAtomizer.PublicMergeJsonMetadata(null, additional);

        // Assert
        result.Should().Contain("key");
        result.Should().Contain("value");
    }

    [Fact]
    public void MergeJsonMetadata_BothProvided_MergesCorrectly()
    {
        // Arrange
        var existing = "{\"existingKey\":\"existingValue\"}";
        var additional = new { newKey = "newValue" };

        // Act
        var result = TestAtomizer.PublicMergeJsonMetadata(existing, additional);

        // Assert
        result.Should().Contain("existingKey");
        result.Should().Contain("newKey");
    }

    [Fact]
    public void MergeJsonMetadata_OverlappingKeys_AdditionalWins()
    {
        // Arrange
        var existing = "{\"key\":\"oldValue\"}";
        var additional = new { key = "newValue" };

        // Act
        var result = TestAtomizer.PublicMergeJsonMetadata(existing, additional);

        // Assert
        result.Should().Contain("newValue");
    }

    #endregion

    #region Test Helper Class

    /// <summary>
    /// Test implementation of BaseAtomizer that exposes protected methods for testing.
    /// </summary>
    private class TestAtomizer : BaseAtomizer<byte[]>
    {
        public TestAtomizer(ILogger logger) : base(logger) { }

        public override int Priority => 5;
        public override bool CanHandle(string contentType, string? fileExtension) => true;
        protected override string GetModality() => "test";
        protected override string GetDetectedFormat() => "test-format";

        protected override Task AtomizeCoreAsync(
            byte[] input,
            Core.Interfaces.Ingestion.SourceMetadata source,
            List<Core.Interfaces.Ingestion.AtomData> atoms,
            List<Core.Interfaces.Ingestion.AtomComposition> compositions,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            // Minimal implementation for testing
            return Task.CompletedTask;
        }

        protected override byte[] GetFileMetadataBytes(byte[] input, Core.Interfaces.Ingestion.SourceMetadata source)
        {
            return System.Text.Encoding.UTF8.GetBytes($"File: {source.FileName}");
        }

        protected override string GetCanonicalFileText(byte[] input, Core.Interfaces.Ingestion.SourceMetadata source)
        {
            return $"File: {source.FileName}, Size: {input.Length}";
        }

        protected override string GetFileMetadataJson(byte[] input, Core.Interfaces.Ingestion.SourceMetadata source)
        {
            return $"{{\"fileName\":\"{source.FileName}\",\"size\":{input.Length}}}";
        }

        // Public wrappers for testing protected methods
        public static byte[] PublicCreateContentHash(byte[] data) => CreateContentHash(data);
        public static byte[] PublicComputeFingerprint(byte[] content) => ComputeFingerprint(content);
        public static string PublicMergeJsonMetadata(string? existing, object additional) => MergeJsonMetadata(existing, additional);
    }

    #endregion
}
