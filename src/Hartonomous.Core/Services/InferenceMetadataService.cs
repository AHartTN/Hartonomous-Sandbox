namespace Hartonomous.Core.Services;

/// <summary>
/// Default implementation of inference metadata determination.
/// Extracts business logic for inference parameter calculation from infrastructure layer.
/// </summary>
public class InferenceMetadataService : IInferenceMetadataService
{
    public string DetermineReasoningMode(string taskDescription, bool requiresMultiStep)
    {
        if (requiresMultiStep)
        {
            return "chain-of-thought";
        }

        if (string.IsNullOrWhiteSpace(taskDescription))
        {
            return "direct";
        }

        var lowerTask = taskDescription.ToLowerInvariant();
        
        // Complex reasoning indicators
        if (lowerTask.Contains("analyze") || lowerTask.Contains("reason") || 
            lowerTask.Contains("explain") || lowerTask.Contains("compare"))
        {
            return "analytical";
        }
        
        // Creative tasks
        if (lowerTask.Contains("create") || lowerTask.Contains("generate") || 
            lowerTask.Contains("design") || lowerTask.Contains("write"))
        {
            return "creative";
        }
        
        return "direct";
    }

    public int CalculateComplexity(int inputTokenCount, bool requiresMultiModal, bool requiresToolUse)
    {
        int complexity = 1;
        
        // Base complexity from token count
        if (inputTokenCount > 8000) complexity += 4;
        else if (inputTokenCount > 4000) complexity += 3;
        else if (inputTokenCount > 2000) complexity += 2;
        else if (inputTokenCount > 1000) complexity += 1;
        
        // Multi-modal adds complexity
        if (requiresMultiModal) complexity += 2;
        
        // Tool use adds complexity
        if (requiresToolUse) complexity += 2;
        
        // Cap at 10
        return Math.Min(complexity, 10);
    }

    public string DetermineSla(string priority, int complexity)
    {
        var lowerPriority = priority?.ToLowerInvariant() ?? "medium";
        
        if (lowerPriority == "critical" || (lowerPriority == "high" && complexity <= 3))
        {
            return "realtime";
        }
        
        if (lowerPriority == "high" || (lowerPriority == "medium" && complexity <= 5))
        {
            return "expedited";
        }
        
        return "standard";
    }

    public int EstimateResponseTime(string modelName, int complexity)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return complexity * 5; // Base estimate
        }

        var lowerName = modelName.ToLowerInvariant();
        
        // Fast models (GPT-3.5, embeddings)
        if (lowerName.Contains("gpt-3.5") || lowerName.Contains("gpt-35") || 
            lowerName.Contains("embedding") || lowerName.Contains("ada"))
        {
            return complexity * 2;
        }
        
        // Medium speed (GPT-4)
        if (lowerName.Contains("gpt-4"))
        {
            return complexity * 5;
        }
        
        // Slower models (image generation, audio)
        if (lowerName.Contains("dall-e") || lowerName.Contains("stable-diffusion") || 
            lowerName.Contains("whisper") || lowerName.Contains("tts"))
        {
            return complexity * 10;
        }
        
        // Default estimate
        return complexity * 5;
    }
}
