using Hartonomous.Data.Entities.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Abstraction for maintaining the SQL graph surface area in sync with the relational atom store.
/// </summary>
public interface IAtomGraphWriter
{
    /// <summary>
    /// Inserts or updates the graph node representing the supplied atom, including semantic metadata.
    /// </summary>
    Task UpsertAtomNodeAsync(Atom atom, AtomEmbedding? primaryEmbedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the graph edge that mirrors the supplied atom relation.
    /// </summary>
    Task UpsertRelationAsync(AtomRelation relation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the lightweight synchronization routine that backfills the node and edge tables from relational state.
    /// </summary>
    Task SynchronizeAsync(CancellationToken cancellationToken = default);
}
