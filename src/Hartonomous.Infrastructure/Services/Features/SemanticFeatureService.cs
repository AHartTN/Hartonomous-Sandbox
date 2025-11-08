using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Features;

/// <summary>
/// Implements semantic feature extraction using SQL Server stored procedures and text analysis.
/// Provides topic classification, sentiment analysis, and entity/keyword extraction.
/// </summary>
public sealed class SemanticFeatureService : ISemanticFeatureService
{
    private readonly ISqlCommandExecutor _sql;
    private readonly ILogger<SemanticFeatureService> _logger;

    private static readonly Regex WordRegex = new("[A-Za-z0-9']+", RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(
        new[]
        {
            "the", "and", "or", "to", "a", "of", "in", "for", "on", "with", "is",
            "are", "was", "were", "be", "by", "as", "at", "it", "an", "this", "that",
            "from", "but", "not", "have", "has", "had", "will", "would", "can", "could", "should",
            "we", "you", "they", "their", "its", "our", "your", "i"
        },
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new semantic feature service.
    /// </summary>
    /// <param name="sqlCommandExecutor">SQL command executor abstraction.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public SemanticFeatureService(
        ISqlCommandExecutor sqlCommandExecutor,
        ILogger<SemanticFeatureService> logger)
    {
        _sql = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<SemanticFeatures> ComputeSemanticFeaturesAsync(
        IReadOnlyList<long> atomEmbeddingIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Computing semantic features for {Count} embeddings",
            atomEmbeddingIds.Count);

        if (atomEmbeddingIds.Count == 0)
        {
            return new SemanticFeatures();
        }

        // Step 1: Invoke sp_ComputeSemanticFeatures for each embedding
        foreach (var atomEmbeddingId in atomEmbeddingIds)
        {
            await _sql.ExecuteAsync(async (command, token) =>
            {
                command.CommandText = "dbo.sp_ComputeSemanticFeatures";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@AtomEmbeddingId", atomEmbeddingId));

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        // Step 2: Retrieve computed features from SemanticFeatures table
        var featureRows = await _sql.ExecuteAsync(async (command, token) =>
        {
            var parameterNames = new string[atomEmbeddingIds.Count];
            for (var i = 0; i < atomEmbeddingIds.Count; i++)
            {
                var parameterName = $"@id{i}";
                parameterNames[i] = parameterName;
                command.Parameters.Add(new SqlParameter(parameterName, atomEmbeddingIds[i]));
            }

            command.CommandText = $@"
SELECT
    sf.AtomEmbeddingId,
    sf.TopicTechnical,
    sf.TopicBusiness,
    sf.TopicScientific,
    sf.TopicCreative,
    sf.SentimentScore,
    sf.FormalityScore,
    sf.ComplexityScore,
    sf.TemporalRelevance,
    sf.TextLength,
    sf.WordCount,
    sf.AvgWordLength,
    sf.UniqueWordRatio,
    a.CanonicalText
FROM dbo.SemanticFeatures AS sf
INNER JOIN dbo.AtomEmbeddings AS ae ON ae.AtomEmbeddingId = sf.AtomEmbeddingId
INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
WHERE sf.AtomEmbeddingId IN ({string.Join(",", parameterNames)});";
            command.CommandType = CommandType.Text;

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            var rows = await reader.ToListAsync(MapFeatureRow, token).ConfigureAwait(false);

            return rows;
        }, cancellationToken).ConfigureAwait(false);

        if (featureRows.Count == 0)
        {
            _logger.LogWarning("Semantic feature rows were not returned for embeddings {Ids}", string.Join(',', atomEmbeddingIds));
            return new SemanticFeatures();
        }

        var aggregate = AggregateSemanticFeatures(featureRows);

        _logger.LogInformation("Semantic features computed for {Count} embeddings", featureRows.Count);
        return aggregate;
    }

    /// <summary>
    /// Maps SqlDataReader row to SemanticFeatureRow using extension methods.
    /// </summary>
    private static SemanticFeatureRow MapFeatureRow(SqlDataReader reader)
    {
        // Cache ordinals for performance
        var idOrd = reader.GetOrdinal("AtomEmbeddingId");
        var techOrd = reader.GetOrdinal("TopicTechnical");
        var busOrd = reader.GetOrdinal("TopicBusiness");
        var sciOrd = reader.GetOrdinal("TopicScientific");
        var creOrd = reader.GetOrdinal("TopicCreative");
        var sentOrd = reader.GetOrdinal("SentimentScore");
        var formOrd = reader.GetOrdinal("FormalityScore");
        var compOrd = reader.GetOrdinal("ComplexityScore");
        var tempOrd = reader.GetOrdinal("TemporalRelevance");
        var lenOrd = reader.GetOrdinal("TextLength");
        var wordOrd = reader.GetOrdinal("WordCount");
        var avgOrd = reader.GetOrdinal("AvgWordLength");
        var uniqOrd = reader.GetOrdinal("UniqueWordRatio");
        var textOrd = reader.GetOrdinal("CanonicalText");

        return new SemanticFeatureRow(
            reader.GetInt64(idOrd),
            reader.GetDoubleOrNull(techOrd) ?? 0.0,
            reader.GetDoubleOrNull(busOrd) ?? 0.0,
            reader.GetDoubleOrNull(sciOrd) ?? 0.0,
            reader.GetDoubleOrNull(creOrd) ?? 0.0,
            reader.GetDoubleOrNull(sentOrd) ?? 0.0,
            reader.GetDoubleOrNull(formOrd) ?? 0.0,
            reader.GetDoubleOrNull(compOrd) ?? 0.0,
            reader.GetDoubleOrNull(tempOrd) ?? 0.0,
            reader.GetInt32OrNull(lenOrd) ?? 0,
            reader.GetInt32OrNull(wordOrd) ?? 0,
            reader.GetDoubleOrNull(avgOrd) ?? 0.0,
            reader.GetDoubleOrNull(uniqOrd) ?? 0.0,
            reader.GetStringOrNull(textOrd) ?? string.Empty);
    }

    /// <summary>
    /// Aggregates individual feature rows into a unified SemanticFeatures result.
    /// Computes averages, extracts entities/keywords, and ranks topics.
    /// </summary>
    private static SemanticFeatures AggregateSemanticFeatures(IReadOnlyList<SemanticFeatureRow> rows)
    {
        var count = rows.Count;

        var topicScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["technical"] = rows.Sum(row => row.TopicTechnical) / count,
            ["business"] = rows.Sum(row => row.TopicBusiness) / count,
            ["scientific"] = rows.Sum(row => row.TopicScientific) / count,
            ["creative"] = rows.Sum(row => row.TopicCreative) / count
        };

        var sortedTopics = topicScores
            .OrderByDescending(pair => pair.Value)
            .Where(pair => pair.Value > 0.15)
            .Select(pair => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pair.Key))
            .ToList();

        if (sortedTopics.Count == 0)
        {
            sortedTopics.Add(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                topicScores.OrderByDescending(pair => pair.Value).First().Key));
        }

        var sentiment = rows.Average(row => row.SentimentScore);
        var temporalRelevance = rows.Average(row => row.TemporalRelevance);

        var keywordsFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entityFrequency = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.CanonicalText))
            {
                continue;
            }

