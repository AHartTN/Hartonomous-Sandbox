using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces.Reasoning;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Reasoning;

/// <summary>
/// Mock implementation of reasoning services for marketing site demonstrations.
/// Returns realistic sample data without requiring database connectivity.
/// Inherits from ReasoningServiceBase for standardized validation and telemetry.
/// </summary>
public sealed class MockReasoningService : ReasoningServiceBase<MockReasoningService>
{
    private static readonly Random _random = new();

    public MockReasoningService(ILogger<MockReasoningService> logger)
        : base(logger)
    {
    }

    protected override async Task<ReasoningResult> ExecuteChainOfThoughtInternalAsync(
        long sessionId,
        string prompt,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("[DEMO] Executing Chain-of-Thought reasoning for session {SessionId}", sessionId);
        
        await Task.Delay(_random.Next(100, 500), cancellationToken);
        
        return new ReasoningResult
        {
            Id = _random.NextInt64(1, 10000),
            SessionId = sessionId,
            Strategy = "ChainOfThought",
            Prompt = prompt,
            Conclusion = "Based on the analysis, the optimal solution involves...",
            IntermediateSteps = GenerateMockChainOfThought(),
            ConfidenceScore = 0.91,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = _random.Next(200, 800)
        };
    }

    protected override async Task<ReasoningResult> ExecuteTreeOfThoughtInternalAsync(
        long sessionId,
        string prompt,
        int maxBranches,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("[DEMO] Executing Tree-of-Thought reasoning for session {SessionId} with {MaxBranches} branches", 
            sessionId, maxBranches);
        
        await Task.Delay(_random.Next(150, 600), cancellationToken);
        
        return new ReasoningResult
        {
            Id = _random.NextInt64(1, 10000),
            SessionId = sessionId,
            Strategy = "TreeOfThought",
            Prompt = prompt,
            Conclusion = "After exploring multiple reasoning paths, branch 2 provides the most coherent solution...",
            IntermediateSteps = GenerateMockTreeOfThought(),
            ConfidenceScore = 0.92,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = _random.Next(300, 1000)
        };
    }

    protected override Task<IEnumerable<ReasoningResult>> GetSessionHistoryInternalAsync(
        long sessionId,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("[DEMO] Retrieving reasoning history for session {SessionId}", sessionId);
        
        var results = new List<ReasoningResult>
        {
            new()
            {
                Id = 1,
                SessionId = sessionId,
                Strategy = "ChainOfThought",
                Prompt = "Sample historical prompt 1",
                Conclusion = "Historical conclusion 1",
                ConfidenceScore = 0.89,
                ExecutedAt = DateTime.UtcNow.AddMinutes(-10),
                ExecutionTimeMs = 450
            },
            new()
            {
                Id = 2,
                SessionId = sessionId,
                Strategy = "TreeOfThought",
                Prompt = "Sample historical prompt 2",
                Conclusion = "Historical conclusion 2",
                ConfidenceScore = 0.93,
                ExecutedAt = DateTime.UtcNow.AddMinutes(-5),
                ExecutionTimeMs = 620
            }
        };
        
        return Task.FromResult<IEnumerable<ReasoningResult>>(results);
    }

    private static string GenerateMockChainOfThought()
    {
        return $$"""
            {
              "operation": "ChainOfThought",
              "reasoning_chain": [
                {
                  "step": 1,
                  "thought": "Analyzing input context and identifying key semantic components",
                  "confidence": 0.92
                },
                {
                  "step": 2,
                  "thought": "Retrieving relevant knowledge atoms from spatial index",
                  "confidence": 0.87,
                  "atoms_retrieved": 147
                },
                {
                  "step": 3,
                  "thought": "Synthesizing logical inference path through provenance graph",
                  "confidence": 0.89,
                  "graph_depth": 5
                },
                {
                  "step": 4,
                  "thought": "Validating conclusion against error clustering patterns",
                  "confidence": 0.94,
                  "error_clusters_checked": 23
                }
              ],
              "conclusion": "Based on atomized knowledge synthesis, the inference converges on a high-confidence result.",
              "confidence_score": 0.91,
              "processing_time_ms": {{_random.Next(150, 450)}},
              "atoms_processed": {{_random.Next(100, 300)}},
              "demo_mode": true
            }
            """;
    }

    private static string GenerateMockTreeOfThought()
    {
        return $$"""
            {
              "operation": "TreeOfThought",
              "thought_tree": {
                "root": {
                  "thought": "Initial hypothesis space exploration",
                  "branches": 4,
                  "confidence": 0.78
                },
                "branches": [
                  {
                    "id": "branch_1",
                    "thought": "Path A: Direct semantic mapping approach",
                    "confidence": 0.85,
                    "pruned": false,
                    "depth": 3
                  },
                  {
                    "id": "branch_2",
                    "thought": "Path B: Contextual inference with spatial correlation",
                    "confidence": 0.92,
                    "pruned": false,
                    "depth": 4
                  },
                  {
                    "id": "branch_3",
                    "thought": "Path C: Error-aware probabilistic reasoning",
                    "confidence": 0.73,
                    "pruned": true,
                    "reason": "Low confidence threshold"
                  },
                  {
                    "id": "branch_4",
                    "thought": "Path D: Graph-traversal synthesis",
                    "confidence": 0.88,
                    "pruned": false,
                    "depth": 3
                  }
                ],
                "best_path": "branch_2",
                "convergence_depth": 4
              },
              "conclusion": "Tree exploration converged on optimal reasoning path through spatial-semantic correlation.",
              "confidence_score": 0.92,
              "processing_time_ms": {{_random.Next(200, 600)}},
              "branches_explored": 4,
              "branches_pruned": 1,
              "demo_mode": true
            }
            """;
    }
}
