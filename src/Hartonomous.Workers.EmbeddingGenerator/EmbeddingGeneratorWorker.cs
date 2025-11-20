using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Hartonomous.Workers.EmbeddingGenerator;

/// <summary>
/// Background worker that polls for atoms without embeddings
/// and generates embeddings using configured model (ONNX or Azure OpenAI).
/// 
/// OPTIONAL: This worker is for future embedding generation.
/// Currently atoms use existing embeddings from source models.
/// 
/// CRITICAL: Uses IServiceScopeFactory to create scopes for DbContext
/// since BackgroundService is a singleton.
/// </summary>
public class EmbeddingGeneratorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingGeneratorWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;

    public EmbeddingGeneratorWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EmbeddingGeneratorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _batchSize = configuration.GetValue<int>("EmbeddingGenerator:BatchSize", 100);
        _pollInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("EmbeddingGenerator:PollIntervalSeconds", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Embedding Generator Worker starting...");
        _logger.LogInformation("Batch size: {BatchSize}, Poll interval: {PollInterval}", _batchSize, _pollInterval);

        // Delay startup to ensure database is ready
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Embedding Generator Worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Embedding Generator Worker stopping...");
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        // Create scope for scoped services (DbContext)
        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dbContext = serviceProvider.GetRequiredService<HartonomousDbContext>();
        var telemetry = serviceProvider.GetService<TelemetryClient>();

        // Find atoms without embeddings
        var atomsWithoutEmbeddings = await dbContext.Atoms
            .Where(a => !a.AtomEmbeddings.Any())
            .Where(a => a.CanonicalText != null) // Only atoms with text to embed
            .OrderBy(a => a.AtomId)
            .Take(_batchSize)
            .ToListAsync(stoppingToken);

        if (atomsWithoutEmbeddings.Count == 0)
        {
            _logger.LogDebug("No atoms without embeddings found");
            return;
        }

        _logger.LogInformation("Found {Count} atoms without embeddings", atomsWithoutEmbeddings.Count);

        using var operation = telemetry?.StartOperation<RequestTelemetry>("EmbeddingGenerator.ProcessBatch");

        try
        {
            operation?.Telemetry.Properties.Add("AtomCount", atomsWithoutEmbeddings.Count.ToString());

            foreach (var atom in atomsWithoutEmbeddings)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // TODO: Implement actual embedding generation
                    // For now, this is a placeholder that would call:
                    // - Azure OpenAI embeddings API
                    // - Local ONNX embedding model
                    // - Sentence-BERT model
                    
                    _logger.LogInformation(
                        "Generating embedding for atom: {AtomHash}, Text length: {Length}",
                        Convert.ToHexString(atom.ContentHash ?? Array.Empty<byte>()), 
                        atom.CanonicalText?.Length ?? 0);

                    // Placeholder: Generate random embedding (1536 dimensions for OpenAI compatibility)
                    // In production, replace with actual model inference
                    var embedding = GeneratePlaceholderEmbedding();
                    
                    // Create AtomEmbedding record
                    // Note: Requires spatial data - using default point for placeholder
                    var atomEmbedding = new AtomEmbedding
                    {
                        AtomId = atom.AtomId,
                        TenantId = atom.TenantId,
                        ModelId = 1, // TODO: Get from configuration
                        EmbeddingType = "text-embedding-ada-002", // TODO: Make configurable
                        Dimension = embedding.Length,
                        EmbeddingVector = new Microsoft.Data.SqlTypes.SqlVector<float>(embedding),
                        SpatialKey = new Point(0, 0), // Placeholder - should be computed from embedding
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    dbContext.AtomEmbeddings.Add(atomEmbedding);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate embedding for atom {AtomHash}",
                        Convert.ToHexString(atom.ContentHash ?? Array.Empty<byte>()));
                }
            }

            // Save all embeddings in one transaction
            var savedCount = await dbContext.SaveChangesAsync(stoppingToken);

            telemetry?.TrackMetric("EmbeddingGenerator.EmbeddingsGenerated", savedCount);
            telemetry?.TrackEvent("EmbeddingGenerator.BatchCompleted", new Dictionary<string, string>
            {
                ["EmbeddingsGenerated"] = savedCount.ToString(),
                ["BatchSize"] = _batchSize.ToString()
            });

            if (operation != null)
            {
                operation.Telemetry.Success = true;
            }

            _logger.LogInformation("Batch processing completed: {Count} embeddings generated", savedCount);
        }
        catch (Exception ex)
        {
            if (operation != null)
            {
                operation.Telemetry.Success = false;
            }

            _logger.LogError(ex, "Failed to process embedding batch");
            throw;
        }
    }

    /// <summary>
    /// Placeholder embedding generator
    /// TODO: Replace with actual model inference (ONNX, Azure OpenAI, etc.)
    /// </summary>
    private static float[] GeneratePlaceholderEmbedding()
    {
        // Generate 1536-dimensional embedding (OpenAI ada-002 compatible)
        var embedding = new float[1536];
        var random = new Random();
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random values between -1 and 1
        }

        // Normalize the vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(embedding[i] / magnitude);
        }

        return embedding;
    }
}
