using System.Collections.Generic;
using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TokenVocabulary operations.
/// Provides access to the token vocabulary for embedding generation.
/// Note: Does not inherit EfRepository - uses specialized projection queries.
/// </summary>
public class TokenVocabularyRepository : ITokenVocabularyRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<TokenVocabularyRepository> _logger;

    public TokenVocabularyRepository(HartonomousDbContext context, ILogger<TokenVocabularyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, (int tokenId, string tokenText)>> GetTokensByTextAsync(
        IEnumerable<string> tokenTexts,
        CancellationToken cancellationToken = default)
    {
        var tokenList = tokenTexts.ToList();
        if (!tokenList.Any())
        {
            return new Dictionary<string, (int, string)>();
        }

        var tokens = await _context.TokenVocabulary
            .Where(tv => tokenList.Contains(tv.Token))
            .Select(tv => new { tv.Token, tv.DimensionIndex })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = tokens.ToDictionary(
            t => t.Token,
            t => (t.DimensionIndex, t.Token));

        _logger.LogDebug("Retrieved {Count} tokens from vocabulary for {RequestedCount} token texts",
            result.Count, tokenList.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TokenVocabulary>> GetAllTokensAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await _context.TokenVocabulary
            .OrderBy(tv => tv.DimensionIndex)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} tokens from vocabulary", tokens.Count);
        return tokens;
    }
}