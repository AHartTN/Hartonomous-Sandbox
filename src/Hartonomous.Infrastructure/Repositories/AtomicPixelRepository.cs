using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomicPixelRepository"/>.
/// Inherits base CRUD from EfRepository, adds hash-based deduplication and reference counting.
/// </summary>
public class AtomicPixelRepository : AtomicReferenceRepository<AtomicPixel, byte[], byte[]>, IAtomicPixelRepository
{
    public AtomicPixelRepository(HartonomousDbContext context, ILogger<AtomicPixelRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomicPixels are identified by PixelHash property.
    /// </summary>
    protected override Expression<Func<AtomicPixel, byte[]>> GetIdExpression() => p => p.PixelHash;

    protected override Expression<Func<AtomicPixel, byte[]>> GetHashExpression() => p => p.PixelHash;

    protected override Expression<Func<AtomicPixel, bool>> BuildKeyPredicate(byte[] key) => pixel => pixel.PixelHash == key;

    // Domain-specific queries stay available via base implementations.
}