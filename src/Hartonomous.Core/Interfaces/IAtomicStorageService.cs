namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Service interface for atomic component storage.
/// Implements content-addressable storage for pixels, audio samples, and text tokens.
/// </summary>
public interface IAtomicStorageService
{
    /// <summary>
    /// Store an atomic pixel (RGBA) with deduplication.
    /// Returns existing ID if pixel already exists.
    /// </summary>
    /// <param name="r">Red channel (0-255)</param>
    /// <param name="g">Green channel (0-255)</param>
    /// <param name="b">Blue channel (0-255)</param>
    /// <param name="a">Alpha channel (0-255)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pixel ID (new or existing)</returns>
    Task<long> StoreAtomicPixelAsync(byte r, byte g, byte b, byte a, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store an atomic audio sample with deduplication.
    /// Returns existing ID if sample already exists.
    /// </summary>
    /// <param name="amplitude">Sample amplitude (normalized -1.0 to 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sample ID (new or existing)</returns>
    Task<long> StoreAtomicAudioSampleAsync(float amplitude, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store an atomic text token with deduplication.
    /// Returns existing ID if token already exists.
    /// </summary>
    /// <param name="tokenText">Text content of token</param>
    /// <param name="vocabId">Optional vocabulary ID if token is in model vocabulary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token ID (new or existing)</returns>
    Task<long> StoreAtomicTextTokenAsync(string tokenText, int? vocabId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store a batch of atomic pixels for efficient bulk operations.
    /// </summary>
    /// <param name="pixels">Collection of pixels to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of pixel IDs</returns>
    Task<IEnumerable<long>> StoreBatchPixelsAsync(
        IEnumerable<(byte r, byte g, byte b, byte a)> pixels,
        CancellationToken cancellationToken = default);
}
