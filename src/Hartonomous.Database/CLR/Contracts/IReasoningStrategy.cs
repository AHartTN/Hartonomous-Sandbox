using Hartonomous.Clr.Models;

namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Interface for reasoning strategies (Chain of Thought, Tree of Thought, Reflexion, etc).
    /// Enables polymorphic reasoning across different algorithms.
    /// </summary>
    public interface IReasoningStrategy
    {
        /// <summary>
        /// Gets the reasoning strategy name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the reasoning strategy.
        /// </summary>
        /// <param name="prompt">Initial reasoning prompt</param>
        /// <param name="embedding">Embedding vector for spatial reasoning</param>
        /// <param name="maxSteps">Maximum reasoning steps</param>
        /// <param name="temperature">Sampling temperature</param>
        /// <returns>Array of reasoning steps with conclusions</returns>
        ReasoningStep[] Reason(string prompt, float[] embedding, int maxSteps, float temperature);

        /// <summary>
        /// Evaluates confidence/coherence of reasoning chain.
        /// </summary>
        /// <param name="steps">Reasoning steps to evaluate</param>
        /// <returns>Confidence score [0, 1]</returns>
        float EvaluateCoherence(ReasoningStep[] steps);
    }
}
