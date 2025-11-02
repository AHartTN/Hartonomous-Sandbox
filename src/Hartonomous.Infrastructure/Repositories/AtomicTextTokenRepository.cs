using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Repositories;

public class AtomicTextTokenRepository : IAtomicTextTokenRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<AtomicTextTokenRepository> _logger;

    public AtomicTextTokenRepository(HartonomousDbContext context, ILogger<AtomicTextTokenRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AtomicTextToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.AtomicTextTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<AtomicTextToken?> GetByIdAsync(long tokenId, CancellationToken cancellationToken = default)
    {
        return await _context.AtomicTextTokens.FindAsync(new object[] { tokenId }, cancellationToken);
    }

    public async Task<AtomicTextToken> AddAsync(AtomicTextToken token, CancellationToken cancellationToken = default)
    {
        _context.AtomicTextTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateReferenceCountAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        var token = await GetByHashAsync(tokenHash, cancellationToken);
        if (token != null)
        {
            token.ReferenceCount++;
            token.LastReferenced = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<long> GetReferenceCountAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        var token = await GetByHashAsync(tokenHash, cancellationToken);
        return token?.ReferenceCount ?? 0;
    }
}