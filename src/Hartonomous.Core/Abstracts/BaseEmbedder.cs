using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Abstract base class for text embedders.
/// Provides common functionality and error handling.
/// </summary>
public abstract class BaseTextEmbedder : BaseService, ITextEmbedder
{
    protected BaseTextEmbedder(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }
    public abstract int EmbeddingDimension { get; }

    public abstract Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default);

    public virtual async Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var embedding = await EmbedTextAsync(text, cancellationToken);
                results.Add(embedding);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to embed text: {TextPreview}",
                    text.Length > 50 ? text[..50] + "..." : text);
                // Continue with other texts
            }
        }
        return results.ToArray();
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to embed a test string
            await EmbedTextAsync("test", cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for image embedders.
/// </summary>
public abstract class BaseImageEmbedder : BaseService, IImageEmbedder
{
    protected BaseImageEmbedder(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }
    public abstract int EmbeddingDimension { get; }

    public abstract Task<float[]> EmbedImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    public virtual async Task<float[][]> EmbedImagesAsync(IEnumerable<byte[]> imageBytes, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var image in imageBytes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var embedding = await EmbedImageAsync(image, cancellationToken);
                results.Add(embedding);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to embed image of {Size} bytes", image.Length);
                // Continue with other images
            }
        }
        return results.ToArray();
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to embed a minimal test image
            var testImage = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Minimal JPEG header
            await EmbedImageAsync(testImage, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for audio embedders.
/// </summary>
public abstract class BaseAudioEmbedder : BaseService, IAudioEmbedder
{
    protected BaseAudioEmbedder(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }
    public abstract int EmbeddingDimension { get; }

    public abstract Task<float[]> EmbedAudioAsync(byte[] audioBytes, CancellationToken cancellationToken = default);

    public virtual async Task<float[][]> EmbedAudiosAsync(IEnumerable<byte[]> audioBytes, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var audio in audioBytes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var embedding = await EmbedAudioAsync(audio, cancellationToken);
                results.Add(embedding);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to embed audio of {Size} bytes", audio.Length);
                // Continue with other audio samples
            }
        }
        return results.ToArray();
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to embed minimal test audio
            var testAudio = new byte[] { 0x00, 0x00 }; // Minimal audio data
            await EmbedAudioAsync(testAudio, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for video embedders.
/// </summary>
public abstract class BaseVideoEmbedder : BaseService, IVideoEmbedder
{
    protected BaseVideoEmbedder(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }
    public abstract int EmbeddingDimension { get; }

    public abstract Task<float[]> EmbedVideoFrameAsync(byte[] frameBytes, CancellationToken cancellationToken = default);

    public virtual async Task<float[][]> EmbedVideoFramesAsync(IEnumerable<byte[]> frameBytes, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var frame in frameBytes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var embedding = await EmbedVideoFrameAsync(frame, cancellationToken);
                results.Add(embedding);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to embed video frame of {Size} bytes", frame.Length);
                // Continue with other frames
            }
        }
        return results.ToArray();
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to embed minimal test frame
            var testFrame = new byte[] { 0x00, 0x00, 0x00 }; // Minimal frame data
            await EmbedVideoFrameAsync(testFrame, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Abstract base class for unified embedders.
/// Provides common functionality for all embedder types.
/// </summary>
public abstract class BaseEmbedder : BaseService, IEmbedder
{
    protected BaseEmbedder(ILogger logger) : base(logger) { }

    public abstract string ProviderName { get; }
    public abstract int EmbeddingDimension { get; }
    public abstract string Modality { get; }

    public abstract Task<float[]> EmbedAsync(object input, CancellationToken cancellationToken = default);

    public virtual async Task<float[][]> EmbedBatchAsync(IEnumerable<object> inputs, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var input in inputs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var embedding = await EmbedAsync(input, cancellationToken);
                results.Add(embedding);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to embed input of type {Type}", input.GetType().Name);
                // Continue with other inputs
            }
        }
        return results.ToArray();
    }

    public virtual async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to embed a test input
            var testInput = GetTestInput();
            await EmbedAsync(testInput, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get a test input for health checking.
    /// Override in derived classes to provide appropriate test data.
    /// </summary>
    protected abstract object GetTestInput();
}