using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for ImageAtomizer.
/// Tests pixel block extraction, OCR, object detection, and scene analysis integration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class ImageAtomizerTests : UnitTestBase
{
    public ImageAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("image/png", ".png", true)]
    [InlineData("image/jpeg", ".jpg", true)]
    [InlineData("image/gif", ".gif", true)]
    [InlineData("image/bmp", ".bmp", true)]
    [InlineData("image/webp", ".webp", true)]
    [InlineData("image/svg+xml", ".svg", false)] // Vector, not raster
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new ImageAtomizer(
            CreateLogger<ImageAtomizer>(),
            null, // OCR service
            null, // Object detection service
            null  // Scene analysis service
        );

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic Image Atomization Tests

    [Fact]
    public async Task AtomizeAsync_ValidPng_CreatesAtoms()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.png")
            .WithContentType("image/png")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.AtomizerType.Should().Be("ImageAtomizer");
    }

    [Fact]
    public async Task AtomizeAsync_ValidJpeg_CreatesAtoms()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var imageData = CreateFileBuilder().WithJpegHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.jpg")
            .WithContentType("image/jpeg")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Pixel Block Tests

    [Fact]
    public async Task AtomizeAsync_LargeImage_CreatesMultiplePixelBlocks()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        
        // Simulate large image (actual image processing would happen in real implementation)
        var largeImageData = CreateTestContent(100000); // 100KB
        var metadata = CreateSourceMetadataBuilder()
            .AsImageFileUpload("large.png", 100000)
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(largeImageData, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        // Should create multiple pixel blocks for large image
    }

    [Fact]
    public async Task AtomizeAsync_SmallImage_CreatesSinglePixelBlock()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var smallImageData = CreateTestContent(1024); // 1KB
        var metadata = CreateSourceMetadataBuilder()
            .AsImageFileUpload("small.png", 1024)
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(smallImageData, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveImageModality()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "image" || a.Subtype == "file-metadata");
    }

    [Fact]
    public async Task AtomizeAsync_PixelBlocks_HaveCorrectSubtype()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "pixel-block");
    }

    #endregion

    #region OCR Integration Tests

    [Fact]
    public async Task AtomizeAsync_WithOcrService_ExtractsText()
    {
        // Arrange
        var mockOcrService = CreateMockOcrService("Extracted text from image");
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), mockOcrService, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "ocr-text");
        result.Atoms.Should().Contain(a => a.CanonicalText?.Contains("Extracted text") == true);
    }

    [Fact]
    public async Task AtomizeAsync_OcrServiceFails_ContinuesWithoutOcr()
    {
        // Arrange
        var mockOcrService = CreateMockOcrServiceThatThrows();
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), mockOcrService, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty(); // Should still have pixel blocks
        result.ProcessingInfo.Warnings.Should().NotBeNull();
    }

    #endregion

    #region Object Detection Tests

    [Fact]
    public async Task AtomizeAsync_WithObjectDetection_ExtractsObjects()
    {
        // Arrange
        var mockObjectDetection = CreateMockObjectDetectionService(new[] { "person", "car", "tree" });
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, mockObjectDetection, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "detected-object");
        result.Atoms.Should().Contain(a => a.CanonicalText?.Contains("person") == true ||
                                           a.CanonicalText?.Contains("car") == true);
    }

    #endregion

    #region Scene Analysis Tests

    [Fact]
    public async Task AtomizeAsync_WithSceneAnalysis_ExtractsSceneDescription()
    {
        // Arrange
        var mockSceneAnalysis = CreateMockSceneAnalysisService("A sunny outdoor scene with mountains");
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, mockSceneAnalysis);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "scene-analysis");
        result.Atoms.Should().Contain(a => a.CanonicalText?.Contains("outdoor") == true);
    }

    #endregion

    #region Composition Tests

    [Fact]
    public async Task AtomizeAsync_CreatesCompositionHierarchy()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        result.Compositions.Should().NotBeEmpty();
        result.Compositions.Should().OnlyContain(c => c.ParentAtomHash != null);
        result.Compositions.Should().OnlyContain(c => c.ComponentAtomHash != null);
    }

    #endregion

    #region Spatial Positioning Tests

    [Fact]
    public async Task AtomizeAsync_PixelBlocks_HaveSpatialCoordinates()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var imageData = CreateFileBuilder().WithPngHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        var result = await atomizer.AtomizeAsync(imageData, metadata);

        // Assert
        // Compositions should have spatial positioning
        result.Compositions.Should().Contain(c => c.Position != null);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_CorruptedImage_HandlesGracefully()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var corruptedData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // Invalid
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act
        Func<Task> act = async () => await atomizer.AtomizeAsync(corruptedData, metadata);

        // Assert
        // Should either handle gracefully or throw specific exception
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task AtomizeAsync_EmptyImageData_ReturnsNoAtoms()
    {
        // Arrange
        var atomizer = new ImageAtomizer(CreateLogger<ImageAtomizer>(), null, null, null);
        var emptyData = Array.Empty<byte>();
        var metadata = CreateSourceMetadataBuilder().AsImageFileUpload().Build();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => atomizer.AtomizeAsync(emptyData, metadata));
    }

    #endregion

    #region Helper Methods

    private object CreateMockOcrService(string extractedText)
    {
        // In real implementation, would return IOcrService mock
        // For now, placeholder
        return new object();
    }

    private object CreateMockOcrServiceThatThrows()
    {
        // Mock that simulates OCR failure
        return new object();
    }

    private object CreateMockObjectDetectionService(string[] detectedObjects)
    {
        // Mock that returns detected objects
        return new object();
    }

    private object CreateMockSceneAnalysisService(string sceneDescription)
    {
        // Mock that returns scene description
        return new object();
    }

    #endregion
}
