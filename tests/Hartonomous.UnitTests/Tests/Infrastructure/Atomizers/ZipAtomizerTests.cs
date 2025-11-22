using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for ZipAtomizer - Archive extraction and recursive atomization.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class ZipAtomizerTests : UnitTestBase
{
    public ZipAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/zip", ".zip", true)]
    [InlineData("application/x-zip-compressed", ".zip", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new ZipAtomizer(CreateLogger<ZipAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic Archive Tests

    [Fact]
    public async Task AtomizeAsync_ValidZip_ExtractsFiles()
    {
        // Arrange
        var atomizer = new ZipAtomizer(CreateLogger<ZipAtomizer>());
        var zipData = CreateZipWithFiles(("file1.txt", "content1"), ("file2.txt", "content2"));
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.zip").Build();

        // Act
        var result = await atomizer.AtomizeAsync(zipData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ChildSources.Should().NotBeNull();
    }

    [Fact]
    public async Task AtomizeAsync_EmptyZip_ReturnsMetadataOnly()
    {
        // Arrange
        var atomizer = new ZipAtomizer(CreateLogger<ZipAtomizer>());
        var emptyZip = CreateEmptyZip();
        var metadata = CreateSourceMetadataBuilder().WithFileName("empty.zip").Build();

        // Act
        var result = await atomizer.AtomizeAsync(emptyZip, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty(); // Metadata atom
        result.ChildSources.Should().BeEmpty();
    }

    #endregion

    #region Nested Archive Tests

    [Fact]
    public async Task AtomizeAsync_NestedZip_ExtractsRecursively()
    {
        // Arrange
        var atomizer = new ZipAtomizer(CreateLogger<ZipAtomizer>());
        var innerZip = CreateZipWithFiles(("inner.txt", "nested content"));
        var outerZip = CreateZipWithFiles(("inner.zip", System.Text.Encoding.UTF8.GetString(innerZip)));
        var metadata = CreateSourceMetadataBuilder().WithFileName("outer.zip").Build();

        // Act
        var result = await atomizer.AtomizeAsync(outerZip, metadata);

        // Assert
        result.ChildSources.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private byte[] CreateZipWithFiles(params (string name, string content)[] files)
    {
        using var ms = new System.IO.MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new System.IO.StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        return ms.ToArray();
    }

    private byte[] CreateEmptyZip()
    {
        using var ms = new System.IO.MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            // Empty archive
        }
        return ms.ToArray();
    }

    #endregion
}
