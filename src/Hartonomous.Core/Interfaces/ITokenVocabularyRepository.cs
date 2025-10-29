using Hartonomous.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository interface for TokenVocabulary operations.
/// Provides access to the token vocabulary for embedding generation.
/// </summary>
public interface ITokenVocabularyRepository
{
    /// <summary>
    /// Gets token information for the specified token texts.
    /// </summary>
    /// <param name="tokenTexts">The token texts to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping token text to (tokenId, tokenText) pairs.</returns>
    Task<IDictionary<string, (int tokenId, string tokenText)>> GetTokensByTextAsync(
        IEnumerable<string> tokenTexts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tokens from the vocabulary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All tokens in the vocabulary.</returns>
    Task<IEnumerable<TokenVocabulary>> GetAllTokensAsync(CancellationToken cancellationToken = default);
}