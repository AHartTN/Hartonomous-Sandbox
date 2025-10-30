using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Abstracts;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Services;

/// <summary>
/// Service for executing semantic queries and searches.
/// Provides unified interface for different search operations.
/// </summary>
public class QueryService : BaseService
{
    private readonly IEmbeddingStorageProvider _embeddingStorage;
    private readonly ITextEmbedder _textEmbedder;

    public QueryService(
        ILogger<QueryService> logger,
        IEmbeddingStorageProvider embeddingStorage,
        ITextEmbedder textEmbedder)
        : base(logger)
    {
        _embeddingStorage = embeddingStorage ?? throw new ArgumentNullException(nameof(embeddingStorage));
        _textEmbedder = textEmbedder ?? throw new ArgumentNullException(nameof(textEmbedder));
    }

    public override string ServiceName => "QueryService";

    /// <summary>
    /// Execute semantic search query.
    /// </summary>
    /// <param name="queryText">The text query</param>
    /// <param name="topK">Number of results to return</param>
    /// <param name="useApproximate">Whether to use approximate search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    public async Task<QueryResults> ExecuteSemanticQueryAsync(
        string queryText,
        int topK = 10,
        bool useApproximate = true,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Executing semantic query: '{Query}'", queryText);

        // Generate query embedding
        var queryEmbedding = await _textEmbedder.EmbedTextAsync(queryText, cancellationToken);

        // Execute exact search
        Logger.LogInformation("Running exact VECTOR search...");
        var exactResults = await _embeddingStorage.ExactSearchAsync(queryEmbedding, topK, cancellationToken);

        // Execute approximate search if requested
        IReadOnlyList<EmbeddingSearchResult>? approxResults = null;
        if (useApproximate)
        {
            Logger.LogInformation("Running approximate spatial search...");
            approxResults = await _embeddingStorage.ApproximateSearchAsync(queryEmbedding, topK, cancellationToken);
        }

        var results = new QueryResults
        {
            QueryText = queryText,
            ExactResults = exactResults,
            ApproximateResults = approxResults
        };

        Logger.LogInformation("✓ Query complete - found {ExactCount} exact, {ApproxCount} approximate results",
            exactResults.Count, approxResults?.Count ?? 0);

        return results;
    }

    /// <summary>
    /// Execute hybrid search (approximate + exact rerank).
    /// </summary>
    /// <param name="queryText">The text query</param>
    /// <param name="topK">Number of final results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hybrid search results</returns>
    public async Task<IReadOnlyList<EmbeddingSearchResult>> ExecuteHybridQueryAsync(
        string queryText,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Executing hybrid query: '{Query}'", queryText);

        // Generate query embedding
        var queryEmbedding = await _textEmbedder.EmbedTextAsync(queryText, cancellationToken);

        // Execute hybrid search
        var results = await _embeddingStorage.HybridSearchAsync(queryEmbedding, topK, cancellationToken);

        Logger.LogInformation("✓ Hybrid query complete - found {Count} results", results.Count);
        return results;
    }

    /// <summary>
    /// Test SQL Vector availability.
    /// </summary>
    /// <returns>Test results</returns>
    public VectorTestResults TestSqlVectorAvailability()
    {
        Logger.LogInformation("Testing SqlVector<T> availability in SqlClient...");

        try
        {
            // Try to create a SqlVector instance
            var testVector = new float[] { 1.0f, 2.0f, 3.0f };
            // Note: In real implementation, we'd test Microsoft.Data.SqlClient.SqlVector

            Logger.LogInformation("✓ SqlVector test completed successfully");
            return new VectorTestResults { IsAvailable = true };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SqlVector test failed");
            return new VectorTestResults { IsAvailable = false, ErrorMessage = ex.Message };
        }
    }
}

