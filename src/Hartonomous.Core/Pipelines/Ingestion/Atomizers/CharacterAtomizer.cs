using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers;

/// <summary>
/// TRUE ATOMIC TEXT ATOMIZER
/// 
/// Decomposes text into individual characters or BPE tokens.
/// Each character/token becomes a deduplicated atom with position as GEOMETRY.
/// 
/// Philosophy: Store each unique character ONCE globally, reference by position.
/// "The quick brown fox" = ~16 unique chars × ~18 positions = massive deduplication across all documents.
/// </summary>
public sealed class CharacterAtomizer : IAtomizer<string>
{
    private readonly ILogger<CharacterAtomizer>? _logger;
    private readonly CharacterAtomizationStrategy _strategy;

    public CharacterAtomizer(
        CharacterAtomizationStrategy strategy = CharacterAtomizationStrategy.Utf8Character,
        ILogger<CharacterAtomizer>? logger = null)
    {
        _strategy = strategy;
        _logger = logger;
    }

    public string Modality => "text";

    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        string source,
        AtomizationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(source))
        {
            _logger?.LogWarning("Empty text source for atomic character decomposition");
            yield break;
        }

        _logger?.LogDebug(
            "Atomizing {Length} chars into individual character atoms using {Strategy}",
            source.Length, _strategy);

        for (int i = 0; i < source.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var ch = source[i];
            var charBytes = Encoding.UTF8.GetBytes(new[] { ch });
            var charHash = SHA256.HashData(charBytes);

            // Spatial key: POINT(charPosition, lineNumber, paragraphNumber, sectionId)
            // For now, just use character index as X coordinate
            var spatialWkt = $"POINT({i} 0 0 0)";

            yield return new AtomCandidate
            {
                Modality = "text",
                Subtype = "utf8-char",
                AtomicValue = charBytes,
                CanonicalText = ch.ToString(),
                SourceUri = context.SourceUri ?? "unknown",
                SourceType = "text-document",
                ContentHash = Convert.ToHexString(charHash),
                
                // Position as spatial geometry
                SpatialKey = spatialWkt,
                
                Metadata = new Dictionary<string, object>
                {
                    ["position"] = i,
                    ["codepoint"] = (int)ch,
                    ["category"] = char.GetUnicodeCategory(ch).ToString(),
                    ["isWhitespace"] = char.IsWhiteSpace(ch),
                    ["isControl"] = char.IsControl(ch)
                },
                
                QualityScore = 1.0  // All characters are valid
            };
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Atomization strategy for text
/// </summary>
public enum CharacterAtomizationStrategy
{
    /// <summary>Individual UTF-8 characters</summary>
    Utf8Character,
    
    /// <summary>BPE tokens (more efficient, fewer atoms)</summary>
    BpeToken,
    
    /// <summary>Unicode grapheme clusters (combining chars handled correctly)</summary>
    GraphemeCluster
}
