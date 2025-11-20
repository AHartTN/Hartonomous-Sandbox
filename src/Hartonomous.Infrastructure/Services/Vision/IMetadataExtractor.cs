namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Interface for all metadata extractors.
/// Enables polymorphic extraction and format detection.
/// </summary>
public interface IMetadataExtractor
{
    /// <summary>
    /// Check if this extractor can handle the given data.
    /// </summary>
    bool CanHandle(byte[] data);
    
    /// <summary>
    /// Extract metadata from data.
    /// </summary>
    MediaMetadata ExtractMetadata(byte[] data);
}