            foreach (Match match in WordRegex.Matches(row.CanonicalText))
            {
                var token = match.Value;
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                var lower = token.ToLowerInvariant();
                if (StopWords.Contains(lower))
                {
                    continue;
                }

                keywordsFrequency[lower] = keywordsFrequency.TryGetValue(lower, out var countValue)
                    ? countValue + 1
                    : 1;

                if (char.IsUpper(token[0]) && token.Any(char.IsLetter))
                {
                    entityFrequency[token] = entityFrequency.TryGetValue(token, out var entityCount)
                        ? entityCount + 1
                        : 1;
                }
            }
        }

        var keywords = keywordsFrequency
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(pair => pair.Key)
            .ToArray();

        var entities = entityFrequency.Count > 0
            ? entityFrequency
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(8)
                .Select(pair => pair.Key)
                .ToArray()
            : keywords
                .Take(5)
                .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word))
                .ToArray();

        var featureScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["TopicTechnical"] = (float)topicScores["technical"],
            ["TopicBusiness"] = (float)topicScores["business"],
            ["TopicScientific"] = (float)topicScores["scientific"],
            ["TopicCreative"] = (float)topicScores["creative"],
            ["FormalityScore"] = (float)(rows.Sum(row => row.FormalityScore) / count),
            ["ComplexityScore"] = (float)(rows.Sum(row => row.ComplexityScore) / count),
            ["TemporalRelevance"] = (float)temporalRelevance,
            ["AverageWordLength"] = (float)(rows.Sum(row => row.AverageWordLength) / count),
            ["UniqueWordRatio"] = (float)(rows.Sum(row => row.UniqueWordRatio) / count)
        };

        return new SemanticFeatures
        {
            Topics = sortedTopics,
            SentimentScore = (float)sentiment,
            Entities = entities,
            Keywords = keywords,
            TemporalRelevance = (float)temporalRelevance,
            FeatureScores = featureScores
        };
    }

    /// <summary>
    /// Internal record for deserializing semantic feature rows from database.
    /// </summary>
    private sealed record SemanticFeatureRow(
        long AtomEmbeddingId,
        double TopicTechnical,
        double TopicBusiness,
        double TopicScientific,
        double TopicCreative,
        double SentimentScore,
        double FormalityScore,
        double ComplexityScore,
        double TemporalRelevance,
        int TextLength,
        int WordCount,
        double AverageWordLength,
        double UniqueWordRatio,
        string CanonicalText);
}
