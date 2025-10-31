namespace Hartonomous.Core.Entities;

/// <summary>
/// Associates a tensor atom with a parent tensor signature (layer, tensor role) via a coefficient.
/// </summary>
public class TensorAtomCoefficient
{
    public long TensorAtomCoefficientId { get; set; }

    public long TensorAtomId { get; set; }

    public long ParentLayerId { get; set; }

    public string? TensorRole { get; set; }

    public float Coefficient { get; set; }

    public TensorAtom TensorAtom { get; set; } = null!;

    public ModelLayer ParentLayer { get; set; } = null!;
}
