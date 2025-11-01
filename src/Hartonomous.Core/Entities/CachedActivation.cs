using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a cached layer activation to speed up inference by avoiding redundant computation.
/// </summary>
public class CachedActivation
{
    /// <summary>
    /// Gets or sets the unique identifier for the cached activation.
    /// </summary>
    public long CacheId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the model.
    /// </summary>
    public int ModelId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the layer.
    /// </summary>
    public long LayerId { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the input data.
    /// </summary>
    public required byte[] InputHash { get; set; }

    /// <summary>
    /// Gets or sets the cached activation output as a VECTOR.
    /// For large activations exceeding 1998 dimensions, chunk into multiple entries.
    /// </summary>
    public SqlVector<float>? ActivationOutput { get; set; }

    /// <summary>
    /// Gets or sets the shape of the output tensor as JSON (e.g., "[batch, seq_len, hidden_dim]").
    /// </summary>
    public string? OutputShape { get; set; }

    /// <summary>
    /// Gets or sets the number of times this cached activation has been reused.
    /// </summary>
    public long HitCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the timestamp when this cache entry was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp of the last access to this cache entry.
    /// </summary>
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total computation time saved in milliseconds by reusing this cache entry.
    /// </summary>
    public long ComputeTimeSavedMs { get; set; } = 0;

    /// <summary>
    /// Gets or sets the parent model.
    /// </summary>
    public Model Model { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parent layer.
    /// </summary>
    public ModelLayer Layer { get; set; } = null!;
}
