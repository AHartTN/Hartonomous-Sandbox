using Hartonomous.Core.Models;

namespace Hartonomous.Core.Services;

/// <summary>
/// Service for inferring model capabilities from model names and metadata.
/// Provides centralized business logic for determining what a model can do.
/// </summary>
public interface IModelCapabilityService
{
    /// <summary>
    /// Infers the capabilities of a model based on its name.
    /// </summary>
    /// <param name="modelName">The name of the model (e.g., "gpt-4", "dall-e-3", "whisper-1")</param>
    /// <returns>A ModelCapabilities object describing what the model can do</returns>
    ModelCapabilities InferFromModelName(string modelName);
    
    /// <summary>
    /// Determines if a model supports a specific capability.
    /// </summary>
    /// <param name="modelName">The model name to check</param>
    /// <param name="capability">The capability to check for</param>
    /// <returns>True if the model supports the capability; otherwise false</returns>
    bool SupportsCapability(string modelName, string capability);
    
    /// <summary>
    /// Gets the primary modality of a model (text, image, audio, multimodal).
    /// </summary>
    /// <param name="modelName">The model name</param>
    /// <returns>The primary modality</returns>
    string GetPrimaryModality(string modelName);
}
