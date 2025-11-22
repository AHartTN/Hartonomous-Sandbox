using Hartonomous.Core.Interfaces.Ingestion;
using Moq;

namespace Hartonomous.UnitTests.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating mock IFileTypeDetector in tests.
/// Simplifies file type detection mocking with sensible defaults.
/// </summary>
public class MockFileTypeDetectorBuilder
{
    private string _contentType = "text/plain";
    private FileCategory _category = FileCategory.Text;
    private string _specificFormat = "plain-text";
    private double _confidence = 0.95;
    private string _extension = "txt";
    private string? _metadata;
    private bool _throwOnDetect;

    /// <summary>
    /// Configures the detector to return text/plain.
    /// </summary>
    public MockFileTypeDetectorBuilder AsTextPlain()
    {
        _contentType = "text/plain";
        _category = FileCategory.Text;
        _specificFormat = "plain-text";
        _extension = "txt";
        return this;
    }

    /// <summary>
    /// Configures the detector to return image/png.
    /// </summary>
    public MockFileTypeDetectorBuilder AsPng()
    {
        _contentType = "image/png";
        _category = FileCategory.ImageRaster;
        _specificFormat = "PNG";
        _extension = "png";
        return this;
    }

    /// <summary>
    /// Configures the detector to return image/jpeg.
    /// </summary>
    public MockFileTypeDetectorBuilder AsJpeg()
    {
        _contentType = "image/jpeg";
        _category = FileCategory.ImageRaster;
        _specificFormat = "JPEG";
        _extension = "jpg";
        return this;
    }

    /// <summary>
    /// Configures the detector to return application/pdf.
    /// </summary>
    public MockFileTypeDetectorBuilder AsPdf()
    {
        _contentType = "application/pdf";
        _category = FileCategory.DocumentPdf;
        _specificFormat = "PDF";
        _extension = "pdf";
        return this;
    }

    /// <summary>
    /// Configures the detector to return application/x-gguf.
    /// </summary>
    public MockFileTypeDetectorBuilder AsGguf()
    {
        _contentType = "application/x-gguf";
        _category = FileCategory.ModelGguf;
        _specificFormat = "GGUF";
        _extension = "gguf";
        return this;
    }

    /// <summary>
    /// Configures the detector to return application/x-safetensors.
    /// </summary>
    public MockFileTypeDetectorBuilder AsSafeTensors()
    {
        _contentType = "application/x-safetensors";
        _category = FileCategory.ModelSafeTensors;
        _specificFormat = "SafeTensors";
        _extension = "safetensors";
        return this;
    }

    /// <summary>
    /// Configures the detector to return unknown file type.
    /// </summary>
    public MockFileTypeDetectorBuilder AsUnknown()
    {
        _contentType = "application/octet-stream";
        _category = FileCategory.Unknown;
        _specificFormat = "unknown";
        _extension = "bin";
        _confidence = 0.3;
        return this;
    }

    /// <summary>
    /// Sets custom content type and category.
    /// </summary>
    public MockFileTypeDetectorBuilder WithContentType(string contentType, FileCategory category)
    {
        _contentType = contentType;
        _category = category;
        return this;
    }

    /// <summary>
    /// Sets the confidence score (0.0 to 1.0).
    /// </summary>
    public MockFileTypeDetectorBuilder WithConfidence(double confidence)
    {
        _confidence = Math.Clamp(confidence, 0.0, 1.0);
        return this;
    }

    /// <summary>
    /// Sets the file extension.
    /// </summary>
    public MockFileTypeDetectorBuilder WithExtension(string extension)
    {
        _extension = extension.TrimStart('.');
        return this;
    }

    /// <summary>
    /// Sets optional metadata.
    /// </summary>
    public MockFileTypeDetectorBuilder WithMetadata(string metadata)
    {
        _metadata = metadata;
        return this;
    }

    /// <summary>
    /// Configures the detector to throw an exception when Detect is called.
    /// </summary>
    public MockFileTypeDetectorBuilder ThrowOnDetect()
    {
        _throwOnDetect = true;
        return this;
    }

    /// <summary>
    /// Builds the mock IFileTypeDetector with configured behavior.
    /// </summary>
    public IFileTypeDetector Build()
    {
        var mock = new Mock<IFileTypeDetector>();

        var fileTypeInfo = new FileTypeInfo
        {
            ContentType = _contentType,
            Category = _category,
            SpecificFormat = _specificFormat,
            Confidence = _confidence,
            Extension = _extension,
            Metadata = _metadata
        };

        // Detect with ReadOnlySpan<byte>
        mock.Setup(x => x.Detect(It.IsAny<ReadOnlySpan<byte>>(), It.IsAny<string>()))
            .Returns(() =>
            {
                if (_throwOnDetect)
                    throw new InvalidOperationException("Mock configured to throw on Detect");
                return fileTypeInfo;
            });

        // Detect with Stream
        mock.Setup(x => x.Detect(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(() =>
            {
                if (_throwOnDetect)
                    throw new InvalidOperationException("Mock configured to throw on Detect");
                return fileTypeInfo;
            });

        return mock.Object;
    }
}
