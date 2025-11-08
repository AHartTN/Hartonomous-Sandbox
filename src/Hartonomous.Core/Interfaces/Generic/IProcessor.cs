namespace Hartonomous.Core.Interfaces.Generic;

public interface IProcessor<TInput, TOutput>
{
    /// <summary>
    /// Process a single input item.
    /// </summary>
    /// <param name="input">The input to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processed output</returns>
    Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process multiple input items in batch.
    /// </summary>
    /// <param name="inputs">The inputs to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processed outputs</returns>
    Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the input can be processed.
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>True if the input is valid for processing</returns>
    bool CanProcess(TInput input);
}

/// <summary>
/// Generic validator interface for validating objects.
/// Provides a consistent pattern for validation logic.
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
