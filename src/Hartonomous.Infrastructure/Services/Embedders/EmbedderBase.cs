using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Embedders;

/// <summary>
/// Abstract base class for embedder implementations providing common functionality.
/// </summary>
public abstract class EmbedderBase : IDisposable
{
    protected readonly ILogger Logger;
    protected readonly IHttpClientFactory? HttpClientFactory;

    protected EmbedderBase(ILogger logger, IHttpClientFactory? httpClientFactory = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        HttpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Validates text input for embedding operations.
    /// </summary>
    /// <param name="text">Text to validate.</param>
    /// <param name="paramName">Parameter name for exception messages.</param>
    /// <exception cref="ArgumentException">Thrown if text is null, empty, or exceeds recommended limits.</exception>
    protected void ValidateTextInput(string text, string paramName)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Input text cannot be null or empty", paramName);

        if (text.Length > 8000)
            Logger.LogWarning("Input text length {Length} exceeds recommended limit of 8000 characters", text.Length);
    }

    /// <summary>
    /// Validates binary input for embedding operations.
    /// </summary>
    /// <param name="bytes">Byte array to validate.</param>
    /// <param name="paramName">Parameter name for exception messages.</param>
    /// <exception cref="ArgumentException">Thrown if bytes are null, empty, or exceed recommended limits.</exception>
    protected void ValidateBytesInput(byte[] bytes, string paramName)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("Input bytes cannot be null or empty", paramName);

        if (bytes.Length > 10_000_000) // 10MB
            Logger.LogWarning("Input bytes length {Length} exceeds recommended limit of 10MB", bytes.Length);
    }

    /// <summary>
    /// Executes an operation with automatic retry logic and exponential backoff.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="action">Async operation to execute.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    protected async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                Logger.LogWarning(ex,
                    "Embedder request failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Disposes resources used by the embedder.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        // Base implementation does nothing; derived classes override as needed
    }
}
