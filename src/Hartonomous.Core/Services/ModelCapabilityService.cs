using Hartonomous.Core.Models;

namespace Hartonomous.Core.Services;

/// <summary>
/// Default implementation of model capability inference.
/// Extracts business logic for model capability determination from infrastructure layer.
/// </summary>
public class ModelCapabilityService : IModelCapabilityService
{
    public ModelCapabilities InferFromModelName(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return new ModelCapabilities();
        }

        var lowerName = modelName.ToLowerInvariant();
        
        // Image generation models
        if (lowerName.Contains("dall-e") || lowerName.Contains("dalle") || lowerName.Contains("stable-diffusion"))
        {
            return new ModelCapabilities
            {
                SupportsImageGeneration = true,
                PrimaryModality = "image",
                MaxTokens = 4000
            };
        }
        
        // Audio models
        if (lowerName.Contains("whisper") || lowerName.Contains("tts"))
        {
            return new ModelCapabilities
            {
                SupportsAudioGeneration = lowerName.Contains("tts"),
                PrimaryModality = "audio",
                MaxTokens = 25000
            };
        }
        
        // Embedding models
        if (lowerName.Contains("embedding") || lowerName.Contains("ada"))
        {
            return new ModelCapabilities
            {
                SupportsEmbeddings = true,
                PrimaryModality = "text",
                MaxTokens = 8191
            };
        }
        
        // Vision-capable text models
        if (lowerName.Contains("gpt-4") && (lowerName.Contains("vision") || lowerName.Contains("v")))
        {
            return new ModelCapabilities
            {
                SupportsTextGeneration = true,
                SupportsVisionAnalysis = true,
                SupportsFunctionCalling = true,
                SupportsStreaming = true,
                PrimaryModality = "multimodal",
                MaxTokens = 4096,
                MaxContextWindow = lowerName.Contains("32k") ? 32768 : 128000
            };
        }
        
        // Standard GPT-4 models
        if (lowerName.Contains("gpt-4"))
        {
            return new ModelCapabilities
            {
                SupportsTextGeneration = true,
                SupportsFunctionCalling = true,
                SupportsStreaming = true,
                PrimaryModality = "text",
                MaxTokens = 4096,
                MaxContextWindow = lowerName.Contains("32k") ? 32768 : 8192
            };
        }
        
        // GPT-3.5 models
        if (lowerName.Contains("gpt-3.5") || lowerName.Contains("gpt-35"))
        {
            return new ModelCapabilities
            {
                SupportsTextGeneration = true,
                SupportsFunctionCalling = lowerName.Contains("turbo"),
                SupportsStreaming = true,
                PrimaryModality = "text",
                MaxTokens = 4096,
                MaxContextWindow = lowerName.Contains("16k") ? 16384 : 4096
            };
        }
        
        // Default text model
        return new ModelCapabilities
        {
            SupportsTextGeneration = true,
            PrimaryModality = "text",
            MaxTokens = 2048,
            MaxContextWindow = 4096
        };
    }

    public bool SupportsCapability(string modelName, string capability)
    {
        var capabilities = InferFromModelName(modelName);
        return capability.ToLowerInvariant() switch
        {
            "text" or "textgeneration" => capabilities.SupportsTextGeneration,
            "image" or "imagegeneration" => capabilities.SupportsImageGeneration,
            "audio" or "audiogeneration" => capabilities.SupportsAudioGeneration,
            "embedding" or "embeddings" => capabilities.SupportsEmbeddings,
            "vision" or "visionanalysis" => capabilities.SupportsVisionAnalysis,
            "function" or "functioncalling" => capabilities.SupportsFunctionCalling,
            "streaming" => capabilities.SupportsStreaming,
            _ => false
        };
    }

    public string GetPrimaryModality(string modelName)
    {
        return InferFromModelName(modelName).PrimaryModality;
    }
}
