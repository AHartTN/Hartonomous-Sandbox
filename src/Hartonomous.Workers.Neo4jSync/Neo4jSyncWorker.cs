using System.Xml.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Hartonomous.Workers.Neo4jSync;

/// <summary>
/// Background worker that polls SQL Server Service Broker Neo4jSyncQueue
/// and synchronizes provenance graph data to Neo4j.
/// 
/// CRITICAL: Uses IServiceScopeFactory to create scopes for scoped services
/// since BackgroundService is a singleton.
/// </summary>
public class Neo4jSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Neo4jSyncWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public Neo4jSyncWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<Neo4jSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("HartonomousDb connection string not found");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Neo4j Sync Worker starting...");

        // Delay startup to ensure database is ready
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextMessageAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Neo4j Sync Worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Neo4j Sync Worker stopping...");
    }

    private async Task ProcessNextMessageAsync(CancellationToken stoppingToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(stoppingToken);

        await using var transaction = connection.BeginTransaction();

        try
        {
            // RECEIVE message from Service Broker queue with 5 second timeout
            await using var command = new SqlCommand(@"
                WAITFOR (
                    RECEIVE TOP(1) 
                        conversation_handle, 
                        message_body,
                        message_type_name
                    FROM dbo.Neo4jSyncQueue
                ), TIMEOUT 5000", connection, transaction);

            await using var reader = await command.ExecuteReaderAsync(stoppingToken);

            if (await reader.ReadAsync(stoppingToken))
            {
                var conversationHandle = reader.GetGuid(0);
                var messageBody = reader.GetString(1);
                var messageTypeName = reader.GetString(2);

                await reader.CloseAsync();

                _logger.LogInformation(
                    "Received message from Neo4jSyncQueue: Type={MessageType}, Length={Length}",
                    messageTypeName, messageBody.Length);

                // Process the sync message
                var syncSuccess = await ProcessSyncMessageAsync(messageBody, stoppingToken);

                // Only END CONVERSATION if Neo4j sync succeeded
                // This ensures message stays in queue if sync fails (for retry)
                if (syncSuccess)
                {
                    await using var endConversation = new SqlCommand(
                        $"END CONVERSATION @ConversationHandle",
                        connection, transaction);
                    endConversation.Parameters.AddWithValue("@ConversationHandle", conversationHandle);
                    await endConversation.ExecuteNonQueryAsync(stoppingToken);

                    await transaction.CommitAsync(stoppingToken);

                    _logger.LogInformation("Neo4j sync message processed successfully");
                }
                else
                {
                    // Rollback to leave message in queue for retry
                    await transaction.RollbackAsync(stoppingToken);
                    _logger.LogWarning("Neo4j sync failed, message left in queue for retry");
                }
            }
            else
            {
                // No message received within timeout - this is normal
                await transaction.CommitAsync(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Neo4j sync message");
            await transaction.RollbackAsync(stoppingToken);
            throw;
        }
    }

    private async Task<bool> ProcessSyncMessageAsync(string messageBody, CancellationToken stoppingToken)
    {
        // Parse XML message from Service Broker
        var messageXml = XDocument.Parse(messageBody);
        var root = messageXml.Root;
        if (root == null || root.Name != "Neo4jSync")
        {
            _logger.LogWarning("Invalid Neo4j sync message format: {MessageBody}", messageBody);
            return false; // Invalid message, don't retry
        }

        // Extract values from XML
        var entityType = root.Element("EntityType")?.Value ?? "Unknown";
        var entityId = long.Parse(root.Element("EntityId")?.Value ?? "0");
        var syncType = root.Element("SyncType")?.Value ?? "CREATE";
        var tenantId = int.Parse(root.Element("TenantId")?.Value ?? "0");
        
        var request = new Neo4jSyncMessage
        {
            EntityType = entityType,
            EntityId = entityId,
            SyncType = syncType,
            TenantId = tenantId
        };

        // Create a scope to resolve scoped services
        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Resolve Neo4j driver and telemetry
        var driver = serviceProvider.GetRequiredService<IDriver>();
        var telemetry = serviceProvider.GetService<TelemetryClient>();

        using var operation = telemetry?.StartOperation<DependencyTelemetry>("Neo4j.SyncProvenance");

        try
        {
            if (operation != null)
            {
                operation.Telemetry.Type = "Neo4j";
                operation.Telemetry.Target = "Neo4j";
                operation.Telemetry.Data = $"Sync {request.EntityType} {request.EntityId}";
                operation.Telemetry.Properties["EntityType"] = request.EntityType;
                operation.Telemetry.Properties["TenantId"] = request.TenantId.ToString();
            }

            _logger.LogInformation(
                "Syncing to Neo4j: EntityType={EntityType}, EntityId={EntityId}, SyncType={SyncType}, TenantId={TenantId}",
                request.EntityType, request.EntityId, request.SyncType, request.TenantId);

            // CRITICAL: Use MERGE instead of CREATE to ensure idempotency
            // If the queue message is processed twice, we won't create duplicate nodes
            await using var session = driver.AsyncSession();
            await session.ExecuteWriteAsync(async tx =>
            {
                // MERGE creates node only if it doesn't exist, updates if it does
                var query = $@"
                    MERGE (e:{request.EntityType} {{id: $entityId, tenantId: $tenantId}})
                    SET e.syncType = $syncType,
                        e.lastSynced = datetime()
                    RETURN e";

                await tx.RunAsync(query,
                    new { entityId = request.EntityId, syncType = request.SyncType, tenantId = request.TenantId });
            });

            telemetry?.TrackMetric("Neo4j.EntitiesSynced", 1);
            telemetry?.TrackEvent("Neo4j.SyncCompleted", new Dictionary<string, string>
            {
                ["EntityType"] = request.EntityType,
                ["EntityId"] = request.EntityId.ToString(),
                ["SyncType"] = request.SyncType,
                ["TenantId"] = request.TenantId.ToString()
            });

            if (operation != null)
            {
                operation.Telemetry.Success = true;
            }

            _logger.LogInformation("Neo4j sync completed: {EntityType} {EntityId}", request.EntityType, request.EntityId);
            return true; // Success - message can be removed from queue
        }
        catch (Exception ex)
        {
            if (operation != null)
            {
                operation.Telemetry.Success = false;
            }

            _logger.LogError(ex, "Failed to sync to Neo4j");
            
            // Return false to keep message in queue for retry
            return false;
        }
    }
}

/// <summary>
/// Represents a message from the Service Broker Neo4jSyncQueue
/// Matches the XML schema from dbo.sp_EnqueueNeo4jSync
/// </summary>
public class Neo4jSyncMessage
{
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string SyncType { get; set; } = "CREATE";
    public int TenantId { get; set; }
}
