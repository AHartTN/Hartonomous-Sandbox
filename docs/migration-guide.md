# Migration Guide: Refactored Architecture

This guide walks through migrating existing projects to use the new refactored abstractions.

---

## CesConsumer Migration

### Step 1: Update Program.cs

**Replace this:**
```csharp
var eventHubConnectionString = context.Configuration["EventHub:ConnectionString"]
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key";
var eventHubName = context.Configuration["EventHub:Name"] ?? "sqlserver-ces-events";

services.AddSingleton<CdcListener>(sp => new CdcListener(
    sp.GetRequiredService<ICdcRepository>(),
    sp.GetRequiredService<ILogger<CdcListener>>(),
    eventHubConnectionString,
    eventHubName));
```

**With this:**
```csharp
using Hartonomous.Infrastructure.Extensions;
using Hartonomous.Infrastructure.Services.Enrichment;
using Hartonomous.Infrastructure.Services.CDC;
using CesConsumer.Services;

// Register Event Hub publisher
services.AddEventHubPublisher(context.Configuration);

// Register enrichment service
services.AddSingleton<ICloudEventEnricher, CloudEventEnricher>();

// Register checkpoint manager (choose one)
// Development: File-based
services.AddSingleton<ICdcCheckpointManager, FileCdcCheckpointManager>();

// Production: SQL-based
// services.AddSingleton<ICdcCheckpointManager, SqlCdcCheckpointManager>();

// Register refactored CDC listener
services.AddSingleton<RefactoredCdcListener>();
services.AddHostedService(sp => sp.GetRequiredService<RefactoredCdcListener>());
```

### Step 2: Update appsettings.json

**Add EventHub configuration section:**
```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;"
  },
  "EventHub": {
    "ConnectionString": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
    "Name": "sqlserver-ces-events",
    "MaxBatchSize": 100,
    "MaxRetryAttempts": 3
  }
}
```

### Step 3: Remove old CdcListener.cs

After testing, delete `src/CesConsumer/CdcListener.cs`

---

## Neo4jSync Migration

### Step 1: Update CloudEvent References

**Remove local CloudEvent class:**
```csharp
// DELETE this from Program.cs
public class CloudEvent
{
    public string Id { get; set; } = string.Empty;
    // ... 
}
```

**Add using statement:**
```csharp
using Hartonomous.Core.Models;
```

### Step 2: Update Program.cs

**Replace manual service registration:**
```csharp
var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
    ?? "Endpoint=sb://localhost;...";

builder.Services.AddSingleton<EventProcessorClient>(sp =>
{
    var storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? "UseDevelopmentStorage=true";
    var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(storageConnectionString);
    // ...
});

builder.Services.AddSingleton<IDriver>(sp =>
{
    var uri = Environment.GetEnvironmentVariable("NEO4J_URI") ?? "bolt://localhost:7687";
    // ...
});
```

**With extension methods:**
```csharp
using Hartonomous.Infrastructure.Extensions;

// Register Event Hub consumer
builder.Services.AddEventHubConsumer(builder.Configuration);

// Register Neo4j
builder.Services.AddNeo4j(builder.Configuration);

// Register services
builder.Services.AddHostedService<CloudEventProcessor>();
builder.Services.AddSingleton<ProvenanceGraphBuilder>();
```

### Step 3: Update appsettings.json

**Add configuration sections:**
```json
{
  "EventHub": {
    "ConnectionString": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
    "Name": "sqlserver-ces-events",
    "ConsumerGroup": "$Default",
    "BlobStorageConnectionString": "UseDevelopmentStorage=true",
    "BlobContainerName": "eventhub-checkpoints"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "password",
    "MaxConnectionPoolSize": 100,
    "ConnectionTimeoutSeconds": 30
  }
}
```

### Step 4: Update CloudEventProcessor

**Modify to use IEventConsumer (optional future enhancement):**

Current approach works, but for consistency you could refactor:

```csharp
public class CloudEventProcessor : BackgroundService
{
    private readonly IEventConsumer _consumer;
    private readonly IProvenanceGraphBuilder _graphBuilder;
    private readonly ILogger<CloudEventProcessor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.StartAsync(async (eventObj, ct) =>
        {
            var cloudEvent = eventObj as CloudEvent;
            if (cloudEvent != null)
            {
                await ProcessCloudEventAsync(cloudEvent, ct);
            }
        }, stoppingToken);
    }
}
```

---

## SQL Server Checkpoint Table (Optional for Production)

If using `SqlCdcCheckpointManager`, create this table:

```sql
CREATE TABLE dbo.CdcCheckpoints
(
    ConsumerName NVARCHAR(128) NOT NULL PRIMARY KEY,
    LastProcessedLsn NVARCHAR(50) NOT NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- Create index for queries
CREATE INDEX IX_CdcCheckpoints_LastUpdated 
ON dbo.CdcCheckpoints(LastUpdated DESC);
GO
```

