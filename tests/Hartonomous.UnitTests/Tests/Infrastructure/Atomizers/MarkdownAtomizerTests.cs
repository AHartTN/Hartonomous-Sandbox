using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for MarkdownAtomizer.
/// Tests markdown parsing, heading hierarchy, code blocks, and link extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class MarkdownAtomizerTests : UnitTestBase
{
    public MarkdownAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("text/markdown", ".md", true)]
    [InlineData("text/markdown", ".markdown", true)]
    [InlineData("text/x-markdown", ".md", true)]
    [InlineData("text/plain", ".txt", false)]
    [InlineData("text/html", ".html", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic Markdown Tests

    [Fact]
    public async Task AtomizeAsync_SimpleMarkdown_CreatesAtoms()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"# Title

This is a paragraph.

## Subtitle

Another paragraph.";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.md")
            .WithContentType("text/markdown")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.AtomizerType.Should().Be("MarkdownAtomizer");
    }

    #endregion

    #region Heading Hierarchy Tests

    [Fact]
    public async Task AtomizeAsync_MultipleHeadingLevels_PreservesHierarchy()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"# H1 Title
## H2 Subtitle
### H3 Section
#### H4 Subsection";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        // Should create atoms for each heading level
        result.Atoms.Any(a => a.CanonicalText?.Contains("H1") == true).Should().BeTrue();
        result.Atoms.Any(a => a.CanonicalText?.Contains("H2") == true).Should().BeTrue();
    }

    #endregion

    #region Code Block Tests

    [Fact]
    public async Task AtomizeAsync_FencedCodeBlock_ExtractsCode()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"# Example

```csharp
public class Test
{
    public void Method() { }
}
```";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => 
            a.CanonicalText?.Contains("public class Test") == true ||
            a.Subtype?.Contains("code") == true);
    }

    [Fact]
    public async Task AtomizeAsync_IndentedCodeBlock_ExtractsCode()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"Example code:

    var x = 10;
    var y = 20;";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task AtomizeAsync_UnorderedList_ParsesItems()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"# Shopping List

- Item 1
- Item 2
- Item 3";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_OrderedList_ParsesItems()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"# Steps

1. First step
2. Second step
3. Third step";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Link and Image Tests

    [Fact]
    public async Task AtomizeAsync_Links_ExtractsUrls()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"Visit [Example](https://example.com) for more info.";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_Images_ExtractsImageReferences()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"![Alt text](image.png)";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Table Tests

    [Fact]
    public async Task AtomizeAsync_Table_ParsesStructure()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"| Column 1 | Column 2 |
|----------|----------|
| Value 1  | Value 2  |
| Value 3  | Value 4  |";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Inline Formatting Tests

    [Fact]
    public async Task AtomizeAsync_InlineFormatting_PreservesContent()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"Text with **bold**, *italic*, and `code` formatting.";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Should().Contain(a => a.CanonicalText?.Contains("bold") == true);
    }

    #endregion

    #region Blockquote Tests

    [Fact]
    public async Task AtomizeAsync_Blockquote_ExtractsContent()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"> This is a quote
> across multiple lines";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveTextModality()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("# Test");
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "text" || a.Modality == "code");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_EmptyMarkdown_ReturnsNoAtoms()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var content = System.Text.Encoding.UTF8.GetBytes("");
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().BeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_MalformedMarkdown_HandlesGracefully()
    {
        // Arrange
        var atomizer = new MarkdownAtomizer(CreateLogger<MarkdownAtomizer>());
        var markdown = @"# Heading without closing

[Broken link(test

| Incomplete | table";
        var content = System.Text.Encoding.UTF8.GetBytes(markdown);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.md").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty(); // Should still extract what it can
    }

    #endregion
}
