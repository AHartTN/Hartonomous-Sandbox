using System.Xml.Linq;
using Hartonomous.Core.Services;
using Hartonomous.Infrastructure.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Workers.CesConsumer;

/// <summary>
/// Background worker that polls SQL Server Service Broker IngestionQueue
/// and processes atomization requests using the ingestion pipeline.
/// 
/// CRITICAL: Uses IServiceScopeFactory to create scopes for scoped services
/// (DbContext, IIngestionService) since BackgroundService is a singleton.
/// </summary>
public class CesConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CesConsumerWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public CesConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CesConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("HartonomousDb") 
            ?? throw new InvalidOperationException("HartonomousDb connection string not found");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CES Consumer Worker starting...");

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
                _logger.LogError(ex, "Error in CES Consumer Worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("CES Consumer Worker stopping...");
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
                    FROM dbo.IngestionQueue
                ), TIMEOUT 5000", connection, transaction);

            await using var reader = await command.ExecuteReaderAsync(stoppingToken);

            if (await reader.ReadAsync(stoppingToken))
            {
                var conversationHandle = reader.GetGuid(0);
                var messageBody = reader.GetString(1);
                var messageTypeName = reader.GetString(2);

                await reader.CloseAsync();

                _logger.LogInformation(
                    "Received message from IngestionQueue: Type={MessageType}, Length={Length}",
                    messageTypeName, messageBody.Length);

                // Process the ingestion message
                await ProcessIngestionMessageAsync(messageBody, stoppingToken);

                // End conversation
                await using var endConversation = new SqlCommand(
                    $"END CONVERSATION @ConversationHandle",
                    connection, transaction);
                endConversation.Parameters.AddWithValue("@ConversationHandle", conversationHandle);
                await endConversation.ExecuteNonQueryAsync(stoppingToken);

                await transaction.CommitAsync(stoppingToken);

                _logger.LogInformation("Message processed successfully");
            }
            else
            {
                // No message received within timeout - this is normal
                await transaction.CommitAsync(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Service Broker message");
            await transaction.RollbackAsync(stoppingToken);
            throw;
        }
    }

    private async Task ProcessIngestionMessageAsync(string messageBody, CancellationToken stoppingToken)
    {
        // Parse XML message from Service Broker
        var messageXml = XDocument.Parse(messageBody);
        var root = messageXml.Root;
        if (root == null || root.Name != "IngestionRequest")
        {
            _logger.LogWarning("Invalid ingestion message format: {MessageBody}", messageBody);
            return;
        }

        // Extract values from XML
        var fileName = root.Element("FileName")?.Value;
        var tenantId = int.Parse(root.Element("TenantId")?.Value ?? "0");
        var fileDataHex = root.Element("FileDataHex")?.Value;
        
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileDataHex))
        {
            _logger.LogWarning("Missing required fields in ingestion message");
            return;
        }
        
        // Convert hex string back to byte array
        var fileData = Convert.FromHexString(fileDataHex.TrimStart('0', 'x'));
        
        var request = new IngestionMessage
        {
            FileName = fileName,
            TenantId = tenantId,
            FileData = fileData
        };

        // Create a scope to resolve scoped services (DbContext, IIngestionService)
        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Resolve services from the scope
        var ingestionService = serviceProvider.GetRequiredService<IIngestionService>();
        var telemetry = serviceProvider.GetService<TelemetryClient>();

        using var operation = telemetry?.StartOperation<RequestTelemetry>("CES.ProcessIngestion");

        try
        {
            if (operation != null)
            {
                operation.Telemetry.Properties["FileName"] = request.FileName ?? "unknown";
                operation.Telemetry.Properties["TenantId"] = request.TenantId.ToString();
            }

            _logger.LogInformation(
                "Processing ingestion: FileName={FileName}, TenantId={TenantId}, DataSize={Size}",
                request.FileName, request.TenantId, request.FileData?.Length ?? 0);

            // Process the file through the ingestion pipeline
            var result = await ingestionService.IngestFileAsync(
                request.FileData ?? Array.Empty<byte>(),
                request.FileName ?? "unknown",
                request.TenantId);

            telemetry?.TrackMetric("CES.AtomsCreated", result.ItemsProcessed);
            telemetry?.TrackEvent("CES.IngestionCompleted", new Dictionary<string, string>
            {
                ["FileName"] = request.FileName ?? "unknown",
                ["AtomCount"] = result.ItemsProcessed.ToString(),
                ["TenantId"] = request.TenantId.ToString()
            });

            if (operation != null)
            {
                operation.Telemetry.Success = true;
            }

            _logger.LogInformation(
                "Ingestion completed: {ItemsProcessed} atoms created",
                result.ItemsProcessed);
        }
        catch (Exception ex)
        {
            if (operation != null)
            {
                operation.Telemetry.Success = false;
            }

            _logger.LogError(ex, "Failed to process ingestion request");
            throw;
        }
    }
}

/// <summary>
/// Represents a message from the Service Broker IngestionQueue
/// </summary>
public class IngestionMessage
{
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public int TenantId { get; set; }
}
