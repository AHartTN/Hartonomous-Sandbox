using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Pipelines;

/// <summary>
/// Integration tests for full ingestion pipeline.
/// Tests end-to-end flow: Upload ? Detect ? Atomize ? Store ? Index.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Pipeline")]
public class FullIngestionPipelineTests : IntegrationTestBase<WebApplicationFactory<Program>>
{
    public FullIngestionPipelineTests(WebApplicationFactory<Program> factory) : base(factory) { }

    #region Text File Pipeline Tests

    [Fact]
    public async Task Pipeline_TextFile_CompletesSuccessfully()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var pipeline = CreateIngestionPipeline(context);
        var fileData = System.Text.Encoding.UTF8.GetBytes("Test content for integration pipeline.");

        // Act
        var result = await pipeline.IngestAsync(fileData, "test.txt", tenantId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ItemsProcessed.Should().BeGreaterThan(0);
        
        // Verify atoms were stored
        context.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Image File Pipeline Tests

    [Fact]
    public async Task Pipeline_ImageFile_CompletesWithPixelAtoms()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var pipeline = CreateIngestionPipeline(context);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();

        // Act
        var result = await pipeline.IngestAsync(imageData, "test.png", tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        context.Atoms.Any(a => a.Modality == "image").Should().BeTrue();
    }

    #endregion

    #region PDF Pipeline Tests

    [Fact]
    public async Task Pipeline_PdfFile_ExtractsTextAndMetadata()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var pipeline = CreateIngestionPipeline(context);
        var pdfData = CreateFileBuilder().WithPdfHeader().BuildContent();

        // Act
        var result = await pipeline.IngestAsync(pdfData, "document.pdf", tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        context.Atoms.Any(a => a.Subtype == "pdf-metadata").Should().BeTrue();
    }

    #endregion

    #region Model File Pipeline Tests

    [Fact]
    public async Task Pipeline_GgufModel_ExtractsModelStructure()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var pipeline = CreateIngestionPipeline(context);
        var ggufData = CreateFileBuilder().WithGgufHeader().BuildContent();

        // Act
        var result = await pipeline.IngestAsync(ggufData, "model.gguf", tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        context.Atoms.Any(a => a.Modality == "model").Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Pipeline_InvalidFile_HandlesGracefully()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var pipeline = CreateIngestionPipeline(context);
        var invalidData = new byte[] { 0xFF, 0xFF, 0xFF };

        // Act
        var result = await pipeline.IngestAsync(invalidData, "unknown.xyz", tenantId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private object CreateIngestionPipeline(HartonomousDbContext context)
    {
        // In real implementation, would wire up full pipeline
        return new { };
    }

    #endregion
}
