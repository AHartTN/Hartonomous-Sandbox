using Hartonomous.Core.Interfaces.Ingestion;
using System.Text;

namespace Hartonomous.UnitTests.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating SourceMetadata test objects.
/// Simplifies source metadata creation with sensible defaults.
/// </summary>
public class TestSourceMetadataBuilder
{
    private string? _fileName = "test.txt";
    private string? _sourceUri = "upload://test.txt";
    private string _sourceType = "file-upload";
    private string _contentType = "text/plain";
    private long _sizeBytes = 1024;
    private int _tenantId = 1;
    private string? _metadata;

    /// <summary>
    /// Sets the filename.
    /// </summary>
    public TestSourceMetadataBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    /// <summary>
    /// Sets the source URI.
    /// </summary>
    public TestSourceMetadataBuilder WithSourceUri(string sourceUri)
    {
        _sourceUri = sourceUri;
        return this;
    }

    /// <summary>
    /// Sets the source type (e.g., "file-upload", "url-fetch", "database").
    /// </summary>
    public TestSourceMetadataBuilder WithSourceType(string sourceType)
    {
        _sourceType = sourceType;
        return this;
    }

    /// <summary>
    /// Sets the content type.
    /// </summary>
    public TestSourceMetadataBuilder WithContentType(string contentType)
    {
        _contentType = contentType;
        return this;
    }

    /// <summary>
    /// Sets the size in bytes.
    /// </summary>
    public TestSourceMetadataBuilder WithSizeBytes(long sizeBytes)
    {
        _sizeBytes = sizeBytes;
        return this;
    }

    /// <summary>
    /// Sets the tenant ID.
    /// </summary>
    public TestSourceMetadataBuilder WithTenantId(int tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Sets metadata as JSON string.
    /// </summary>
    public TestSourceMetadataBuilder WithMetadata(string metadata)
    {
        _metadata = metadata;
        return this;
    }

    /// <summary>
    /// Sets metadata from an object (serialized to JSON).
    /// </summary>
    public TestSourceMetadataBuilder WithMetadata(object metadataObject)
    {
        _metadata = System.Text.Json.JsonSerializer.Serialize(metadataObject);
        return this;
    }

    /// <summary>
    /// Configures as a text file upload source.
    /// </summary>
    public TestSourceMetadataBuilder AsTextFileUpload(string fileName = "document.txt", long sizeBytes = 2048)
    {
        _fileName = fileName;
        _sourceUri = $"upload://{fileName}";
        _sourceType = "file-upload";
        _contentType = "text/plain";
        _sizeBytes = sizeBytes;
        return this;
    }

    /// <summary>
    /// Configures as an image file upload source.
    /// </summary>
    public TestSourceMetadataBuilder AsImageFileUpload(string fileName = "image.png", long sizeBytes = 102400)
    {
        _fileName = fileName;
        _sourceUri = $"upload://{fileName}";
        _sourceType = "file-upload";
        _contentType = "image/png";
        _sizeBytes = sizeBytes;
        return this;
    }

    /// <summary>
    /// Configures as a URL fetch source.
    /// </summary>
    public TestSourceMetadataBuilder AsUrlFetch(string url = "https://example.com/document.html")
    {
        _fileName = Path.GetFileName(url) ?? "document.html";
        _sourceUri = url;
        _sourceType = "url-fetch";
        _contentType = "text/html";
        return this;
    }

    /// <summary>
    /// Configures as a database query source.
    /// </summary>
    public TestSourceMetadataBuilder AsDatabaseQuery(string tableName = "Users")
    {
        _fileName = $"{tableName}.sql";
        _sourceUri = $"database://{tableName}";
        _sourceType = "database-query";
        _contentType = "application/sql";
        return this;
    }

    /// <summary>
    /// Configures as a GGUF model file.
    /// </summary>
    public TestSourceMetadataBuilder AsGgufModel(string modelName = "llama-7b-q4.gguf", long sizeBytes = 3_700_000_000)
    {
        _fileName = modelName;
        _sourceUri = $"upload://{modelName}";
        _sourceType = "file-upload";
        _contentType = "application/x-gguf";
        _sizeBytes = sizeBytes;
        return this;
    }

    /// <summary>
    /// Configures as a SafeTensors model file.
    /// </summary>
    public TestSourceMetadataBuilder AsSafeTensorsModel(string modelName = "model.safetensors", long sizeBytes = 2_000_000_000)
    {
        _fileName = modelName;
        _sourceUri = $"upload://{modelName}";
        _sourceType = "file-upload";
        _contentType = "application/x-safetensors";
        _sizeBytes = sizeBytes;
        return this;
    }

    /// <summary>
    /// Builds the SourceMetadata with configured values.
    /// </summary>
    public SourceMetadata Build()
    {
        return new SourceMetadata
        {
            FileName = _fileName,
            SourceUri = _sourceUri,
            SourceType = _sourceType,
            ContentType = _contentType,
            SizeBytes = _sizeBytes,
            TenantId = _tenantId,
            Metadata = _metadata
        };
    }
}
