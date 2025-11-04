using Hartonomous.Core.Performance;

namespace Hartonomous.Core.Services;

/// <summary>
/// Default implementation of inference metadata determination.
/// OPTIMIZED: Zero-allocation string operations with ReadOnlySpan&lt;char&gt;.
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

        // OPTIMIZED: Use AsSpan() for zero-allocation case-insensitive search
        var taskSpan = taskDescription.AsSpan();
        
        // Complex reasoning indicators
        if (StringUtilities.ContainsIgnoreCase(taskSpan, "analyze") || 
            StringUtilities.ContainsIgnoreCase(taskSpan, "reason") || 
            StringUtilities.ContainsIgnoreCase(taskSpan, "explain") || 
            StringUtilities.ContainsIgnoreCase(taskSpan, "compare"))
        {
            return "analytical";
        }
        
        // Creative tasks
        if (StringUtilities.ContainsIgnoreCase(taskSpan, "create") || 
            StringUtilities.ContainsIgnoreCase(taskSpan, "generate") || 
            StringUtilities.ContainsIgnoreCase(taskSpan, "design") || 
            StringUtilities.ContainsIgnoreCase(taskSpan, "write"))
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
        // OPTIMIZED: Use AsSpan() for zero-allocation comparison
        var prioritySpan = priority.AsSpan();
        
        // Case-insensitive equality using Span
        if (prioritySpan.Equals("critical".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            (prioritySpan.Equals("high".AsSpan(), StringComparison.OrdinalIgnoreCase) && complexity <= 3))
        {
            return "realtime";
        }
        
        if (prioritySpan.Equals("high".AsSpan(), StringComparison.OrdinalIgnoreCase) || 
            (prioritySpan.Equals("medium".AsSpan(), StringComparison.OrdinalIgnoreCase) && complexity <= 5))
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

        // OPTIMIZED: Use AsSpan() for zero-allocation contains checks
        var nameSpan = modelName.AsSpan();
        
        // Fast models (GPT-3.5, embeddings)
        if (StringUtilities.ContainsIgnoreCase(nameSpan, "gpt-3.5") || 
            StringUtilities.ContainsIgnoreCase(nameSpan, "gpt-35") || 
            StringUtilities.ContainsIgnoreCase(nameSpan, "embedding") || 
            StringUtilities.ContainsIgnoreCase(nameSpan, "ada"))
        {
            return complexity * 2;
        }
        
        // Medium speed (GPT-4)
        if (StringUtilities.ContainsIgnoreCase(nameSpan, "gpt-4"))
        {
            return complexity * 5;
        }
        
        // Slower models (image generation, audio)
        if (StringUtilities.ContainsIgnoreCase(nameSpan, "dall-e") || 
            StringUtilities.ContainsIgnoreCase(nameSpan, "stable-diffusion") || 
            StringUtilities.ContainsIgnoreCase(nameSpan, "whisper") || 
            StringUtilities.ContainsIgnoreCase(nameSpan, "tts"))
        {
            return complexity * 10;
        }
        
        // Default estimate
        return complexity * 5;
    }
}