---

## Testing the Migration

### 1. CesConsumer Testing

```bash
# Start the service
cd src/CesConsumer
dotnet run

# Expected log output:
# [INFO] Event Hub Publisher initialized for sqlserver-ces-events
# [INFO] Starting CDC Consumer with CloudEvent processing
# [INFO] Starting from LSN: NULL
```

### 2. Neo4jSync Testing

```bash
# Ensure Azurite is running (for blob checkpoints)
azurite --silent --location c:\azurite --debug c:\azurite\debug.log

# Start the service
cd src/Neo4jSync
dotnet run

# Expected log output:
# [INFO] Event Hub Consumer initialized for sqlserver-ces-events in consumer group $Default
# [INFO] Event Hub Consumer started processing
```

### 3. End-to-End Test

1. Make a change in SQL Server that triggers CDC
2. Verify CesConsumer publishes to Event Hub
3. Verify Neo4jSync receives and processes the event
4. Check Neo4j for the new graph node

```cypher
// Query Neo4j to verify events are being processed
MATCH (e:Event)
WHERE e.id IS NOT NULL
RETURN e
ORDER BY e.time DESC
LIMIT 10;
```

---

## Configuration by Environment

### Development (appsettings.Development.json)

```json
{
  "EventHub": {
    "ConnectionString": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
    "Name": "ces-events-dev",
    "BlobStorageConnectionString": "UseDevelopmentStorage=true"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "dev-password"
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "EventHub": {
    "ConnectionString": "Endpoint=sb://hartonomous-prod.servicebus.windows.net/;SharedAccessKeyName=***;SharedAccessKey=***;",
    "Name": "ces-events",
    "ConsumerGroup": "neo4j-sync-prod",
    "BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=***;AccountKey=***;",
    "BlobContainerName": "eventhub-checkpoints",
    "MaxBatchSize": 500,
    "MaxRetryAttempts": 5
  },
  "Neo4j": {
    "Uri": "bolt://neo4j-prod.internal:7687",
    "Username": "app_user",
    "Password": "***",
    "MaxConnectionPoolSize": 200,
    "ConnectionTimeoutSeconds": 60
  }
}
```

### Environment Variables (Alternative)

You can also use environment variables:

```bash
# Event Hub
export EVENTHUB_CONNECTION_STRING="Endpoint=sb://..."
export EVENTHUB_NAME="ces-events"

# Neo4j
export NEO4J_URI="bolt://localhost:7687"
export NEO4J_PASSWORD="password"
```

Configuration binding will automatically read from:
1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. Command-line arguments

---

## Rollback Plan

If issues arise, you can roll back by:

1. **Keep old files:** Don't delete `CdcListener.cs` until fully tested
2. **Feature flag:** Add configuration to switch between old/new implementations
3. **Gradual migration:** Migrate CesConsumer first, then Neo4jSync

Example feature flag:

```csharp
if (context.Configuration.GetValue<bool>("UseRefactoredServices"))
{
    services.AddEventHubPublisher(context.Configuration);
    services.AddSingleton<RefactoredCdcListener>();
}
else
{
    services.AddSingleton<CdcListener>(...); // Old implementation
}
```

---

## Troubleshooting

### Issue: "Event Hub connection string is not configured"

**Solution:** Verify `EventHub:ConnectionString` in appsettings.json or environment variables

### Issue: "Failed to read checkpoint from database"

**Solution:** 
- Ensure `CdcCheckpoints` table exists
- Verify SQL connection string
- Check user permissions

### Issue: Events not being published

**Solution:**
- Check Event Hub namespace is active
- Verify SAS policy has Send permissions
- Check logs for retry attempts
- Verify network connectivity

### Issue: Neo4j connection timeout

**Solution:**
- Increase `Neo4j:ConnectionTimeoutSeconds`
- Check Neo4j is running: `neo4j status`
- Verify URI and credentials

---

## Performance Tuning

### Event Hub Batch Size

```json
{
  "EventHub": {
    "MaxBatchSize": 500  // Increase for higher throughput
  }
}
```

**Guidelines:**
- Development: 100
- Low volume: 100-250
- High volume: 500-1000

### Neo4j Connection Pool

```json
{
  "Neo4j": {
    "MaxConnectionPoolSize": 200  // Increase for concurrent queries
  }
}
```

**Guidelines:**
- Development: 50
- Production (low): 100
- Production (high): 200-500

---

## Next Steps

After successful migration:

1. ✅ Remove old `CdcListener.cs`
2. ✅ Update unit tests to use new interfaces
3. ✅ Add integration tests for refactored services
4. ✅ Update deployment documentation
5. ✅ Monitor performance metrics
6. ✅ Update team documentation/runbooks

---

**Migration Support:** Contact development team if issues arise  
**Estimated Migration Time:** 2-4 hours per service  
**Risk Level:** Low (backwards compatible, can run both implementations in parallel)
