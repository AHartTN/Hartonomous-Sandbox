using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomRepository"/>.
/// </summary>
public class AtomRepository : IAtomRepository
{
    private readonly HartonomousDbContext _context;

    public AtomRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken = default)
    {
        return await _context.Atoms
            .Include(a => a.Embeddings)
            .Include(a => a.TensorAtoms)
            .FirstOrDefaultAsync(a => a.AtomId == atomId, cancellationToken);
    }

    public async Task<Atom?> GetByContentHashAsync(byte[] contentHash, CancellationToken cancellationToken = default)
    {
        return await _context.Atoms
            .Include(a => a.Embeddings)
            .FirstOrDefaultAsync(a => a.ContentHash == contentHash, cancellationToken);
    }

    public async Task<IReadOnlyList<Atom>> GetByModalityAsync(string modality, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Atoms
            .Where(a => a.Modality == modality)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Atom> AddAsync(Atom atom, CancellationToken cancellationToken = default)
    {
        _context.Atoms.Add(atom);
        await _context.SaveChangesAsync(cancellationToken);
        return atom;
    }

    public async Task UpdateMetadataAsync(long atomId, string? metadata, CancellationToken cancellationToken = default)
    {
        var atom = await _context.Atoms.FirstOrDefaultAsync(a => a.AtomId == atomId, cancellationToken);
        if (atom is null)
        {
            return;
        }

        atom.Metadata = metadata;
        atom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSpatialKeyAsync(long atomId, Point spatialKey, CancellationToken cancellationToken = default)
    {
        var atom = await _context.Atoms.FirstOrDefaultAsync(a => a.AtomId == atomId, cancellationToken);
        if (atom is null)
        {
            return;
        }

        atom.SpatialKey = spatialKey;
        atom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementReferenceCountAsync(long atomId, long delta = 1, CancellationToken cancellationToken = default)
    {
        var atom = await _context.Atoms.FirstOrDefaultAsync(a => a.AtomId == atomId, cancellationToken);
        if (atom is null)
        {
            return;
        }

        atom.ReferenceCount += delta;
        atom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
