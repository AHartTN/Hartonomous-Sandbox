using Hartonomous.Core.Models;

namespace Hartonomous.Core.Services;

/// <summary>
/// Service for querying model capabilities from ingested model metadata.
/// Database-native: queries Model.Metadata.SupportedTasks/SupportedModalities instead of hardcoding.
/// </summary>
public interface IModelCapabilityService
{
    /// <summary>
    /// Gets the capabilities of a model by querying its metadata from the database.
    /// </summary>
    /// <param name="modelName">The name of the model (e.g., "llama-3.1-70b", "stable-diffusion-xl")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A ModelCapabilities object derived from Model.Metadata.SupportedTasks/SupportedModalities, or default capabilities if not found</returns>
    Task<ModelCapabilities> GetCapabilitiesAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a model supports a specific capability by querying its metadata.
    /// </summary>
    /// <param name="modelName">The model name to check</param>
    /// <param name="capability">The capability to check for (e.g., "text-generation", "image-generation")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the model supports the capability; otherwise false</returns>
    Task<bool> SupportsCapabilityAsync(string modelName, string capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary modality of a model from its metadata.
    /// </summary>
    /// <param name="modelName">The model name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The primary modality from SupportedModalities JSON, or "text" if not found</returns>
    Task<string> GetPrimaryModalityAsync(string modelName, CancellationToken cancellationToken = default);
}
