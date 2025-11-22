using FluentAssertions;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for TextAtomizer.
/// Tests text chunking, sentence boundaries, token counting, and atom composition.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class TextAtomizerTests : UnitTestBase
{
    public TextAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("text/plain", ".txt", true)]
    [InlineData("text/plain", ".log", true)]
    [InlineData("text/markdown", ".md", false)] // Handled by MarkdownAtomizer
    [InlineData("application/json", ".json", false)]
    [InlineData("image/png", ".png", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic Atomization Tests

    [Fact]
    public async Task AtomizeAsync_SimpleText_CreatesAtoms()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("This is a simple test. It has multiple sentences.");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.Should().NotBeNull();
        result.ProcessingInfo.AtomizerType.Should().Be("TextAtomizer");
    }

    [Fact]
    public async Task AtomizeAsync_EmptyText_ReturnsNoAtoms()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().BeEmpty();
    }

    #endregion

    #region Sentence Boundary Tests

    [Fact]
    public async Task AtomizeAsync_MultipleSentences_SplitsAtBoundaries()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var text = "First sentence. Second sentence! Third sentence? Fourth sentence.";
        var content = System.Text.Encoding.UTF8.GetBytes(text);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().HaveCountGreaterThan(1);
        result.Atoms.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.CanonicalText));
    }

    [Fact]
    public async Task AtomizeAsync_SentenceWithAbbreviations_DoesNotSplitIncorrectly()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var text = "Dr. Smith works at U.S.A. Institute. He is a scientist.";
        var content = System.Text.Encoding.UTF8.GetBytes(text);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        // Should not split at "Dr." or "U.S.A."
        result.Atoms.Any(a => a.CanonicalText?.Contains("Dr. Smith") == true ||
                             a.CanonicalText?.Contains("U.S.A. Institute") == true)
            .Should().BeTrue();
    }

    #endregion

    #region Chunk Size Tests

    [Fact]
    public async Task AtomizeAsync_LongText_CreatesMultipleChunks()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        
        // Create text larger than typical chunk size (512 chars)
        var longText = string.Join(" ", Enumerable.Range(0, 200)
            .Select(i => $"This is sentence number {i}."));
        
        var content = System.Text.Encoding.UTF8.GetBytes(longText);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().HaveCountGreaterThan(1);
        
        // Each chunk should respect max size
        result.Atoms.Should().OnlyContain(a => 
            a.CanonicalText == null || a.CanonicalText.Length <= 1024);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveCorrectModality()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("Test content.");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "text");
    }

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveContentHash()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("Test content.");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.ContentHash != null && a.ContentHash.Length == 32);
    }

    #endregion

    #region Composition Tests

    [Fact]
    public async Task AtomizeAsync_CreatesFileMetadataAtom()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("Test content.");
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.txt")
            .WithTenantId(1)
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "file-metadata");
    }

    [Fact]
    public async Task AtomizeAsync_CreatesCompositionRelationships()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("First sentence. Second sentence.");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Compositions.Should().NotBeEmpty();
        result.Compositions.Should().OnlyContain(c => c.ParentAtomHash != null);
        result.Compositions.Should().OnlyContain(c => c.ComponentAtomHash != null);
    }

    #endregion

    #region Unicode and Special Characters Tests

    [Fact]
    public async Task AtomizeAsync_UnicodeText_HandlesCorrectly()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var text = "Hello ??! ?????? ???! ????? ???????!";
        var content = System.Text.Encoding.UTF8.GetBytes(text);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Should().Contain(a => a.CanonicalText?.Contains("??") == true ||
                                           a.CanonicalText?.Contains("???") == true);
    }

    [Fact]
    public async Task AtomizeAsync_Emojis_HandlesCorrectly()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var text = "Hello! ?? This is a test with emojis. ????";
        var content = System.Text.Encoding.UTF8.GetBytes(text);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_SingleCharacter_CreatesAtom()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("A");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_OnlyWhitespace_ReturnsNoContentAtoms()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("   \n\t  \r\n  ");
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        // Should only have file metadata atom, no content atoms
        result.Atoms.Where(a => a.Subtype != "file-metadata").Should().BeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_NewlineVariations_HandlesAllTypes()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        var text = "Line 1\nLine 2\r\nLine 3\rLine 4";
        var content = System.Text.Encoding.UTF8.GetBytes(text);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task AtomizeAsync_LargeText_CompletesInReasonableTime()
    {
        // Arrange
        var atomizer = new TextAtomizer(CreateLogger<TextAtomizer>());
        
        // Create 100KB of text
        var largeText = string.Join(" ", Enumerable.Range(0, 10000)
            .Select(i => $"Sentence {i}."));
        
        var content = System.Text.Encoding.UTF8.GetBytes(largeText);
        var metadata = CreateSourceMetadataBuilder().AsTextFileUpload().Build();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await atomizer.AtomizeAsync(content, metadata);
        stopwatch.Stop();

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.DurationMs.Should().BeLessThan(5000); // < 5 seconds
        
        WriteTestDetail("Processing time", $"{stopwatch.ElapsedMilliseconds}ms");
        WriteTestDetail("Atoms created", result.Atoms.Count.ToString());
    }

    #endregion
}
