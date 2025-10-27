using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Microsoft.Data.SqlClient;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Neo4jSyncWorker>();
builder.Services.AddSingleton<IDriver>(sp =>
{
    var uri = "bolt://localhost:7687";
    var user = "neo4j";
    var password = "neo4jneo4j";
    return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
});

var app = builder.Build();
await app.RunAsync();

public class Neo4jSyncWorker : BackgroundService
{
    private readonly ILogger<Neo4jSyncWorker> _logger;
    private readonly IDriver _neo4jDriver;
    private readonly string _sqlConnectionString = "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;";

    public Neo4jSyncWorker(ILogger<Neo4jSyncWorker> logger, IDriver neo4jDriver)
    {
        _logger = logger;
        _neo4jDriver = neo4jDriver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Neo4j Sync Service starting...");

        // Test Neo4j connection
        try
        {
            await using var session = _neo4jDriver.AsyncSession();
            var result = await session.RunAsync("RETURN 'Connected' as status");
            var record = await result.SingleAsync();
            _logger.LogInformation("Neo4j connection successful: {Status}", record["status"].As<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Neo4j");
            return;
        }

        // Test SQL Server connection
        try
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(stoppingToken);
            _logger.LogInformation("SQL Server connection successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SQL Server");
            return;
        }

        _logger.LogInformation("Starting sync loop - polling every 5 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncChanges(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task SyncChanges(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_sqlConnectionString);
        await conn.OpenAsync(ct);
        await using var session = _neo4jDriver.AsyncSession();

        // Sync KnowledgeBase documents
        var kbSynced = await SyncKnowledgeBase(conn, session, ct);

        // Sync Inference Requests (audit trail)
        var infSynced = await SyncInferenceRequests(conn, session, ct);

        if (kbSynced > 0 || infSynced > 0)
        {
            _logger.LogInformation("Synced {KB} knowledge docs, {Inf} inferences to Neo4j", kbSynced, infSynced);
        }
    }

    private async Task<int> SyncKnowledgeBase(SqlConnection conn, IAsyncSession session, CancellationToken ct)
    {
        var query = "SELECT doc_id, content, embedding, category FROM dbo.KnowledgeBase";
        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var synced = 0;
        while (await reader.ReadAsync(ct))
        {
            var docId = reader.GetInt32(0);
            var content = reader.GetString(1);
            var embedding = reader.GetString(2);
            var category = reader.GetString(3);

            var cypher = @"
                MERGE (d:Document {doc_id: $docId})
                SET d.content = $content,
                    d.embedding = $embedding,
                    d.category = $category,
                    d.last_synced = datetime()";

            try
            {
                await session.RunAsync(cypher, new { docId, content, embedding, category });
                synced++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing document {Id}", docId);
            }
        }
        return synced;
    }

    private async Task<int> SyncInferenceRequests(SqlConnection conn, IAsyncSession session, CancellationToken ct)
    {
        var query = @"
            SELECT TOP 100
                inference_id,
                task_type,
                models_used,
                ensemble_strategy,
                total_duration_ms,
                output_metadata
            FROM dbo.InferenceRequests
            ORDER BY inference_id DESC";

        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var synced = 0;
        while (await reader.ReadAsync(ct))
        {
            var inferenceId = reader.GetInt64(0);
            var taskType = reader.GetString(1);
            var modelsUsed = reader.IsDBNull(2) ? null : reader.GetString(2);
            var ensembleStrategy = reader.IsDBNull(3) ? null : reader.GetString(3);
            var duration = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
            var metadata = reader.IsDBNull(5) ? "{}" : reader.GetString(5);

            var cypher = @"
                MERGE (i:Inference {inference_id: $inferenceId})
                SET i.task_type = $taskType,
                    i.models_used = $modelsUsed,
                    i.ensemble_strategy = $ensembleStrategy,
                    i.duration_ms = $duration,
                    i.metadata = $metadata,
                    i.last_synced = datetime()";

            try
            {
                await session.RunAsync(cypher, new {
                    inferenceId,
                    taskType,
                    modelsUsed,
                    ensembleStrategy,
                    duration,
                    metadata
                });
                synced++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing inference {Id}", inferenceId);
            }
        }
        return synced;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Neo4j Sync Service stopping...");
        await _neo4jDriver.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
