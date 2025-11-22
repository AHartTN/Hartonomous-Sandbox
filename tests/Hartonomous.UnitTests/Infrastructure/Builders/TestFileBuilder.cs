using System.Text;

namespace Hartonomous.UnitTests.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating test file data.
/// Supports various file types and content scenarios.
/// </summary>
public class TestFileBuilder
{
    private byte[]? _content;
    private string _fileName = "test.txt";
    private int _sizeBytes = 0;

    /// <summary>
    /// Sets UTF-8 text content.
    /// </summary>
    public TestFileBuilder WithTextContent(string text)
    {
        _content = Encoding.UTF8.GetBytes(text);
        _sizeBytes = _content.Length;
        return this;
    }

    /// <summary>
    /// Creates a PNG file with valid header.
    /// </summary>
    public TestFileBuilder WithPngHeader()
    {
        _content = new byte[] 
        { 
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D,  // IHDR length
            0x49, 0x48, 0x44, 0x52   // IHDR type
        };
        _fileName = "test.png";
        _sizeBytes = _content.Length;
        return this;
    }

    /// <summary>
    /// Creates a JPEG file with valid header.
    /// </summary>
    public TestFileBuilder WithJpegHeader()
    {
        _content = new byte[] 
        { 
            0xFF, 0xD8, 0xFF, 0xE0, // JPEG signature
            0x00, 0x10, 0x4A, 0x46,
            0x49, 0x46, 0x00, 0x01
        };
        _fileName = "test.jpg";
        _sizeBytes = _content.Length;
        return this;
    }

    /// <summary>
    /// Creates a PDF file with valid header.
    /// </summary>
    public TestFileBuilder WithPdfHeader()
    {
        _content = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        _fileName = "test.pdf";
        _sizeBytes = _content.Length;
        return this;
    }

    /// <summary>
    /// Creates a GGUF model file header.
    /// </summary>
    public TestFileBuilder WithGgufHeader()
    {
        _content = Encoding.ASCII.GetBytes("GGUF");
        _fileName = "test.gguf";
        _sizeBytes = _content.Length;
        return this;
    }

    /// <summary>
    /// Creates a SafeTensors model file header.
    /// </summary>
    public TestFileBuilder WithSafeTensorsHeader()
    {
        _content = Encoding.ASCII.GetBytes("safetens");
        _fileName = "test.safetensors";
        _sizeBytes = _content.Length;
        return this;
    }

    /// <summary>
    /// Creates empty file content.
    /// </summary>
    public TestFileBuilder WithEmptyContent()
    {
        _content = Array.Empty<byte>();
        _sizeBytes = 0;
        return this;
    }

    /// <summary>
    /// Creates a file with specified size (filled with zeros).
    /// </summary>
    public TestFileBuilder WithSize(int sizeBytes)
    {
        _content = new byte[sizeBytes];
        _sizeBytes = sizeBytes;
        return this;
    }

    /// <summary>
    /// Sets custom raw bytes.
    /// </summary>
    public TestFileBuilder WithRawBytes(byte[] bytes)
    {
        _content = bytes;
        _sizeBytes = bytes.Length;
        return this;
    }

    /// <summary>
    /// Sets the filename.
    /// </summary>
    public TestFileBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    /// <summary>
    /// Builds the test file data.
    /// </summary>
    /// <returns>Tuple of (content bytes, filename)</returns>
    public (byte[] content, string fileName) Build()
    {
        return (_content ?? Array.Empty<byte>(), _fileName);
    }

    /// <summary>
    /// Builds just the content bytes.
    /// </summary>
    public byte[] BuildContent()
    {
        return _content ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Builds just the filename.
    /// </summary>
    public string BuildFileName()
    {
        return _fileName;
    }
}
