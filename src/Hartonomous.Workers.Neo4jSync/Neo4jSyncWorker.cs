using System.Xml.Linq;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Hartonomous.Workers.Neo4jSync.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
        _logger.LogInformation("Connection string: {ConnectionString}", _connectionString);

        // Delay startup to ensure database is ready
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("Starting message processing loop...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Polling for next message...");
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

        _logger.LogDebug("Database connection opened for message processing");

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

            Guid? conversationHandle = null;
            string? messageBody = null;
            string? messageTypeName = null;
            bool messageReceived = false;

            await using (var reader = await command.ExecuteReaderAsync(stoppingToken))
            {
                if (await reader.ReadAsync(stoppingToken))
                {
                    conversationHandle = reader.GetGuid(0);
                    
                    // Message body is stored as VARBINARY(MAX), convert to string
                    // SQL Server CAST(@xml AS VARBINARY(MAX)) preserves the XML encoding
                    var messageBytes = (byte[])reader.GetValue(1);
                    
                    // Detect encoding: Check for UTF-16 BOM (FF FE) vs UTF-8
                    if (messageBytes.Length >= 2 && messageBytes[0] == 0xFF && messageBytes[1] == 0xFE)
                    {
                        // UTF-16 LE with BOM
                        messageBody = System.Text.Encoding.Unicode.GetString(messageBytes, 2, messageBytes.Length - 2);
                    }
                    else if (messageBytes.Length >= 3 && messageBytes[0] == 0xEF && messageBytes[1] == 0xBB && messageBytes[2] == 0xBF)
                    {
                        // UTF-8 with BOM
                        messageBody = System.Text.Encoding.UTF8.GetString(messageBytes, 3, messageBytes.Length - 3);
                    }
                    else
                    {
                        // Assume UTF-16 LE without BOM (SQL Server XML default)
                        messageBody = System.Text.Encoding.Unicode.GetString(messageBytes);
                    }
                    
                    messageTypeName = reader.GetString(2);
                    messageReceived = true;
                    _logger.LogInformation(
                        "Message RECEIVED - ConversationHandle={ConversationHandle}, Type={MessageType}, BodyLength={Length}",
                        conversationHandle, messageTypeName, messageBody?.Length ?? 0);
                }
                else
                {
                    _logger.LogDebug("No message available (timeout after 5 seconds)");
                }
            } // DataReader disposed here

            if (messageReceived && conversationHandle.HasValue && messageBody != null)
            {
                _logger.LogInformation("Processing message body: {MessageBody}", messageBody);

                // Process the sync message (pass conversationHandle for poison message handling)
                var (syncSuccess, conversationEnded) = await ProcessSyncMessageAsync(messageBody, conversationHandle.Value, stoppingToken);

                // Only END CONVERSATION if Neo4j sync succeeded AND conversation wasn't already ended
                // (Poison messages end conversation inside ProcessSyncMessageAsync)
                if (syncSuccess && !conversationEnded)
                {
                    _logger.LogInformation("Sync successful - ending conversation {ConversationHandle}", conversationHandle);
                    await using var endConversation = new SqlCommand(
                        $"END CONVERSATION @ConversationHandle",
                        connection, transaction);
                    endConversation.Parameters.AddWithValue("@ConversationHandle", conversationHandle.Value);
                    await endConversation.ExecuteNonQueryAsync(stoppingToken);

                    await transaction.CommitAsync(stoppingToken);

                    _logger.LogInformation("Neo4j sync message processed successfully and committed");
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
        catch (SqlException sqlEx) when (sqlEx.Number == 9617)
        {
            // Queue disabled - this shouldn't happen with POISON_MESSAGE_HANDLING OFF
            // But if it does, log and continue (deployment script will re-enable)
            _logger.LogError(sqlEx, "Service Broker queue disabled (Error 9617). Deployment script will fix on next run.");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Back off before retry
        }
        catch (Exception ex)
        {
            // Unexpected error in receive loop - log but don't crash the worker
            _logger.LogError(ex, "Unexpected error in message processing loop. Continuing...");
            try
            {
                await transaction.RollbackAsync(stoppingToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction after error");
            }
            // Don't re-throw - continue processing next message
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Brief delay before retry
        }
    }

    private async Task<(bool success, bool conversationEnded)> ProcessSyncMessageAsync(string messageBody, Guid conversationHandle, CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== BEGIN ProcessSyncMessageAsync ===");
        _logger.LogInformation("Raw message body: {MessageBody}", messageBody);

        try
        {
            // Parse XML message from Service Broker
            XDocument messageXml;
            try
            {
                messageXml = XDocument.Parse(messageBody);
            }
            catch (System.Xml.XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "Malformed XML message - ending conversation with error");
                await EndConversationWithErrorAsync(conversationHandle, "Malformed XML", messageBody, stoppingToken);
                return (true, true); // Success=true (commit), ConversationEnded=true
            }

            var root = messageXml.Root;
            if (root == null || root.Name != "Neo4jSync")
            {
                _logger.LogError("Invalid Neo4j sync message format - ending conversation with error: {MessageBody}", messageBody);
                await EndConversationWithErrorAsync(conversationHandle, "Invalid message format", messageBody, stoppingToken);
                return (true, true); // Success=true (commit), ConversationEnded=true
            }

            // Extract values from XML with error handling
            var entityType = root.Element("EntityType")?.Value ?? "Unknown";
            long entityId;
            int tenantId;
            try
            {
                entityId = long.Parse(root.Element("EntityId")?.Value ?? "0");
                tenantId = int.Parse(root.Element("TenantId")?.Value ?? "0");
            }
            catch (FormatException formatEx)
            {
                _logger.LogError(formatEx, "Invalid numeric values in message - ending conversation with error");
                await EndConversationWithErrorAsync(conversationHandle, "Invalid numeric values", messageBody, stoppingToken);
                return (true, true); // Success=true (commit), ConversationEnded=true
            }
            var syncType = root.Element("SyncType")?.Value ?? "CREATE";
            
            _logger.LogInformation(
                "Parsed XML - EntityType={EntityType}, EntityId={EntityId}, SyncType={SyncType}, TenantId={TenantId}",
                entityType, entityId, syncType, tenantId);

            var request = new Neo4jSyncMessage
            {
                EntityType = entityType,
                EntityId = entityId,
                SyncType = syncType,
                TenantId = tenantId
            };

            // Create a scope to resolve scoped services
            _logger.LogInformation("Creating service scope...");
            using var scope = _scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Resolve Neo4j driver and telemetry
            _logger.LogInformation("Resolving Neo4j driver...");
            var driver = serviceProvider.GetRequiredService<IDriver>();
            
            if (driver == null)
            {
                _logger.LogError("Neo4j driver is null - Neo4j may be disabled in configuration");
                return (false, false); // Failure - retry
            }
            
            _logger.LogInformation("Neo4j driver resolved successfully");

            var telemetry = serviceProvider.GetService<TelemetryClient>();

            using var operation = telemetry?.StartOperation<DependencyTelemetry>("Neo4j.SyncProvenance");

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
            _logger.LogInformation("Creating Neo4j session...");
            await using var session = driver.AsyncSession();
            
            _logger.LogInformation("Executing Neo4j write transaction...");
            await session.ExecuteWriteAsync(async tx =>
            {
                // MERGE creates node only if it doesn't exist, updates if it does
                var query = $@"
                    MERGE (e:{request.EntityType} {{id: $entityId, tenantId: $tenantId}})
                    SET e.syncType = $syncType,
                        e.lastSynced = datetime()
                    RETURN e";

                _logger.LogInformation("Executing Cypher query: {Query}", query);
                _logger.LogInformation("Parameters - entityId={EntityId}, syncType={SyncType}, tenantId={TenantId}",
                    request.EntityId, request.SyncType, request.TenantId);

                var result = await tx.RunAsync(query,
                    new { entityId = request.EntityId, syncType = request.SyncType, tenantId = request.TenantId });

                var records = await result.ToListAsync();
                _logger.LogInformation("Neo4j query executed - {RecordCount} records returned", records.Count);
                
                foreach (var record in records)
                {
                    _logger.LogInformation("Created/updated node: {Node}", record["e"]);
                }
            });

            _logger.LogInformation("Neo4j transaction committed successfully");

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

            _logger.LogInformation("=== END ProcessSyncMessageAsync (SUCCESS) ===");
            return (true, false); // Success=true, ConversationEnded=false (caller will end it)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== END ProcessSyncMessageAsync (FAILURE) - Exception occurred ===");
            return (false, false); // Failure=false (rollback and retry), ConversationEnded=false
        }
    }

    private async Task EndConversationWithErrorAsync(
        Guid conversationHandle,
        string errorDescription,
        string messageBody,
        CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HartonomousDbContext>();

            // Log to dead letter table for audit using proper EF Core entity
            try
            {
                var deadLetterEntry = new Neo4jSyncDeadLetterQueue
                {
                    ConversationHandle = conversationHandle,
                    ErrorDescription = errorDescription,
                    MessageBody = messageBody,
                    ErrorTimestamp = DateTime.UtcNow
                };
                
                dbContext.Neo4jSyncDeadLetterQueues.Add(deadLetterEntry);
                await dbContext.SaveChangesAsync(stoppingToken);
                
                _logger.LogInformation(
                    "Logged poison message to dead letter queue: DeadLetterId={DeadLetterId}",
                    deadLetterEntry.DeadLetterId);
            }
            catch (Exception logEx)
            {
                // If dead letter table doesn't exist or save fails, just log to application logs
                _logger.LogWarning(logEx, "Could not log to dead letter table (table may not exist yet)");
            }

            // End conversation with error (tells sender there's a problem)
            // Must use raw SQL for Service Broker commands (no EF Core abstraction)
            await dbContext.Database.ExecuteSqlRawAsync(
                "END CONVERSATION @p0 WITH ERROR = 50000 DESCRIPTION = @p1",
                new[] {
                    new SqlParameter("@p0", conversationHandle),
                    new SqlParameter("@p1", errorDescription)
                },
                stoppingToken);

            _logger.LogWarning(
                "Ended conversation {ConversationHandle} with error: {ErrorDescription}",
                conversationHandle, errorDescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end conversation with error for {ConversationHandle}", conversationHandle);
            // Don't re-throw - we want to continue processing
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