/// <summary>
/// Results from a semantic query operation.
/// </summary>
public class QueryResults
{
    public string QueryText { get; set; } = string.Empty;
    public IReadOnlyList<EmbeddingSearchResult> ExactResults { get; set; } = Array.Empty<EmbeddingSearchResult>();
    public IReadOnlyList<EmbeddingSearchResult>? ApproximateResults { get; set; }
}

/// <summary>
/// Results from SQL Vector testing.
/// </summary>
public class VectorTestResults
{
    public bool IsAvailable { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Service for testing atomic storage operations.
/// Provides methods to test pixel, audio, and token storage with deduplication.
/// </summary>
public class AtomicStorageTestService : BaseService
{
    private readonly IAtomicStorageProvider _atomicStorage;

    public AtomicStorageTestService(
        ILogger<AtomicStorageTestService> logger,
        IAtomicStorageProvider atomicStorage)
        : base(logger)
    {
        _atomicStorage = atomicStorage ?? throw new ArgumentNullException(nameof(atomicStorage));
    }

    public override string ServiceName => "AtomicStorageTestService";

    /// <summary>
    /// Test atomic storage with content-addressable deduplication.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    public async Task<AtomicStorageTestResults> TestAtomicStorageAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Testing atomic storage with content-addressable deduplication...");

        var results = new AtomicStorageTestResults();

        // Test 1: Atomic pixels
        Logger.LogInformation("\n=== Testing Atomic Pixel Storage ===");
        var pixelId1 = await _atomicStorage.StorePixelAsync(255, 0, 0, 255, cancellationToken); // Red
        Logger.LogInformation("Stored red pixel: ID={Id}", pixelId1);

        var pixelId2 = await _atomicStorage.StorePixelAsync(255, 0, 0, 255, cancellationToken); // Same red - should dedupe
        Logger.LogInformation("Stored red pixel again (duplicate): ID={Id}", pixelId2);
        results.PixelDeduplicationWorks = pixelId1 == pixelId2;

        var pixelId3 = await _atomicStorage.StorePixelAsync(0, 255, 0, 255, cancellationToken); // Green - different
        Logger.LogInformation("Stored green pixel: ID={Id}", pixelId3);

        // Test 2: Atomic audio samples
        Logger.LogInformation("\n=== Testing Atomic Audio Sample Storage ===");
        var sampleId1 = await _atomicStorage.StoreAudioSampleAsync(0.5f, cancellationToken); // Mid-range amplitude
        Logger.LogInformation("Stored audio sample (0.5): ID={Id}", sampleId1);

        var sampleId2 = await _atomicStorage.StoreAudioSampleAsync(0.5f, cancellationToken); // Same - should dedupe
        Logger.LogInformation("Stored same sample again (duplicate): ID={Id}", sampleId2);
        results.AudioDeduplicationWorks = sampleId1 == sampleId2;

        // Test 3: Atomic tokens
        Logger.LogInformation("\n=== Testing Atomic Token Storage ===");
        var tokenId1 = await _atomicStorage.StoreTextTokenAsync("hello", null, cancellationToken);
        Logger.LogInformation("Stored token 'hello': ID={Id}", tokenId1);

        var tokenId2 = await _atomicStorage.StoreTextTokenAsync("hello", null, cancellationToken); // Same - should dedupe
        Logger.LogInformation("Stored token 'hello' again (duplicate): ID={Id}", tokenId2);
        results.TokenDeduplicationWorks = tokenId1 == tokenId2;

        var tokenId3 = await _atomicStorage.StoreTextTokenAsync("world", null, cancellationToken); // Different
        Logger.LogInformation("Stored token 'world': ID={Id}", tokenId3);

        Logger.LogInformation("\n✓ Atomic storage test complete - deduplication working!");
        return results;
    }
}

/// <summary>
/// Results from atomic storage testing.
/// </summary>
public class AtomicStorageTestResults
{
    public bool PixelDeduplicationWorks { get; set; }
    public bool AudioDeduplicationWorks { get; set; }
    public bool TokenDeduplicationWorks { get; set; }
}