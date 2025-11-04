using System.Text.Json;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Performance;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Services;

/// <summary>
/// Database-native implementation of model capability queries.
/// Queries Model.Metadata.SupportedTasks/SupportedModalities instead of hardcoding model names.
/// ARCHITECTURAL FIX: No hardcoded third-party model names (gpt-4, dall-e, whisper).
/// </summary>
public class ModelCapabilityService : IModelCapabilityService
{
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<ModelCapabilityService> _logger;

    // Default capabilities for models without metadata
    private static readonly ModelCapabilities DefaultCapabilities = new()
    {
        SupportsTextGeneration = true,
        PrimaryModality = "text",
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

            // Parse JSON metadata
            var supportedTasks = ParseJsonArray(model.Metadata.SupportedTasks);
            var supportedModalities = ParseJsonArray(model.Metadata.SupportedModalities);

            // Build capabilities from database metadata
            return new ModelCapabilities
            {
                SupportsTextGeneration = ContainsTask(supportedTasks, "text-generation"),
                SupportsImageGeneration = ContainsTask(supportedTasks, "image-generation"),
                SupportsAudioGeneration = ContainsTask(supportedTasks, "audio-generation") || 
                                          ContainsTask(supportedTasks, "text-to-speech"),
                SupportsVideoGeneration = ContainsTask(supportedTasks, "video-generation"),
                SupportsEmbeddings = ContainsTask(supportedTasks, "embeddings") || 
                                     ContainsTask(supportedTasks, "feature-extraction"),
                SupportsVisionAnalysis = ContainsTask(supportedTasks, "image-to-text") || 
                                         ContainsTask(supportedTasks, "vision"),
                SupportsFunctionCalling = ContainsTask(supportedTasks, "function-calling") || 
                                          ContainsTask(supportedTasks, "tool-use"),
                SupportsStreaming = ContainsTask(supportedTasks, "streaming"),
                
                PrimaryModality = supportedModalities?.FirstOrDefault() ?? "text",
                MaxTokens = model.Metadata.MaxOutputLength ?? 2048,
                MaxContextWindow = model.Metadata.MaxInputLength ?? 4096
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
        
        // OPTIMIZED: Use AsSpan() for zero-allocation comparison
        var capabilitySpan = capability.AsSpan();
        
        if (capabilitySpan.Equals("text".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("text-generation".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsTextGeneration;
        
        if (capabilitySpan.Equals("image".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("image-generation".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsImageGeneration;
        
        if (capabilitySpan.Equals("audio".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("audio-generation".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsAudioGeneration;
        
        if (capabilitySpan.Equals("video".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("video-generation".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsVideoGeneration;
        
        if (capabilitySpan.Equals("embedding".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("embeddings".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsEmbeddings;
        
        if (capabilitySpan.Equals("vision".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("vision-analysis".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsVisionAnalysis;
        
        if (capabilitySpan.Equals("function".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            capabilitySpan.Equals("function-calling".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsFunctionCalling;
        
        if (capabilitySpan.Equals("streaming".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return capabilities.SupportsStreaming;
        
        return false;
    }

    public async Task<string> GetPrimaryModalityAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(modelName, cancellationToken);
        return capabilities.PrimaryModality;
    }

    // HELPER: Parse JSON array with fallback
    private string[]? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<string[]>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON array: {Json}", json);
            return null;
        }
    }

    // HELPER: Case-insensitive task check (zero-allocation with Span)
    private static bool ContainsTask(string[]? tasks, string taskName)
    {
        if (tasks == null || tasks.Length == 0)
            return false;

        var taskSpan = taskName.AsSpan();
        foreach (var task in tasks)
        {
            if (task.AsSpan().Equals(taskSpan, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
