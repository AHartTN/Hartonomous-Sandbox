using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for tensor atom storage and retrieval.
/// </summary>
public interface ITensorAtomRepository
{
    Task<TensorAtom?> GetByIdAsync(long tensorAtomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TensorAtom>> GetByModelLayerAsync(int modelId, long? layerId, string? atomType, int take = 256, CancellationToken cancellationToken = default);
    Task<TensorAtom> AddAsync(TensorAtom tensorAtom, CancellationToken cancellationToken = default);
    Task AddCoefficientsAsync(long tensorAtomId, IEnumerable<TensorAtomCoefficient> coefficients, CancellationToken cancellationToken = default);
}
