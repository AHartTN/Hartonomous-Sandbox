using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Data.Entities.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Hartonomous.UnitTests.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating AtomData test objects.
/// Simplifies atom creation with sensible defaults.
/// </summary>
public class TestAtomDataBuilder
{
    private byte[]? _atomicValue;
    private byte[]? _contentHash;
    private string _modality = "text";
    private string? _subtype = "chunk";
    private string? _contentType = "text/plain";
    private string? _canonicalText;
    private string? _metadata;

    /// <summary>
    /// Sets the atomic value from a string (UTF-8 encoded, max 64 bytes).
    /// </summary>
    public TestAtomDataBuilder WithAtomicValue(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        _atomicValue = bytes.Length <= 64 ? bytes : bytes.Take(64).ToArray();
        _canonicalText = value;
        return this;
    }

    /// <summary>
    /// Sets the atomic value from raw bytes (max 64 bytes).
    /// </summary>
    public TestAtomDataBuilder WithAtomicValue(byte[] value)
    {
        _atomicValue = value.Length <= 64 ? value : value.Take(64).ToArray();
        return this;
    }

    /// <summary>
    /// Sets a custom content hash. If not set, hash will be computed from atomic value.
    /// </summary>
    public TestAtomDataBuilder WithContentHash(byte[] hash)
    {
        _contentHash = hash;
        return this;
    }

    /// <summary>
    /// Sets the modality (e.g., "text", "image", "audio", "video", "code").
    /// </summary>
    public TestAtomDataBuilder WithModality(string modality)
    {
        _modality = modality;
        return this;
    }

    /// <summary>
    /// Configures as a text atom.
    /// </summary>
    public TestAtomDataBuilder AsText(string text = "Sample text atom")
    {
        _modality = "text";
        _subtype = "chunk";
        _contentType = "text/plain";
        WithAtomicValue(text);
        return this;
    }

    /// <summary>
    /// Configures as an image atom.
    /// </summary>
    public TestAtomDataBuilder AsImage()
    {
        _modality = "image";
        _subtype = "pixel-block";
        _contentType = "image/png";
        _atomicValue = new byte[64]; // Dummy pixel data
        _canonicalText = "Image atom";
        return this;
    }

    /// <summary>
    /// Configures as a code atom.
    /// </summary>
    public TestAtomDataBuilder AsCode(string code = "function test() { return true; }")
    {
        _modality = "code";
        _subtype = "function";
        _contentType = "text/x-csharp";
        WithAtomicValue(code);
        return this;
    }

    /// <summary>
    /// Configures as an audio atom.
    /// </summary>
    public TestAtomDataBuilder AsAudio()
    {
        _modality = "audio";
        _subtype = "waveform-segment";
        _contentType = "audio/wav";
        _atomicValue = new byte[64]; // Dummy audio data
        _canonicalText = "Audio atom";
        return this;
    }

    /// <summary>
    /// Configures as a video atom.
    /// </summary>
    public TestAtomDataBuilder AsVideo()
    {
        _modality = "video";
        _subtype = "frame";
        _contentType = "video/mp4";
        _atomicValue = new byte[64]; // Dummy video data
        _canonicalText = "Video frame";
        return this;
    }

    /// <summary>
    /// Sets the subtype.
    /// </summary>
    public TestAtomDataBuilder WithSubtype(string? subtype)
    {
        _subtype = subtype;
        return this;
    }

    /// <summary>
    /// Sets the content type.
    /// </summary>
    public TestAtomDataBuilder WithContentType(string? contentType)
    {
        _contentType = contentType;
        return this;
    }

    /// <summary>
    /// Sets the canonical text representation.
    /// </summary>
    public TestAtomDataBuilder WithCanonicalText(string? canonicalText)
    {
        _canonicalText = canonicalText;
        return this;
    }

    /// <summary>
    /// Sets metadata as JSON string.
    /// </summary>
    public TestAtomDataBuilder WithMetadata(string? metadata)
    {
        _metadata = metadata;
        return this;
    }

    /// <summary>
    /// Sets metadata from an object (serialized to JSON).
    /// </summary>
    public TestAtomDataBuilder WithMetadata(object metadataObject)
    {
        _metadata = System.Text.Json.JsonSerializer.Serialize(metadataObject);
        return this;
    }

    /// <summary>
    /// Builds the AtomData with configured values.
    /// </summary>
    public AtomData Build()
    {
        // Ensure we have atomic value
        if (_atomicValue == null)
        {
            _atomicValue = Encoding.UTF8.GetBytes("Default atom content").Take(64).ToArray();
            _canonicalText ??= "Default atom content";
        }

        // Compute content hash if not provided
        if (_contentHash == null)
        {
            _contentHash = SHA256.HashData(_atomicValue);
        }

        return new AtomData
        {
            AtomicValue = _atomicValue,
            ContentHash = _contentHash,
            Modality = _modality,
            Subtype = _subtype,
            ContentType = _contentType,
            CanonicalText = _canonicalText,
            Metadata = _metadata
        };
    }

    /// <summary>
    /// Builds multiple atoms with sequential content.
    /// </summary>
    public List<AtomData> BuildMany(int count)
    {
        var atoms = new List<AtomData>();
        for (int i = 0; i < count; i++)
        {
            var builder = new TestAtomDataBuilder()
                .WithModality(_modality)
                .WithSubtype(_subtype)
                .WithContentType(_contentType)
                .WithAtomicValue($"{_canonicalText ?? "Atom"} {i}")
                .WithMetadata(_metadata);

            atoms.Add(builder.Build());
        }
        return atoms;
    }
}
