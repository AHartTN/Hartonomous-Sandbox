using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomicTextTokenRepository"/>.
/// Inherits base CRUD from EfRepository, adds hash-based deduplication logic.
/// </summary>
public class AtomicTextTokenRepository : AtomicReferenceRepository<AtomicTextToken, long, byte[]>, IAtomicTextTokenRepository
{
    public AtomicTextTokenRepository(HartonomousDbContext context, ILogger<AtomicTextTokenRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomicTextTokens are identified by TokenId property.
    /// </summary>
    protected override Expression<Func<AtomicTextToken, long>> GetIdExpression() => t => t.TokenId;

    protected override Expression<Func<AtomicTextToken, byte[]>> GetHashExpression() => t => t.TokenHash;

    protected override Expression<Func<AtomicTextToken, bool>> BuildKeyPredicate(long key) => token => token.TokenId == key;

    // Domain-specific queries stay available via base implementations.
}