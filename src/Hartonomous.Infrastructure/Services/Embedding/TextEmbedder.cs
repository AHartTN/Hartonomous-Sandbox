using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Embedding;

/// <summary>
/// Generates embeddings for text using TF-IDF from database vocabulary.
/// </summary>
public sealed class TextEmbedder : ModalityEmbedderBase<string>
{
    private readonly ITokenVocabularyRepository _tokenVocabularyRepository;
    private readonly ILogger<TextEmbedder> _logger;

    public override string ModalityType => "text";

    public TextEmbedder(
        ITokenVocabularyRepository tokenVocabularyRepository,
        ILogger<TextEmbedder> logger)
    {
        _tokenVocabularyRepository = tokenVocabularyRepository ?? throw new ArgumentNullException(nameof(tokenVocabularyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override void ValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Text cannot be empty.", nameof(input));
        }
    }

    protected override async Task ExtractFeaturesAsync(
        string text,
        Memory<float> embedding,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating text embedding for: {TextPreview}",
            text.Length > 50 ? text[..50] + "..." : text);

        // Zero-allocation tokenization
        var tokens = TokenizeTextOptimized(text);

        // Get vocabulary information
        var vocabularyTokens = await _tokenVocabularyRepository.GetTokensByTextAsync(
            tokens.Distinct(),
            cancellationToken);

        // Count term frequencies
        var termFrequencies = new Dictionary<string, int>();
        foreach (var token in tokens)
        {
            termFrequencies[token] = termFrequencies.GetValueOrDefault(token, 0) + 1;
        }

        int termsFound = 0;
        foreach (var kvp in vocabularyTokens)
        {
            var tokenText = kvp.Key;
            var (tokenId, _) = kvp.Value;

            if (termFrequencies.TryGetValue(tokenText, out var frequency))
            {
                var dimension = tokenId % EmbeddingDimension;
                embedding.Span[dimension] += frequency * 1.0f;
                termsFound++;
            }
        }

        if (termsFound == 0)
        {
            _logger.LogWarning("No vocabulary terms found for text. Using random initialization.");
            InitializeRandomEmbedding(embedding.Span, (uint)text.GetHashCode());
        }

        _logger.LogInformation("Text embedding generated: {TermsFound} vocabulary terms mapped.", termsFound);
    }

    private static List<string> TokenizeTextOptimized(string text)
    {
        return text
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '-', '_' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToList();
    }

    private static void InitializeRandomEmbedding(Span<float> embedding, uint seed)
    {
        var random = new Random((int)seed);
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }
    }
}
