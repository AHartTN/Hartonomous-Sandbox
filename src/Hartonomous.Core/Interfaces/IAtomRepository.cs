using Hartonomous.Data.Entities;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for working with atoms and their lifecycle.
/// </summary>
public interface IAtomRepository
{
    Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken = default);
    Task<Atom?> GetByContentHashAsync(byte[] contentHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atom>> GetByModalityAsync(string modality, int take = 100, CancellationToken cancellationToken = default);
    Task<Atom> AddAsync(Atom atom, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(long atomId, string? metadata, CancellationToken cancellationToken = default);
    Task UpdateSpatialKeyAsync(long atomId, Point spatialKey, CancellationToken cancellationToken = default);
    Task IncrementReferenceCountAsync(long atomId, long delta = 1, CancellationToken cancellationToken = default);
}
