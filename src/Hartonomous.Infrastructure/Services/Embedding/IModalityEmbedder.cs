using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Embedding;

/// <summary>
/// Interface for modality-specific embedding generation.
/// Enables polymorphic embedding strategies for different data types.
/// </summary>
public interface IModalityEmbedder<in TInput>
{
    /// <summary>
    /// Generates an embedding vector for the supplied input.
    /// </summary>
    /// <param name="input">Input data to embed (bytes, text, etc.).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Normalized embedding vector (typically 768 dimensions).</returns>
    Task<float[]> EmbedAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the expected embedding dimension for this modality.
    /// </summary>
    int EmbeddingDimension { get; }

    /// <summary>
    /// Gets the modality type identifier (e.g., "text", "image", "audio").
    /// </summary>
    string ModalityType { get; }
}

/// <summary>
/// Base class for modality embedders with common validation and normalization logic.
/// Reduces code duplication across different embedding strategies.
/// </summary>
/// <typeparam name="TInput">The input type for this embedder.</typeparam>
public abstract class ModalityEmbedderBase<TInput> : IModalityEmbedder<TInput>
{
    /// <inheritdoc />
    public int EmbeddingDimension => 768;

    /// <inheritdoc />
    public abstract string ModalityType { get; }

    /// <inheritdoc />
    public async Task<float[]> EmbedAsync(TInput input, CancellationToken cancellationToken = default)
    {
        ValidateInput(input);

        var embedding = new float[EmbeddingDimension];

        await ExtractFeaturesAsync(input, embedding.AsMemory(), cancellationToken);

        NormalizeEmbedding(embedding);

        return embedding;
    }

    /// <summary>
    /// Validates the input before processing.
    /// Throws ArgumentException if input is invalid.
    /// </summary>
    protected abstract void ValidateInput(TInput input);

    /// <summary>
    /// Extracts features into the embedding buffer.
    /// Implemented by specific modality embedders.
    /// </summary>
    protected abstract Task ExtractFeaturesAsync(
        TInput input,
        Memory<float> embedding,
        CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the embedding to unit length using SIMD if available.
    /// </summary>
    protected virtual void NormalizeEmbedding(float[] embedding)
    {
        Hartonomous.Core.Performance.VectorMath.Normalize(embedding.AsSpan());
    }
}
