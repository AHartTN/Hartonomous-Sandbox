using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for atom relation graph operations.
/// </summary>
public interface IAtomRelationRepository
{
    Task<AtomRelation?> GetByIdAsync(long relationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtomRelation>> GetRelationsForAtomAsync(long atomId, int take = 256, CancellationToken cancellationToken = default);
    Task<AtomRelation> AddAsync(AtomRelation relation, CancellationToken cancellationToken = default);
}
