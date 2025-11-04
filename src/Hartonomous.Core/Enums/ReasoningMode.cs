using System.Text.Json.Serialization;

namespace Hartonomous.Core.Enums;

/// <summary>
/// Defines the reasoning approach for inference execution.
/// Used to control how models process prompts and generate outputs.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReasoningMode>))]
public enum ReasoningMode
{
    /// <summary>
    /// Direct inference without intermediate reasoning steps.
    /// Fast, single-pass generation.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Analytical reasoning with structured problem decomposition.
    /// Step-by-step logical analysis.
    /// JSON value: "analytical"
    /// </summary>
    Analytical = 1,

    /// <summary>
    /// Creative reasoning for open-ended generation.
    /// Exploration of multiple solution paths.
    /// JSON value: "creative"
    /// </summary>
    Creative = 2,

    /// <summary>
    /// Chain-of-thought reasoning with explicit intermediate steps.
    /// Generates reasoning trace before final output.
    /// JSON value: "chain-of-thought"
    /// </summary>
    ChainOfThought = 3,

    /// <summary>
    /// Tree-of-thought reasoning exploring multiple branches.
    /// Evaluates alternative reasoning paths.
    /// JSON value: "tree-of-thought"
    /// </summary>
    TreeOfThought = 4,

    /// <summary>
    /// Self-consistency reasoning with multiple samples and voting.
    /// Generate N outputs, select most consistent.
    /// JSON value: "self-consistency"
    /// </summary>
    SelfConsistency = 5,

    /// <summary>
    /// Reflexion-based reasoning with self-critique and refinement.
    /// Iterative improvement through feedback loops.
    /// JSON value: "reflexion"
    /// </summary>
    Reflexion = 6
}
