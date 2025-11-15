# 13 - Worker Services: Architecture and Implementation

The Hartonomous platform relies on a constellation of background services to orchestrate complex, long-running tasks. These workers are the "nervous system" that coordinates between the database engine, the provenance graph, and external systems. Each service has a single, well-defined responsibility.

## 1. The Worker Service Pattern

All workers follow the standard .NET `BackgroundService` pattern introduced in ASP.NET Core. This provides:

- **Dependency Injection:** Full DI container support for clean architecture
- **Lifecycle Management:** Graceful startup and shutdown
- **Health Checks:** Built-in health check endpoints for monitoring
- **Logging:** Structured logging via `ILogger`

### Base Worker Template

```csharp
public class ExampleWorker : BackgroundService
{
    private readonly ILogger<ExampleWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ExampleWorker(
        ILogger<ExampleWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a scope for each iteration to manage DbContext lifecycle
                using var scope = _scopeFactory.CreateScope();

                // Get scoped services
                var repository = scope.ServiceProvider
                    .GetRequiredService<IExampleRepository>();

                // Do work
                await DoWorkAsync(repository, stoppingToken);

                // Wait before next iteration
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker execution");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task DoWorkAsync(
        IExampleRepository repository,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## 2. Worker Services Overview

The Hartonomous platform includes five core worker services:

1. **`Hartonomous.Workers.Ingestion`** - Listens to Service Broker and atomizes incoming data
2. **`Hartonomous.Workers.Neo4jSync`** - Synchronizes provenance data to Neo4j
3. **`Hartonomous.Workers.EmbeddingGenerator`** - Generates embeddings for new atoms
4. **`Hartonomous.Workers.SpatialProjector`** - Projects high-dim vectors to 2D/3D geometry
5. **`Hartonomous.Workers.Gpu`** (Optional) - Out-of-process GPU acceleration service

## 3. Ingestion Worker (Critical Path)

### Responsibility
Consumes raw data events from SQL Service Broker, atomizes the content, and inserts atoms into the database.

### Event Flow

```
SQL Service Broker Queue
  ↓
Ingestion Worker (Dequeue)
  ↓
AtomizationPipeline (from Core)
  ↓
Atomizer (Text/Image/Model specific)
  ↓
SQL Server (sp_AtomizeAndInsert)
  ↓
Publish "Atoms Created" Event → Service Broker
```

### Implementation

```csharp
public class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceBrokerClient _serviceBroker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Receive message from Service Broker queue
                var message = await _serviceBroker.ReceiveAsync(
                    "IngestionQueue",
                    timeout: TimeSpan.FromSeconds(30),
                    stoppingToken);

                if (message == null)
                    continue;

                using var scope = _scopeFactory.CreateScope();
                var pipeline = scope.ServiceProvider
                    .GetRequiredService<IAtomIngestionPipeline>();

                // Deserialize the message payload
                var ingestionRequest = JsonSerializer
                    .Deserialize<IngestionRequest>(message.Body);

                // Execute the atomization pipeline
                var result = await pipeline.AtomizeAsync(
                    ingestionRequest.SourceId,
                    ingestionRequest.ContentType,
                    ingestionRequest.RawData,
                    stoppingToken);

                _logger.LogInformation(
                    "Atomized source {SourceId}: {AtomCount} atoms created",
                    ingestionRequest.SourceId,
                    result.AtomCount);

                // Acknowledge the message
                await _serviceBroker.EndConversationAsync(
                    message.ConversationHandle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ingestion message");
            }
        }
    }
}
```

### Atomization Pipeline Interface

```csharp
public interface IAtomIngestionPipeline
{
    Task<AtomizationResult> AtomizeAsync(
        int sourceId,
        string contentType,
        byte[] rawData,
        CancellationToken cancellationToken);
}
```

### Pipeline Implementation Strategy

The pipeline uses a **strategy pattern** to select the appropriate atomizer:

```csharp
public class AtomIngestionPipeline : IAtomIngestionPipeline
{
    private readonly IEnumerable<IAtomizer> _atomizers;
    private readonly IAtomRepository _atomRepository;

