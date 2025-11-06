using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a reusable tensor atom (kernel, basis vector, attention head slice) derived from a larger tensor.
/// Tensor atoms enable decomposition and reuse of neural network components across models and layers.
/// </summary>
public class TensorAtom
{
    /// <summary>
    /// Gets or sets the unique identifier for the tensor atom.
    /// </summary>
    public long TensorAtomId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the base atom that this tensor atom is associated with.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the model from which this tensor atom was extracted.
    /// </summary>
    public int? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the layer from which this tensor atom was extracted.
    /// </summary>
    public long? LayerId { get; set; }

    /// <summary>
    /// Gets or sets the type of tensor atom (e.g., 'kernel', 'bias', 'attention_head', 'basis_vector').
    /// </summary>
    public required string AtomType { get; set; }

    /// <summary>
    /// Gets or sets the spatial signature point representing the tensor atom's position in high-dimensional space.
    /// </summary>
    public Point? SpatialSignature { get; set; }

    /// <summary>
    /// Gets or sets a geometric footprint representing the tensor atom's influence or receptive field.
    /// </summary>
    public Geometry? GeometryFootprint { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., shape, dimensions, activation patterns).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the importance or salience score of the tensor atom (0.0 to 1.0).
    /// Higher scores indicate more significant contributions to model behavior.
    /// </summary>
    public float? ImportanceScore { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the tensor atom was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation property to the base atom.
    /// </summary>
    public Atom Atom { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the model.
    /// </summary>
    public Model? Model { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the model layer.
    /// </summary>
    public ModelLayer? Layer { get; set; }

    /// <summary>
    /// Gets or sets the collection of coefficients that define this tensor atom's values.
    /// </summary>
    public ICollection<TensorAtomCoefficient> Coefficients { get; set; } = new List<TensorAtomCoefficient>();
}
