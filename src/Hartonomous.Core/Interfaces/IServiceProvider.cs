using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Vendor-agnostic interface for message publishers.
/// Allows swapping between different message queue providers (Event Hub, Service Bus, Kafka, etc.).
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Gets the name of the message publisher.
    /// </summary>
    string PublisherName { get; }

    /// <summary>
    /// Publish a single message.
    /// </summary>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the publish operation</returns>
    Task PublishAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish multiple messages in batch.
    /// </summary>
    /// <param name="messages">The messages to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the publish operation</returns>
    Task PublishBatchAsync(IEnumerable<object> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the publisher is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the publisher is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for model downloaders.
/// Allows swapping between different model repositories (Hugging Face, Ollama, Azure, etc.).
/// </summary>
public interface IModelDownloader
{
    /// <summary>
    /// Gets the name of the model downloader.
    /// </summary>
    string DownloaderName { get; }

    /// <summary>
    /// Download a model from the specified repository.
    /// </summary>
    /// <param name="modelIdentifier">The model identifier (e.g., "organization/model-name")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The local path to the downloaded model</returns>
    Task<string> DownloadModelAsync(string modelIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available models from the repository.
    /// </summary>
    /// <param name="filter">Optional filter for model names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available model identifiers</returns>
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(string? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the downloader is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the downloader is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for model format readers.
/// Allows swapping between different model format parsers (ONNX, PyTorch, Safetensors, etc.).
/// </summary>
public interface IModelFormatReader
{
    /// <summary>
    /// Gets the name of the format reader.
    /// </summary>
    string ReaderName { get; }

    /// <summary>
    /// Get the supported file extensions for this format.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Check if this reader can handle the given model file.
    /// </summary>
    /// <param name="modelPath">Path to the model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if this reader can handle the file</returns>
    Task<bool> CanReadAsync(string modelPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read and parse the model file.
    /// </summary>
    /// <param name="modelPath">Path to the model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The parsed model data</returns>
    Task<Model> ReadModelAsync(string modelPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the model file is valid for this format.
    /// </summary>
    /// <param name="modelPath">Path to the model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file is valid</returns>
    Task<bool> ValidateAsync(string modelPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for event processors.
/// Allows swapping between different event processing strategies.
/// </summary>
public interface IEventProcessor
{
    /// <summary>
    /// Gets the name of the event processor.
    /// </summary>
    string ProcessorName { get; }

    /// <summary>
    /// Process a single event.
    /// </summary>
    /// <param name="event">The event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing operation</returns>
    Task ProcessEventAsync(object @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process multiple events in batch.
    /// </summary>
    /// <param name="events">The events to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing operation</returns>
    Task ProcessEventsAsync(IEnumerable<object> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the processor is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the processor is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Vendor-agnostic interface for semantic enrichers.
/// Allows swapping between different enrichment strategies.
/// </summary>
public interface ISemanticEnricher
{
    /// <summary>
    /// Gets the name of the semantic enricher.
    /// </summary>
    string EnricherName { get; }

    /// <summary>
    /// Enrich an event with semantic information.
    /// </summary>
    /// <param name="event">The event to enrich</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The enriched event</returns>
    Task<object> EnrichAsync(object @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrich multiple events with semantic information.
    /// </summary>
    /// <param name="events">The events to enrich</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The enriched events</returns>
    Task<IEnumerable<object>> EnrichBatchAsync(IEnumerable<object> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the enricher is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the enricher is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}