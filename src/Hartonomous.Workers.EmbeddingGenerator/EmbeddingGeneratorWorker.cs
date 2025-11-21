using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Data;

namespace Hartonomous.Workers.EmbeddingGenerator;

/// <summary>
/// Background worker that polls for atoms without embeddings
/// and generates embeddings using CLR functions (fn_ComputeEmbedding).
/// 
/// PHASE 2 IMPLEMENTATION: Real embeddings with spatial projection
/// - Calls CLR fn_ComputeEmbedding for real embedding computation
/// - Calls CLR fn_ProjectTo3D for 3D spatial projection
/// - Calls CLR clr_ComputeHilbertValue for cache-friendly indexing
/// - Computes spatial buckets for grid-based retrieval
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
    private readonly string _connectionString;
    private readonly int _defaultModelId;

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
        _connectionString = configuration.GetConnectionString("HartonomousDb") 
            ?? throw new InvalidOperationException("HartonomousDb connection string not configured");
        _defaultModelId = configuration.GetValue<int>("EmbeddingGenerator:DefaultModelId", 1);
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
        var backgroundJobService = serviceProvider.GetRequiredService<IBackgroundJobService>();
        var telemetry = serviceProvider.GetService<TelemetryClient>();

        // ===== FIX 1: Check job queue for pending embedding jobs =====
        var pendingJobs = await backgroundJobService.GetPendingJobsAsync(
            "GenerateEmbedding",
            _batchSize,
            stoppingToken);

        var pendingJobList = pendingJobs.ToList();

        if (pendingJobList.Count == 0)
        {
            _logger.LogDebug("No pending embedding jobs found");
            return;
        }

        _logger.LogInformation("Found {Count} pending embedding jobs", pendingJobList.Count);

        // Extract AtomIds from job parameters
        var atomIds = new List<long>();
        var jobLookup = new Dictionary<long, Guid>(); // AtomId -> JobId
        
        foreach (var (jobId, parametersJson) in pendingJobList)
        {
            try
            {
                var jobParams = System.Text.Json.JsonSerializer.Deserialize<EmbeddingJobParameters>(parametersJson);
                if (jobParams != null)
                {
                    atomIds.Add(jobParams.AtomId);
                    jobLookup[jobParams.AtomId] = jobId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse job parameters for JobId={JobId}", jobId);
            }
        }

        // Load atoms for these jobs
        var atomsWithoutEmbeddings = await dbContext.Atoms
            .Where(a => atomIds.Contains(a.AtomId))
            .ToListAsync(stoppingToken);

        if (atomsWithoutEmbeddings.Count == 0)
        {
            _logger.LogWarning("No atoms found for {Count} jobs", atomIds.Count);
            return;
        }
        // ===== END FIX 1 =====

        _logger.LogInformation("Processing embeddings for {Count} atoms", atomsWithoutEmbeddings.Count);

        using var operation = telemetry?.StartOperation<RequestTelemetry>("EmbeddingGenerator.ProcessBatch");

        try
        {
            operation?.Telemetry.Properties.Add("AtomCount", atomsWithoutEmbeddings.Count.ToString());

            var processedJobIds = new List<Guid>();

            foreach (var atom in atomsWithoutEmbeddings)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                // Find the corresponding job
                Guid? jobId = null;
                if (jobLookup.TryGetValue(atom.AtomId, out var foundJobId))
                {
                    jobId = foundJobId;
                }

                try
                {
                    _logger.LogInformation(
                        "Generating embedding for atom: AtomId={AtomId}, Modality={Modality}, TenantId={TenantId}",
                        atom.AtomId, atom.Modality, atom.TenantId);

                    // STEP 1: Compute embedding using CLR function (calls EmbeddingFunctions.cs)
                    var embeddingBytes = await ComputeEmbeddingAsync(
                        atom.AtomId, 
                        _defaultModelId, 
                        atom.TenantId, 
                        stoppingToken);
                        
                    if (embeddingBytes == null || embeddingBytes.Length == 0)
                    {
                        _logger.LogWarning("Empty embedding returned for AtomId={AtomId}", atom.AtomId);
                        
                        if (jobId.HasValue)
                        {
                            await backgroundJobService.UpdateJobAsync(
                                jobId.Value,
                                "Failed",
                                errorMessage: "Empty embedding returned",
                                cancellationToken: stoppingToken);
                        }
                        continue;
                    }

                    // STEP 2: Project to 3D spatial key using CLR function
                    var spatialKey = await ProjectTo3DAsync(embeddingBytes, stoppingToken);
                    
                    if (spatialKey == null)
                    {
                        _logger.LogWarning("Spatial projection failed for AtomId={AtomId}", atom.AtomId);
                        
                        if (jobId.HasValue)
                        {
                            await backgroundJobService.UpdateJobAsync(
                                jobId.Value,
                                "Failed",
                                errorMessage: "Spatial projection failed",
                                cancellationToken: stoppingToken);
                        }
                        continue;
                    }

                    // STEP 3: Compute Hilbert curve value for cache locality
                    var hilbertValue = await ComputeHilbertValueAsync(spatialKey, 21, stoppingToken);

                    // STEP 4: Compute spatial buckets for grid-based indexing
                    var (bucketX, bucketY, bucketZ) = ComputeSpatialBuckets(spatialKey);

                    // STEP 5: Convert embedding bytes to SqlVector<float>
                    var embedding = BytesToFloatArray(embeddingBytes);
                    var dimension = embedding.Length;
                    
                    // STEP 6: Create AtomEmbedding record with ALL spatial indices populated
                    var atomEmbedding = new AtomEmbedding
                    {
                        AtomId = atom.AtomId,
                        TenantId = atom.TenantId,
                        ModelId = _defaultModelId,
                        EmbeddingType = "semantic", // From CLR model inference
                        Dimension = dimension,
                        EmbeddingVector = new Microsoft.Data.SqlTypes.SqlVector<float>(embedding),
                        SpatialKey = spatialKey,  // ? REAL 3D GEOMETRY from CLR
                        HilbertValue = hilbertValue,  // ? REAL HILBERT VALUE from CLR
                        SpatialBucketX = bucketX,
                        SpatialBucketY = bucketY,
                        SpatialBucketZ = bucketZ,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    dbContext.AtomEmbeddings.Add(atomEmbedding);

                    _logger.LogInformation(
                        "Embedding created: AtomId={AtomId}, Dimension={Dimension}, Hilbert={Hilbert}, Bucket=({BX},{BY},{BZ})",
                        atom.AtomId, dimension, hilbertValue, bucketX, bucketY, bucketZ);

                    // Mark job as complete
                    if (jobId.HasValue)
                    {
                        processedJobIds.Add(jobId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate embedding for atom {AtomId}",
                        atom.AtomId);

                    // Mark job as failed
                    if (jobId.HasValue)
                    {
                        await backgroundJobService.UpdateJobAsync(
                            jobId.Value,
                            "Failed",
                            errorMessage: ex.Message,
                            cancellationToken: stoppingToken);
                    }
                }
            }

            // Save all embeddings in one transaction
            var savedCount = await dbContext.SaveChangesAsync(stoppingToken);

            // Update all successful jobs
            foreach (var jobId in processedJobIds)
            {
                await backgroundJobService.UpdateJobAsync(
                    jobId,
                    "Completed",
                    resultJson: "Embedding generated successfully",
                    cancellationToken: stoppingToken);
            }

            telemetry?.TrackMetric("EmbeddingGenerator.EmbeddingsGenerated", savedCount);
            telemetry?.TrackMetric("EmbeddingGenerator.JobsCompleted", processedJobIds.Count);
            telemetry?.TrackEvent("EmbeddingGenerator.BatchCompleted", new Dictionary<string, string>
            {
                ["EmbeddingsGenerated"] = savedCount.ToString(),
                ["JobsCompleted"] = processedJobIds.Count.ToString(),
                ["BatchSize"] = _batchSize.ToString()
            });

            if (operation != null)
            {
                operation.Telemetry.Success = true;
            }

            _logger.LogInformation(
                "Batch processing completed: {Count} embeddings generated, {JobCount} jobs completed", 
                savedCount, processedJobIds.Count);
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
    /// Compute embedding using CLR function dbo.fn_ComputeEmbedding
    /// This calls EmbeddingFunctions.cs which loads transformer weights from TensorAtoms
    /// and runs proper forward pass
    /// </summary>
    private async Task<byte[]?> ComputeEmbeddingAsync(
        long atomId,
        int modelId,
        int tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(@"
            SELECT dbo.fn_ComputeEmbedding(@AtomId, @ModelId, @TenantId)", connection);
        
        command.Parameters.Add("@AtomId", SqlDbType.BigInt).Value = atomId;
        command.Parameters.Add("@ModelId", SqlDbType.Int).Value = modelId;
        command.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;
        command.CommandTimeout = 120; // Embedding computation can take time

        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return (byte[])result;
    }

    /// <summary>
    /// Project 1998D embedding to 3D spatial point using CLR function dbo.fn_ProjectTo3D
    /// Uses landmark projection with SVD for dimensionality reduction
    /// </summary>
    private async Task<Geometry?> ProjectTo3DAsync(
        byte[] embeddingBytes,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(@"
            SELECT dbo.fn_ProjectTo3D(@EmbeddingVector).ToString()", connection);
        
        command.Parameters.Add("@EmbeddingVector", SqlDbType.VarBinary, -1).Value = embeddingBytes;

        var wkt = await command.ExecuteScalarAsync(cancellationToken) as string;
        
        if (string.IsNullOrEmpty(wkt))
        {
            return null;
        }

        // Parse WKT to Geometry
        var reader = new WKTReader();
        return reader.Read(wkt);
    }

    /// <summary>
    /// Compute Hilbert curve value for cache-friendly spatial indexing
    /// Uses CLR function dbo.clr_ComputeHilbertValue
    /// </summary>
    private async Task<long> ComputeHilbertValueAsync(
        Geometry spatialKey,
        int precision,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Convert Geometry to SqlGeometry
        var writer = new WKTWriter();
        var wkt = writer.Write(spatialKey);

        await using var command = new SqlCommand(@"
            SELECT dbo.clr_ComputeHilbertValue(geometry::STGeomFromText(@WKT, 0), @Precision)", connection);
        
        command.Parameters.Add("@WKT", SqlDbType.NVarChar, -1).Value = wkt;
        command.Parameters.Add("@Precision", SqlDbType.Int).Value = precision;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        if (result == null || result == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt64(result);
    }

    /// <summary>
    /// Compute spatial buckets for grid-based indexing
    /// Divides 3D space into 0.1 unit cubes for efficient range queries
    /// </summary>
    private static (int bucketX, int bucketY, int bucketZ) ComputeSpatialBuckets(Geometry spatialKey)
    {
        var point = (Point)spatialKey;
        var bucketSize = 0.1;
        
        return (
            (int)Math.Floor(point.X / bucketSize),
            (int)Math.Floor(point.Y / bucketSize),
            (int)Math.Floor(point.Coordinate.Z / bucketSize)
        );
    }

    /// <summary>
    /// Convert byte array to float array
    /// </summary>
    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}

/// <summary>
/// Parameters for embedding generation job
/// </summary>
internal class EmbeddingJobParameters
{
    public long AtomId { get; set; }
    public int TenantId { get; set; }
    public string? Modality { get; set; }
}
