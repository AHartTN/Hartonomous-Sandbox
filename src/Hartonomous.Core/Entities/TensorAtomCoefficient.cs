namespace Hartonomous.Core.Entities;

/// <summary>
/// Associates a tensor atom with a parent tensor signature (layer, tensor role) via a coefficient.
/// Enables decomposition of large tensors into reusable tensor atoms with weighted contributions.
/// </summary>
public class TensorAtomCoefficient
{
    /// <summary>
    /// Gets or sets the unique identifier for the tensor atom coefficient.
    /// </summary>
    public long TensorAtomCoefficientId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the tensor atom.
    /// </summary>
    public long TensorAtomId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent layer containing this coefficient.
    /// </summary>
    public long ParentLayerId { get; set; }

    /// <summary>
    /// Gets or sets the role of the tensor within the layer (e.g., 'weight', 'bias', 'query_projection').
    /// </summary>
    public string? TensorRole { get; set; }

    /// <summary>
    /// Gets or sets the coefficient value representing the contribution of this tensor atom.
    /// </summary>
    public float Coefficient { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the tensor atom.
    /// </summary>
    public TensorAtom TensorAtom { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the parent layer.
    /// </summary>
    public ModelLayer ParentLayer { get; set; } = null!;
}