    public async Task<AtomizationResult> AtomizeAsync(
        int sourceId,
        string contentType,
        byte[] rawData,
        CancellationToken cancellationToken)
    {
        // Select the appropriate atomizer
        var atomizer = _atomizers.FirstOrDefault(a => a.CanHandle(contentType))
            ?? throw new NotSupportedException($"No atomizer for {contentType}");

        // Atomize the content
        var atoms = await atomizer.AtomizeAsync(rawData, cancellationToken);

        // Insert via stored procedure (handles deduplication)
        await _atomRepository.InsertAtomsAsync(sourceId, atoms, cancellationToken);

        return new AtomizationResult { AtomCount = atoms.Count };
    }
}
```

### Atomizer Interface

```csharp
public interface IAtomizer
{
    bool CanHandle(string contentType);

    Task<IReadOnlyList<AtomData>> AtomizeAsync(
        byte[] rawData,
        CancellationToken cancellationToken);
}
```

### Example: Text Atomizer

```csharp
public class TextAtomizer : IAtomizer
{
    public bool CanHandle(string contentType) =>
        contentType.StartsWith("text/");

    public async Task<IReadOnlyList<AtomData>> AtomizeAsync(
        byte[] rawData,
        CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(rawData);
        var atoms = new List<AtomData>();

        // Tokenize (example: by sentence)
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < sentences.Length; i++)
        {
            var sentenceBytes = Encoding.UTF8.GetBytes(sentences[i].Trim());
            var hash = SHA256.HashData(sentenceBytes);

            atoms.Add(new AtomData
            {
                AtomHash = Convert.ToHexString(hash),
                Content = sentenceBytes,
                ContentType = "text/sentence",
                Ordinal = i
            });
        }

        return atoms;
    }
}
```

## 4. Neo4j Sync Worker (Provenance Builder)

### Responsibility
Listens for "Atoms Created" and "Inference Completed" events, then creates corresponding nodes and relationships in Neo4j.

### Implementation

```csharp
public class Neo4jSyncWorker : BackgroundService
{
    private readonly IServiceBrokerClient _serviceBroker;
    private readonly IDriver _neo4jDriver; // Neo4j.Driver

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _serviceBroker.ReceiveAsync(
                "ProvenanceQueue",
                timeout: TimeSpan.FromSeconds(30),
                stoppingToken);

            if (message == null)
                continue;

            var eventData = JsonSerializer
                .Deserialize<ProvenanceEvent>(message.Body);

            await using var session = _neo4jDriver.AsyncSession();

            switch (eventData.EventType)
            {
                case "AtomsCreated":
                    await SyncAtomsToNeo4jAsync(session, eventData);
                    break;
                case "InferenceCompleted":
                    await SyncInferenceToNeo4jAsync(session, eventData);
                    break;
            }

            await _serviceBroker.EndConversationAsync(message.ConversationHandle);
        }
    }

    private async Task SyncAtomsToNeo4jAsync(
        IAsyncSession session,
        ProvenanceEvent eventData)
    {
        // Use MERGE for idempotency
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync(@"
                MERGE (a:Atom {atomHash: $hash})
                ON CREATE SET
                  a.atomId = $id,
                  a.contentType = $contentType,
                  a.createdAt = datetime($createdAt)
                MERGE (s:Source {sourceId: $sourceId})
                MERGE (a)-[:INGESTED_FROM]->(s)",
                new
                {
                    hash = eventData.AtomHash,
                    id = eventData.AtomId,
                    contentType = eventData.ContentType,
                    createdAt = eventData.CreatedAt,
                    sourceId = eventData.SourceId
                });
        });
    }
}
```

## 5. Embedding Generator Worker (AI Pipeline Stage 1)

### Responsibility
Monitors for new atoms that don't have embeddings, generates embeddings (via local model or API), and stores them.

### Implementation

```csharp
public class EmbeddingGeneratorWorker : BackgroundService
{
    private readonly IEmbeddingModel _embeddingModel;
    private readonly IAtomRepository _atomRepository;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<IAtomRepository>();

            // Query for atoms without embeddings
            var unembeddedAtoms = await repository
                .GetAtomsWithoutEmbeddingsAsync(batchSize: 100, stoppingToken);

