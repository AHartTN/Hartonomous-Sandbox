using System.Text.Json;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;

using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Services;

/// <summary>
/// Database-native implementation of model capability queries.
/// Queries Model.Metadata.SupportedTasks/SupportedModalities and converts to type-safe enums.
/// ARCHITECTURAL FIX: No hardcoded third-party model names (gpt-4, dall-e, whisper).
/// </summary>
public class ModelCapabilityService : IModelCapabilityService
{
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<ModelCapabilityService> _logger;

    // Default capabilities for models without metadata
    private static readonly ModelCapabilities DefaultCapabilities = new()
    {
        SupportedTasks = TaskType.TextGeneration,
        SupportedModalities = Modality.Text,
        MaxTokens = 2048,
        MaxContextWindow = 4096
    };

    public ModelCapabilityService(IModelRepository modelRepository, ILogger<ModelCapabilityService> logger)
    {
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<ModelCapabilities> GetCapabilitiesAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return DefaultCapabilities;
        }

        try
        {
            // Query model from database (includes Metadata navigation property)
            var model = await _modelRepository.GetByNameAsync(modelName, cancellationToken);

            if (model?.Metadata == null)
            {
                _logger.LogWarning("Model '{ModelName}' not found or has no metadata. Returning default capabilities.", modelName);
                return DefaultCapabilities;
            }

            // Parse JSON metadata into type-safe enums
            var supportedTasks = EnumExtensions.ParseTaskTypes(model.Metadata.SupportedTasks);
            var supportedModalities = EnumExtensions.ParseModalities(model.Metadata.SupportedModalities);

            // Build capabilities from database metadata
            return new ModelCapabilities
            {
                SupportedTasks = supportedTasks,
                SupportedModalities = supportedModalities,
                MaxTokens = model.Metadata.MaxOutputLength ?? 2048,
                MaxContextWindow = model.Metadata.MaxInputLength ?? 4096,
                EmbeddingDimension = model.Metadata.EmbeddingDimension
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying capabilities for model '{ModelName}'", modelName);
            return DefaultCapabilities;
        }
    }

    public async Task<bool> SupportsCapabilityAsync(string modelName, string capability, CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(modelName, cancellationToken);

        // Try parsing capability as TaskType first
        var taskType = capability.ToTaskType();
        if (taskType != TaskType.None)
        {
            return capabilities.SupportsTask(taskType);
        }

        // Try parsing as Modality
        var modality = capability.ToModality();
        if (modality != Modality.None)
        {
            return capabilities.SupportsModality(modality);
        }

        _logger.LogWarning("Unrecognized capability string: '{Capability}'", capability);
        return false;
    }

    public async Task<string> GetPrimaryModalityAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(modelName, cancellationToken);
        return capabilities.PrimaryModality.ToJsonString();
    }
}
