using Hartonomous.Core.Models;

namespace Hartonomous.Core.Services;

/// <summary>
/// Service for determining inference metadata such as reasoning modes, complexity, and SLA requirements.
/// Database-native: queries Model.Metadata.PerformanceMetrics for actual latency data.
/// </summary>
public interface IInferenceMetadataService
{
    /// <summary>
    /// Determines the appropriate reasoning mode based on task complexity and requirements.
    /// </summary>
    /// <param name="taskDescription">Description of the task</param>
    /// <param name="requiresMultiStep">Whether multi-step reasoning is required</param>
    /// <returns>The recommended reasoning mode</returns>
    string DetermineReasoningMode(string taskDescription, bool requiresMultiStep);
    
    /// <summary>
    /// Calculates the complexity score for an inference task.
    /// </summary>
    /// <param name="inputTokenCount">Number of input tokens</param>
    /// <param name="requiresMultiModal">Whether multiple modalities are involved</param>
    /// <param name="requiresToolUse">Whether function/tool calling is needed</param>
    /// <returns>Complexity score from 1 (simple) to 10 (very complex)</returns>
    int CalculateComplexity(int inputTokenCount, bool requiresMultiModal, bool requiresToolUse);
    
    /// <summary>
    /// Determines the appropriate SLA tier based on task priority and complexity.
    /// </summary>
    /// <param name="priority">Task priority (low, medium, high, critical)</param>
    /// <param name="complexity">Task complexity score</param>
    /// <returns>SLA tier (standard, expedited, realtime)</returns>
    string DetermineSla(string priority, int complexity);
    
    /// <summary>
    /// Estimates the expected response time by querying Model.Metadata.PerformanceMetrics from the database.
    /// Falls back to complexity-based estimation if no performance data available.
    /// </summary>
    /// <param name="modelName">The model being used</param>
    /// <param name="complexity">Task complexity score</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Estimated response time in seconds</returns>
    Task<int> EstimateResponseTimeAsync(string modelName, int complexity, CancellationToken cancellationToken = default);
}