            foreach (var atom in unembeddedAtoms)
            {
                // Generate embedding
                var vector = await _embeddingModel.GenerateAsync(
                    atom.Content,
                    stoppingToken);

                // Store in database
                await repository.InsertEmbeddingAsync(
                    atom.AtomId,
                    vector,
                    embeddingTypeId: 1, // From configuration
                    stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

## 6. Spatial Projector Worker (AI Pipeline Stage 2)

### Responsibility
Takes high-dimensional embeddings and projects them to 2D/3D `GEOMETRY` for spatial indexing.

### Implementation

```csharp
public class SpatialProjectorWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<IAtomRepository>();

            // Get embeddings without spatial projections
            var unprojectedEmbeddings = await repository
                .GetEmbeddingsWithoutSpatialProjectionsAsync(
                    batchSize: 1000,
                    stoppingToken);

            // Use T-SQL/CLR to perform dimensionality reduction
            // (e.g., t-SNE, UMAP, or custom projection)
            await repository.ExecuteStoredProcedureAsync(
                "dbo.sp_ProjectEmbeddingsToGeometry",
                new { embeddingIds = unprojectedEmbeddings.Select(e => e.Id) },
                stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## 7. GPU Worker (Optional, Out-of-Process)

### Responsibility
Provides GPU-accelerated computation via IPC for CLR functions.

### Architecture
This is a standalone console application, not a `BackgroundService`. It hosts a Named Pipe server and listens for computation requests.

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var context = Context.Create(deviceBuilder => deviceBuilder.AllAccelerators());
        var accelerator = context.GetPreferredDevice(preferCPU: false).CreateAccelerator(context);

        Console.WriteLine($"GPU Worker started on {accelerator.Name}");

        var server = new GpuIpcServer(accelerator);
        await server.ListenAsync();
    }
}

public class GpuIpcServer
{
    private readonly Accelerator _accelerator;

    public async Task ListenAsync()
    {
        var server = new NamedPipeServerStream(
            "hartonomous-gpu-pipe",
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous);

        while (true)
        {
            await server.WaitForConnectionAsync();

            // Read request
            var request = await ReadRequestAsync(server);

            // Execute on GPU
            var result = request.Operation switch
            {
                GpuOperation.DotProduct => ComputeDotProduct(request.Payload),
                _ => throw new NotSupportedException()
            };

            // Write response
            await WriteResponseAsync(server, result);

            server.Disconnect();
        }
    }
}
```

## 8. Configuration and Deployment

### appsettings.json for Workers

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;",
    "Neo4j": "bolt://localhost:7687"
  },
  "ServiceBroker": {
    "IngestionQueue": "IngestionQueue",
    "ProvenanceQueue": "ProvenanceQueue"
  },
  "EmbeddingModel": {
    "Type": "ONNX",
    "ModelPath": "./models/all-MiniLM-L6-v2.onnx"
  },
  "Workers": {
    "Ingestion": {
      "Enabled": true,
      "BatchSize": 100
    },
    "Neo4jSync": {
      "Enabled": true,
      "RetryDelaySeconds": 5
    },
    "EmbeddingGenerator": {
      "Enabled": true,
      "PollingIntervalSeconds": 10
    },
    "SpatialProjector": {
      "Enabled": true,
      "PollingIntervalSeconds": 60
    }
  }
}
```

### Deployment as Windows Services

```powershell
# Install as Windows Service
sc.exe create "Hartonomous.Workers.Ingestion" binPath="C:\Services\Hartonomous.Workers.Ingestion.exe"
sc.exe start "Hartonomous.Workers.Ingestion"
```

### Docker Compose for Development

```yaml
version: '3.8'
services:
  ingestion-worker:
    build: ./src/Hartonomous.Workers.Ingestion
    environment:
      - ConnectionStrings__HartonomousDb=Server=sqlserver;Database=Hartonomous;User=sa;Password=YourPassword
    depends_on:
      - sqlserver
      - neo4j

  neo4j-sync-worker:
    build: ./src/Hartonomous.Workers.Neo4jSync
    environment:
      - ConnectionStrings__Neo4j=bolt://neo4j:7687
    depends_on:
      - neo4j
```

## 9. Monitoring and Health Checks

All workers should expose health check endpoints:

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddCheck<ServiceBrokerHealthCheck>("service-broker");

app.MapHealthChecks("/health");
```

This architecture provides a robust, scalable foundation for orchestrating the Hartonomous platform's complex AI workflows.
