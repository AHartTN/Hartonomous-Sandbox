using FluentAssertions;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.FileType;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.FileType;

/// <summary>
/// Comprehensive tests for FileTypeDetector covering all supported file formats.
/// Tests magic byte detection, extension fallback, and heuristic detection.
/// </summary>
public class FileTypeDetectorTests : UnitTestBase
{
    private readonly IFileTypeDetector _detector;

    public FileTypeDetectorTests(ITestOutputHelper output) : base(output)
    {
        _detector = new FileTypeDetector();
    }

    #region Image Format Tests - Raster

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Fast")]
    public void Detect_PngFile_ReturnsPngType()
    {
        // Arrange
        var content = CreateFileBuilder().WithPngHeader().BuildContent();

        // Act
        var result = _detector.Detect(content, "test.png");

        // Assert
        result.ContentType.Should().Be("image/png");
        result.Category.Should().Be(FileCategory.ImageRaster);
        result.SpecificFormat.Should().Be("PNG");
        result.Extension.Should().Be("png");
        result.Confidence.Should().BeGreaterThan(0.9);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_JpegFile_ReturnsJpegType()
    {
        // Arrange
        var content = CreateFileBuilder().WithJpegHeader().BuildContent();

        // Act
        var result = _detector.Detect(content, "test.jpg");

        // Assert
        result.ContentType.Should().Be("image/jpeg");
        result.Category.Should().Be(FileCategory.ImageRaster);
        result.SpecificFormat.Should().Be("JPEG");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_GifFile_ReturnsGifType()
    {
        // Arrange
        var content = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a

        // Act
        var result = _detector.Detect(content, "test.gif");

        // Assert
        result.ContentType.Should().Be("image/gif");
        result.Category.Should().Be(FileCategory.ImageRaster);
        result.SpecificFormat.Should().Be("GIF");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_BmpFile_ReturnsBmpType()
    {
        // Arrange
        var content = new byte[] { 0x42, 0x4D, 0x00, 0x00 }; // BM header

        // Act
        var result = _detector.Detect(content, "test.bmp");

        // Assert
        result.ContentType.Should().Be("image/bmp");
        result.Category.Should().Be(FileCategory.ImageRaster);
    }

    #endregion

    #region Document Format Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_PdfFile_ReturnsPdfType()
    {
        // Arrange
        var content = CreateFileBuilder().WithPdfHeader().BuildContent();

        // Act
        var result = _detector.Detect(content, "test.pdf");

        // Assert
        result.ContentType.Should().Be("application/pdf");
        result.Category.Should().Be(FileCategory.DocumentPdf);
        result.SpecificFormat.Should().Be("PDF");
        result.Extension.Should().Be("pdf");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_ZipFile_ReturnsZipType()
    {
        // Arrange
        var content = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP header

        // Act
        var result = _detector.Detect(content, "test.zip");

        // Assert
        result.ContentType.Should().Be("application/zip");
        result.Category.Should().Be(FileCategory.Archive);
        result.SpecificFormat.Should().Be("ZIP");
    }

    #endregion

    #region AI Model Format Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_GgufFile_ReturnsGgufType()
    {
        // Arrange
        var content = CreateFileBuilder().WithGgufHeader().BuildContent();

        // Act
        var result = _detector.Detect(content, "model.gguf");

        // Assert
        result.ContentType.Should().Be("application/x-gguf");
        result.Category.Should().Be(FileCategory.ModelGguf);
        result.SpecificFormat.Should().Be("GGUF");
        result.Extension.Should().Be("gguf");
        result.Confidence.Should().BeGreaterThan(0.9);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_SafeTensorsFile_ReturnsSafeTensorsType()
    {
        // Arrange
        var content = CreateFileBuilder().WithSafeTensorsHeader().BuildContent();

        // Act
        var result = _detector.Detect(content, "model.safetensors");

        // Assert
        result.ContentType.Should().Be("application/x-safetensors");
        result.Category.Should().Be(FileCategory.ModelSafeTensors);
        result.SpecificFormat.Should().Be("SafeTensors");
        result.Extension.Should().Be("safetensors");
    }

    #endregion

    #region Audio Format Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_Mp3File_ReturnsMp3Type()
    {
        // Arrange
        var content = new byte[] { 0xFF, 0xFB, 0x90, 0x00 }; // MP3 frame sync

        // Act
        var result = _detector.Detect(content, "test.mp3");

        // Assert
        result.ContentType.Should().Be("audio/mpeg");
        result.Category.Should().Be(FileCategory.Audio);
        result.SpecificFormat.Should().Be("MP3");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_FlacFile_ReturnsFlacType()
    {
        // Arrange
        var content = System.Text.Encoding.ASCII.GetBytes("fLaC");

        // Act
        var result = _detector.Detect(content, "test.flac");

        // Assert
        result.ContentType.Should().Be("audio/flac");
        result.Category.Should().Be(FileCategory.Audio);
        result.SpecificFormat.Should().Be("FLAC");
    }

    #endregion

    #region Text Format Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_PlainText_ReturnsTextType()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("This is plain text content with multiple sentences.");

        // Act
        var result = _detector.Detect(content, "test.txt");

        // Assert
        result.ContentType.Should().Be("text/plain");
        result.Category.Should().Be(FileCategory.Text);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_JsonFile_ReturnsJsonType()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("{\"key\":\"value\"}");

        // Act
        var result = _detector.Detect(content, "test.json");

        // Assert
        result.ContentType.Should().Be("application/json");
        result.Category.Should().Be(FileCategory.Json);
        result.SpecificFormat.Should().Be("JSON");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_XmlFile_ReturnsXmlType()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><root></root>");

        // Act
        var result = _detector.Detect(content, "test.xml");

        // Assert
        result.ContentType.Should().Be("application/xml");
        result.Category.Should().Be(FileCategory.Xml);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_YamlFile_ReturnsYamlType()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("---\nkey: value\n");

        // Act
        var result = _detector.Detect(content, "test.yaml");

        // Assert
        result.ContentType.Should().Be("text/yaml");
        result.Category.Should().Be(FileCategory.Yaml);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_MarkdownFile_ReturnsMarkdownType()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("# Heading\n\nContent");

        // Act
        var result = _detector.Detect(content, "test.md");

        // Assert
        result.ContentType.Should().Be("text/markdown");
        result.Category.Should().Be(FileCategory.Markdown);
    }

    #endregion

    #region Executable Format Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_WindowsExe_ReturnsExeType()
    {
        // Arrange
        var content = new byte[] { 0x4D, 0x5A }; // MZ header

        // Act
        var result = _detector.Detect(content, "test.exe");

        // Assert
        result.ContentType.Should().Be("application/x-msdownload");
        result.Category.Should().Be(FileCategory.Executable);
        result.SpecificFormat.Should().Be("PE");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_ElfBinary_ReturnsElfType()
    {
        // Arrange
        var content = new byte[] { 0x7F, 0x45, 0x4C, 0x46 }; // ELF header

        // Act
        var result = _detector.Detect(content, "test.elf");

        // Assert
        result.ContentType.Should().Be("application/x-executable");
        result.Category.Should().Be(FileCategory.Executable);
        result.SpecificFormat.Should().Be("ELF");
    }

    #endregion

    #region Archive Format Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_GzipFile_ReturnsGzipType()
    {
        // Arrange
        var content = new byte[] { 0x1F, 0x8B, 0x08 };

        // Act
        var result = _detector.Detect(content, "test.gz");

        // Assert
        result.ContentType.Should().Be("application/gzip");
        result.Category.Should().Be(FileCategory.Archive);
        result.SpecificFormat.Should().Be("GZIP");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_Bzip2File_ReturnsBzip2Type()
    {
        // Arrange
        var content = new byte[] { 0x42, 0x5A, 0x68 };

        // Act
        var result = _detector.Detect(content, "test.bz2");

        // Assert
        result.ContentType.Should().Be("application/x-bzip2");
        result.Category.Should().Be(FileCategory.Archive);
        result.SpecificFormat.Should().Be("BZIP2");
    }

    #endregion

    #region Fallback and Edge Cases

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_UnknownBinaryData_ReturnsUnknownType()
    {
        // Arrange
        var content = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        // Act
        var result = _detector.Detect(content, "unknown.dat");

        // Assert
        result.ContentType.Should().Be("application/octet-stream");
        result.Category.Should().Be(FileCategory.Binary);
        result.SpecificFormat.Should().Be("unknown");
        result.Confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_EmptyContent_ReturnsUnknownType()
    {
        // Arrange
        var content = Array.Empty<byte>();

        // Act
        var result = _detector.Detect(content, "empty.bin");

        // Assert
        result.Category.Should().Be(FileCategory.Binary);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_NoMagicBytes_FallsBackToExtension()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("Plain text without magic bytes");

        // Act
        var result = _detector.Detect(content, "document.txt");

        // Assert
        result.ContentType.Should().Be("text/plain");
        result.Category.Should().Be(FileCategory.Text);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_Stream_WorksCorrectly()
    {
        // Arrange
        var content = CreateFileBuilder().WithPngHeader().BuildContent();
        using var stream = new MemoryStream(content);

        // Act
        var result = _detector.Detect(stream, "test.png");

        // Assert
        result.ContentType.Should().Be("image/png");
        result.Category.Should().Be(FileCategory.ImageRaster);
        stream.Position.Should().Be(0); // Stream position should be restored
    }

    #endregion

    #region Confidence Score Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_MagicByteMatch_HighConfidence()
    {
        // Arrange
        var content = CreateFileBuilder().WithPdfHeader().BuildContent();

        // Act
        var result = _detector.Detect(content, "test.pdf");

        // Assert
        result.Confidence.Should().BeGreaterThan(0.9);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_ExtensionOnlyMatch_MediumConfidence()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("Some text content");

        // Act
        var result = _detector.Detect(content, "document.txt");

        // Assert
        result.Confidence.Should().BeInRange(0.5, 0.7);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Detect_NoMatch_LowConfidence()
    {
        // Arrange
        var content = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        // Act
        var result = _detector.Detect(content);

        // Assert
        result.Confidence.Should().BeLessThan(0.5);
    }

    #endregion
}
