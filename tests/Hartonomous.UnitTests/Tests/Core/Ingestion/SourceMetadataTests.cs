using FluentAssertions;
using Hartonomous.Core.Interfaces.Ingestion;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Core.Ingestion;

/// <summary>
/// Tests for SourceMetadata domain model.
/// Validates metadata creation and property handling.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Fast")]
public class SourceMetadataTests
{
    [Fact]
    public void SourceMetadata_DefaultConstructor_CreatesInstance()
    {
        // Act
        var metadata = new SourceMetadata();

        // Assert
        metadata.Should().NotBeNull();
    }

    [Fact]
    public void SourceMetadata_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var metadata = new SourceMetadata
        {
            FileName = "test.txt",
            SourceUri = "upload://test.txt",
            SourceType = "file-upload",
            ContentType = "text/plain",
            SizeBytes = 1024,
            TenantId = 1,
            Metadata = "{\"key\":\"value\"}"
        };

        // Assert
        metadata.FileName.Should().Be("test.txt");
        metadata.SourceUri.Should().Be("upload://test.txt");
        metadata.SourceType.Should().Be("file-upload");
        metadata.ContentType.Should().Be("text/plain");
        metadata.SizeBytes.Should().Be(1024);
        metadata.TenantId.Should().Be(1);
        metadata.Metadata.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void SourceMetadata_FileUpload_HasCorrectProperties()
    {
        // Arrange & Act
        var metadata = new SourceMetadata
        {
            FileName = "document.pdf",
            SourceUri = "upload://document.pdf",
            SourceType = "file-upload",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            TenantId = 5
        };

        // Assert
        metadata.FileName.Should().Be("document.pdf");
        metadata.SourceType.Should().Be("file-upload");
        metadata.SizeBytes.Should().Be(2048);
        metadata.TenantId.Should().Be(5);
    }

    [Fact]
    public void SourceMetadata_UrlFetch_HasCorrectProperties()
    {
        // Arrange & Act
        var metadata = new SourceMetadata
        {
            FileName = "page.html",
            SourceUri = "https://example.com/page.html",
            SourceType = "url-fetch",
            ContentType = "text/html",
            TenantId = 1
        };

        // Assert
        metadata.SourceUri.Should().StartWith("https://");
        metadata.SourceType.Should().Be("url-fetch");
    }

    [Fact]
    public void SourceMetadata_OptionalMetadata_CanBeNull()
    {
        // Arrange & Act
        var metadata = new SourceMetadata
        {
            FileName = "test.txt",
            TenantId = 1
        };

        // Assert
        metadata.Metadata.Should().BeNull();
        metadata.SourceUri.Should().BeNull();
    }
}
