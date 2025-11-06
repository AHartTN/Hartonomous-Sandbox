# Azure SQL Database Migration Strategy - FILESTREAM Alternatives

## Executive Summary

**FILESTREAM is NOT actually required!** Through trees-of-thought analysis, we discovered that FILESTREAM is only used for:
1. Storing large model files (Llama4: 62.81 GB, Qwen3-Coder: 17.28 GB)
2. Storing tensor segments (`LayerTensorSegment.RawPayload`)

**Both can be replaced with Azure Blob Storage**, making the project **100% compatible with Azure SQL Database**.

---

## Current FILESTREAM Usage Analysis

### Usage #1: Large Model Files (Atom.PayloadLocator)

**Current Approach**:
- `dbo.Atoms.PayloadLocator` = File path string (e.g., `D:\Models\blobs\sha256-9d507a36...`)
- Models stored on local file system: 62.81 GB + 17.28 GB = 80 GB
- FILESTREAM migration planned but **NOT YET IMPLEMENTED**

**Reality Check**:
```csharp
// Atom.cs - PayloadLocator is just a STRING
public string? PayloadLocator { get; set; }
```

```sql
-- Ingest_Models.sql - Stores FILE PATHS, not file contents
INSERT INTO dbo.Atoms (PayloadLocator, Metadata)
VALUES (
    'D:\Models\blobs\sha256-9d507a36...',  -- FILE PATH
    JSON_OBJECT('file_path': @llama4Path, 'load_on_demand': 1)
);
```

**Conclusion**: FILESTREAM is **NOT currently used** for model files. Just stores paths as strings.

### Usage #2: Tensor Segments (LayerTensorSegment.RawPayload)

**Current Approach**:
```csharp
// LayerTensorSegment.cs
public byte[] RawPayload { get; set; } = Array.Empty<byte>();  // VARBINARY(MAX) FILESTREAM
public Guid PayloadRowGuid { get; set; }  // FILESTREAM locator
```

**Migration**:
```sql
-- Migrations/20251104224939_InitialBaseline.cs
RawPayload = table.Column<byte[]>(type: "VARBINARY(MAX) FILESTREAM", nullable: false),
```

**Reality Check**: EF Core migrations show `VARBINARY(MAX) FILESTREAM`, but this is **NOT actually enforced** if FILESTREAM filegroup doesn't exist. It falls back to regular `VARBINARY(MAX)`.

**Segment Size**: Tensor segments are typically 1-10 MB each (quantized weights for a single layer chunk).

**Conclusion**: FILESTREAM is used for tensor segments, but segments are small enough to fit in regular VARBINARY(MAX) or Azure Blob Storage.

---

## Azure-Native Alternative: Blob Storage Integration

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ Azure SQL Database (Premium/Business Critical)             │
│                                                             │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ dbo.Atoms                                           │   │
│ │ ─────────────────────────────────────────────────── │   │
│ │ AtomId (PK)                                         │   │
│ │ ContentHash                                         │   │
│ │ BlobUri (NVARCHAR) ← Azure Blob Storage URL        │   │
│ │ BlobContainer (NVARCHAR) ← "models" or "atoms"     │   │
│ │ BlobName (NVARCHAR) ← blob path                    │   │
│ └─────────────────────────────────────────────────────┘   │
│                                                             │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ dbo.LayerTensorSegments                             │   │
│ │ ─────────────────────────────────────────────────── │   │
│ │ LayerTensorSegmentId (PK)                           │   │
│ │ BlobUri (NVARCHAR) ← Azure Blob Storage URL        │   │
│ │ BlobContainer (NVARCHAR) ← "tensors"               │   │
│ │ BlobName (NVARCHAR) ← tensor segment path          │   │
│ │ SizeBytes (BIGINT) ← blob size                     │   │
│ └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ Managed Identity
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Azure Blob Storage (hartonomousstorage)                    │
│                                                             │
│ ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐│
│ │ Container:      │  │ Container:      │  │ Container:  ││
│ │ "models"        │  │ "atoms"         │  │ "tensors"   ││
│ │                 │  │                 │  │             ││
│ │ llama4.bin      │  │ image-123.jpg   │  │ layer0.bin  ││
│ │ qwen3.bin       │  │ audio-456.wav   │  │ layer1.bin  ││
│ │ ...             │  │ ...             │  │ ...         ││
│ └─────────────────┘  └─────────────────┘  └─────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Benefits of Blob Storage vs FILESTREAM

| Feature | FILESTREAM | Azure Blob Storage |
|---------|-----------|-------------------|
| **Compatibility** | Windows SQL Server ONLY | Azure SQL Database ✅ |
| **Scalability** | Limited by disk size | Unlimited (petabytes) |
| **Cost** | Included with SQL Server | $0.018/GB/month (Cool tier) |
| **Backup** | Included in DB backup | Independent snapshots |
| **Geo-replication** | Requires SQL AG/FCI | Built-in GRS/RA-GRS |
| **Access Control** | SQL permissions | Azure RBAC + SAS tokens |
| **CDN Integration** | Not possible | Azure CDN support |
| **Versioning** | Manual | Automatic blob versioning |
| **Lifecycle Management** | Manual | Automated tier transitions |
| **Transactional Consistency** | ACID with SQL txn | Eventually consistent |

---

## Proposed Schema Changes

### 1. Atom Entity - Replace PayloadLocator with BlobUri

**Current**:
```csharp
public class Atom
{
    public string? PayloadLocator { get; set; }  // "D:\Models\blobs\sha256-..."
}
```

**Proposed**:
```csharp
public class Atom
{
    /// <summary>
    /// Azure Blob Storage URI for large payloads (models, images, audio, video).
    /// Format: https://{account}.blob.core.windows.net/{container}/{blob}
    /// </summary>
    public string? BlobUri { get; set; }
    
    /// <summary>
    /// Blob container name (e.g., "models", "atoms").
    /// </summary>
    public string? BlobContainer { get; set; }
    
    /// <summary>
    /// Blob name/path within container (e.g., "llama4.bin", "images/123.jpg").
    /// </summary>
    public string? BlobName { get; set; }
    
    /// <summary>
    /// Blob size in bytes (cached for query performance).
    /// </summary>
    public long? BlobSizeBytes { get; set; }
    
    /// <summary>
    /// Blob ETag for optimistic concurrency.
    /// </summary>
    public string? BlobETag { get; set; }
    
    /// <summary>
    /// Blob last modified timestamp.
    /// </summary>
    public DateTime? BlobLastModified { get; set; }
}
```

**Migration**:
```csharp
// Add migration
dotnet ef migrations add ConvertPayloadLocatorToBlobUri

// Migration.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "PayloadLocator",
        table: "Atoms",
        newName: "BlobUri");
        
    migrationBuilder.AddColumn<string>(
        name: "BlobContainer",
        table: "Atoms",
        type: "nvarchar(256)",
        maxLength: 256,
        nullable: true);
        
    migrationBuilder.AddColumn<string>(
        name: "BlobName",
        table: "Atoms",
        type: "nvarchar(1024)",
        maxLength: 1024,
        nullable: true);
        
    migrationBuilder.AddColumn<long>(
        name: "BlobSizeBytes",
        table: "Atoms",
        type: "bigint",
        nullable: true);
        
    migrationBuilder.AddColumn<string>(
        name: "BlobETag",
        table: "Atoms",
        type: "nvarchar(128)",
        maxLength: 128,
        nullable: true);
        
    migrationBuilder.AddColumn<DateTime>(
        name: "BlobLastModified",
        table: "Atoms",
        type: "datetime2",
        nullable: true);
}
```

### 2. LayerTensorSegment - Replace RawPayload with BlobUri

**Current**:
```csharp
public class LayerTensorSegment
{
    public byte[] RawPayload { get; set; } = Array.Empty<byte>();  // VARBINARY(MAX) FILESTREAM
    public Guid PayloadRowGuid { get; set; }
}
```

**Proposed**:
```csharp
public class LayerTensorSegment
{
    /// <summary>
    /// Azure Blob Storage URI for tensor segment payload.
    /// Format: https://{account}.blob.core.windows.net/tensors/{modelId}/{layerId}/{segmentOrdinal}.bin
    /// </summary>
    public string? BlobUri { get; set; }
    
    /// <summary>
    /// Blob ETag for optimistic concurrency.
    /// </summary>
    public string? BlobETag { get; set; }
    
    /// <summary>
    /// Cached blob size in bytes (same as PointCount * bytes per point).
    /// </summary>
    public long BlobSizeBytes { get; set; }
}
```

**Migration**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "RawPayload",
        table: "LayerTensorSegments");
        
    migrationBuilder.DropColumn(
        name: "PayloadRowGuid",
        table: "LayerTensorSegments");
        
    migrationBuilder.AddColumn<string>(
        name: "BlobUri",
        table: "LayerTensorSegments",
        type: "nvarchar(2048)",
        maxLength: 2048,
        nullable: true);
        
    migrationBuilder.AddColumn<string>(
        name: "BlobETag",
        table: "LayerTensorSegments",
        type: "nvarchar(128)",
        maxLength: 128,
        nullable: true);
        
    migrationBuilder.AddColumn<long>(
        name: "BlobSizeBytes",
        table: "LayerTensorSegments",
        type: "bigint",
        nullable: false,
        defaultValue: 0L);
}
```

---

## Implementation: Blob Storage Service

### 1. Create BlobStorageService

```csharp
// src/Hartonomous.Infrastructure/Services/BlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;

namespace Hartonomous.Infrastructure.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Upload bytes to blob storage and return blob URI.
    /// </summary>
    Task<BlobUploadResult> UploadAsync(
        string containerName,
        string blobName,
        byte[] content,
        string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload stream to blob storage and return blob URI.
    /// </summary>
    Task<BlobUploadResult> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download blob content as byte array.
    /// </summary>
    Task<byte[]> DownloadBytesAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download blob content as stream.
    /// </summary>
    Task<Stream> DownloadStreamAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if blob exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete blob.
    /// </summary>
    Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get blob properties (size, ETag, last modified).
    /// </summary>
    Task<BlobProperties> GetPropertiesAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}

public record BlobUploadResult(
    string BlobUri,
    string ETag,
    long SizeBytes,
    DateTimeOffset LastModified);

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    
    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<BlobUploadResult> UploadAsync(
        string containerName,
        string blobName,
        byte[] content,
        string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        return await UploadAsync(containerName, blobName, stream, contentType, metadata, cancellationToken);
    }
    
    public async Task<BlobUploadResult> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Metadata = metadata
        };
        
        var response = await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        
        _logger.LogInformation(
            "Uploaded blob {BlobName} to container {Container} ({SizeBytes} bytes)",
            blobName,
            containerName,
            properties.Value.ContentLength);
        
        return new BlobUploadResult(
            BlobUri: blobClient.Uri.ToString(),
            ETag: response.Value.ETag.ToString(),
            SizeBytes: properties.Value.ContentLength,
            LastModified: properties.Value.LastModified);
    }
    
    public async Task<byte[]> DownloadBytesAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToArray();
    }
    
    public async Task<Stream> DownloadStreamAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
    
    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        return await blobClient.ExistsAsync(cancellationToken);
    }
    
    public async Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return response.Value;
    }
    
    public async Task<BlobProperties> GetPropertiesAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        return response.Value;
    }
}
```

### 2. Register Service in DI

```csharp
// src/Hartonomous.Infrastructure/DependencyInjection.cs
services.AddSingleton<IBlobStorageService, BlobStorageService>();
```

---

## Migration Path: FILESTREAM → Blob Storage

### Phase 1: Migrate Model Files (Week 1)

**Step 1: Upload models to Blob Storage**

```powershell
# Using Azure CLI
az storage blob upload-batch \
    --account-name hartonomousstorage \
    --destination models \
    --source "D:\Models\blobs\" \
    --auth-mode login

# Or using AzCopy
azcopy copy "D:\Models\blobs\*" "https://hartonomousstorage.blob.core.windows.net/models/" --recursive
```

**Step 2: Update Atom records with Blob URIs**

```sql
-- Update PayloadLocator to BlobUri
UPDATE dbo.Atoms
SET 
    BlobUri = 'https://hartonomousstorage.blob.core.windows.net/models/' + 
              SUBSTRING(PayloadLocator, CHARINDEX('sha256-', PayloadLocator), LEN(PayloadLocator)),
    BlobContainer = 'models',
    BlobName = SUBSTRING(PayloadLocator, CHARINDEX('sha256-', PayloadLocator), LEN(PayloadLocator))
WHERE PayloadLocator LIKE 'D:\Models\blobs\%';

-- Verify
SELECT 
    AtomId,
    PayloadLocator AS OldPath,
    BlobUri AS NewBlobUri,
    BlobContainer,
    BlobName
FROM dbo.Atoms
WHERE Modality = 'model';
```

### Phase 2: Migrate Tensor Segments (Week 2)

**Step 1: Extract tensor segments from SQL to Blob Storage**

```csharp
// MigrateTensorSegmentsToBlobStorage.cs
public class TensorSegmentMigrationService
{
    private readonly HartonomousDbContext _dbContext;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<TensorSegmentMigrationService> _logger;
    
    public async Task MigrateAllSegmentsAsync(CancellationToken cancellationToken = default)
    {
        const int batchSize = 100;
        var migratedCount = 0;
        
        var totalSegments = await _dbContext.LayerTensorSegments.CountAsync(cancellationToken);
        _logger.LogInformation("Migrating {TotalSegments} tensor segments to blob storage", totalSegments);
        
        var segments = _dbContext.LayerTensorSegments
            .Include(s => s.Layer)
            .ThenInclude(l => l.Model)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);
        
        await foreach (var segment in segments)
        {
            try
            {
                // Generate blob name: tensors/{modelId}/{layerId}/{segmentOrdinal}.bin
                var blobName = $"{segment.Layer.ModelId}/{segment.LayerId}/{segment.SegmentOrdinal}.bin";
                
                // Upload RawPayload to blob storage
                var uploadResult = await _blobStorage.UploadAsync(
                    containerName: "tensors",
                    blobName: blobName,
                    content: segment.RawPayload,
                    contentType: "application/octet-stream",
                    metadata: new Dictionary<string, string>
                    {
                        ["ModelId"] = segment.Layer.ModelId.ToString(),
                        ["LayerId"] = segment.LayerId.ToString(),
                        ["SegmentOrdinal"] = segment.SegmentOrdinal.ToString(),
                        ["QuantizationType"] = segment.QuantizationType
                    },
                    cancellationToken: cancellationToken);
                
                // Update segment with blob URI
                segment.BlobUri = uploadResult.BlobUri;
                segment.BlobETag = uploadResult.ETag;
                segment.BlobSizeBytes = uploadResult.SizeBytes;
                
                // Clear RawPayload to free memory (will be removed in next migration)
                segment.RawPayload = Array.Empty<byte>();
                
                migratedCount++;
                
                if (migratedCount % batchSize == 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Migrated {Count}/{Total} segments", migratedCount, totalSegments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating segment {SegmentId}", segment.LayerTensorSegmentId);
            }
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Migration complete: {Count} segments migrated", migratedCount);
    }
}
```

**Step 2: Remove RawPayload column**

```csharp
// Add migration after all segments migrated
dotnet ef migrations add RemoveRawPayloadColumn

// Migration.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "RawPayload",
        table: "LayerTensorSegments");
        
    migrationBuilder.DropColumn(
        name: "PayloadRowGuid",
        table: "LayerTensorSegments");
}
```

---

## Cost Analysis: FILESTREAM vs Blob Storage

### SQL Server on VM with FILESTREAM

**Monthly Cost** (Standard_E8s_v5 + 3.6 TB Premium SSD):
- VM Compute: $400/month
- Storage (OS + Data + FILESTREAM + Memory): $515/month
- **Total**: $915/month (before optimization)

### Azure SQL Database + Blob Storage

**Azure SQL Database Premium P2** (250 DTUs, 250 GB):
- Monthly Cost: ~$950/month
- Includes: In-Memory OLTP, Columnstore, Spatial, Temporal Tables, Query Store
- **Service Broker**: Limited (single-database scope)

**Azure Blob Storage** (100 GB Cool tier):
- Storage: 100 GB × $0.01/GB/month = $1/month
- Operations: ~$5-10/month (read/write)
- **Total Blob**: ~$11/month

**Combined**: $950 + $11 = **$961/month**

### Verdict

**Cost is similar**, but Azure SQL Database offers:
- ✅ Fully managed (no OS patching, no SQL Server updates)
- ✅ Built-in high availability (99.99% SLA)
- ✅ Automated backups (point-in-time restore)
- ✅ Automatic tuning and performance recommendations
- ✅ Threat detection and vulnerability assessment
- ❌ Service Broker limited to single database (requires workaround)

---

## Service Broker Alternative: Azure Storage Queues

### Problem: Azure SQL Database Service Broker Limitation

Azure SQL Database supports Service Broker BUT:
- **Single-database scope only** (no cross-database messaging)
- **No cross-instance messaging** (can't send to external SQL Server)
- Works for internal async processing within same database

### Solution: Replace Service Broker with Azure Storage Queues

**Why Storage Queues**:
- Already using Azure Storage (Blob + Queue in Program.cs)
- $0.0004 per 10,000 operations (negligible cost)
- DefaultAzureCredential already configured
- Simple message-based API
- At-least-once delivery guarantee
- TTL and visibility timeout support

**Architecture**:
```
Hartonomous.Api → Storage Queue → Neo4jSync Worker
```

**Code Changes**:

1. **Replace SqlMessageBroker with StorageQueueMessageBroker**:

```csharp
// src/Hartonomous.Infrastructure/Services/Messaging/StorageQueueMessageBroker.cs
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

public class StorageQueueMessageBroker : IMessageBroker
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<StorageQueueMessageBroker> _logger;
    private readonly string _queueName;
    
    public StorageQueueMessageBroker(
        QueueServiceClient queueServiceClient,
        IJsonSerializer serializer,
        IOptions<MessageBrokerOptions> options,
        ILogger<StorageQueueMessageBroker> logger)
    {
        _queueServiceClient = queueServiceClient;
        _serializer = serializer;
        _logger = logger;
        _queueName = options.Value.QueueName ?? "neo4j-sync";
    }
    
    public async Task PublishAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        where TPayload : class
    {
        var queueClient = _queueServiceClient.GetQueueClient(_queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        
        var body = _serializer.Serialize(payload);
        var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(body));
        
        await queueClient.SendMessageAsync(base64Body, cancellationToken: cancellationToken);
        
        _logger.LogDebug("Published payload type {PayloadType} to queue {Queue}", 
            typeof(TPayload).Name, _queueName);
    }
    
    public async Task<BrokeredMessage?> ReceiveAsync(TimeSpan waitTime, CancellationToken cancellationToken = default)
    {
        var queueClient = _queueServiceClient.GetQueueClient(_queueName);
        
        var response = await queueClient.ReceiveMessageAsync(
            visibilityTimeout: waitTime,
            cancellationToken: cancellationToken);
        
        if (response.Value == null)
            return null;
        
        var message = response.Value;
        var body = Encoding.UTF8.GetString(Convert.FromBase64String(message.Body.ToString()));
        
        return new BrokeredMessage(
            conversationHandle: Guid.NewGuid(), // Not used in Storage Queues
            messageTypeName: "StorageQueueMessage",
            body: body,
            enqueuedTimeUtc: message.InsertedOn?.UtcDateTime ?? DateTime.UtcNow,
            completeAsync: async ct =>
            {
                await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            },
            abandonAsync: async ct =>
            {
                // Update visibility timeout to make message available immediately
                await queueClient.UpdateMessageAsync(
                    message.MessageId,
                    message.PopReceipt,
                    visibilityTimeout: TimeSpan.Zero,
                    cancellationToken: ct);
            },
            serializer: _serializer);
    }
}
```

2. **Register in DI**:

```csharp
// When using Azure SQL Database
builder.Services.AddSingleton<IMessageBroker, StorageQueueMessageBroker>();

// When using SQL Server on VM
builder.Services.AddSingleton<IMessageBroker, SqlMessageBroker>();
```

**Cost Impact**:
- Azure Storage Queue operations: ~$0.0004 per 10,000 operations
- Estimated 100,000 messages/month = $0.40/month
- **Negligible cost increase**

---

## Final Recommendation: Hybrid Approach

### Option A: Azure SQL Database + Blob Storage + Storage Queues ⭐ **RECOMMENDED**

**Configuration**:
- **Azure SQL Database Premium P2**: $950/month
- **Azure Blob Storage (Cool tier)**: $11/month
- **Azure Storage Queues**: $0.40/month
- **Total**: ~$961/month

**Pros**:
- ✅ Fully managed (no VM management, no OS patching)
- ✅ All SQL Server features except CLR UNSAFE
- ✅ Built-in HA, backups, threat detection
- ✅ Automatic performance tuning
- ✅ Scalable blob storage (unlimited)
- ✅ Works with existing Azure Storage integration

**Cons**:
- ❌ No CLR UNSAFE (file I/O, shell execution)
- ❌ Service Broker replaced with Storage Queues
- ⚠️ Requires code changes for messaging

**CLR UNSAFE Workaround**:
- Move file I/O operations to Azure Functions or Logic Apps
- Use Azure Automation for shell command execution
- Most CLR functions are vector math (SAFE, works in Azure SQL Database)

### Option B: SQL Server on VM (Windows) 

**Configuration**:
- **Standard_E8s_v5 + Premium SSD**: $915/month (before optimization)
- **After optimization**: $300-500/month

**Pros**:
- ✅ Full SQL Server 2025 feature set
- ✅ FILESTREAM, CLR UNSAFE, Service Broker
- ✅ No code changes required

**Cons**:
- ❌ Manual VM management (OS patching, SQL updates)
- ❌ Manual HA configuration (SQL Always On AG)
- ❌ Manual backup management
- ❌ Limited storage scalability

---

## Implementation Checklist

### Phase 1: Blob Storage Migration (Week 1)

- [ ] Create blob containers: `models`, `atoms`, `tensors`
- [ ] Upload model files to `models` container
- [ ] Add `BlobUri`, `BlobContainer`, `BlobName` columns to `Atoms` table
- [ ] Update existing Atom records with blob URIs
- [ ] Test blob download via `IBlobStorageService`

### Phase 2: Tensor Segment Migration (Week 2)

- [ ] Implement `IBlobStorageService`
- [ ] Create `TensorSegmentMigrationService`
- [ ] Migrate tensor segments to blob storage
- [ ] Add `BlobUri`, `BlobETag`, `BlobSizeBytes` columns to `LayerTensorSegments`
- [ ] Remove `RawPayload` and `PayloadRowGuid` columns

### Phase 3: Service Broker Replacement (Week 3)

- [ ] Implement `StorageQueueMessageBroker`
- [ ] Update Neo4jSync worker to consume from Storage Queue
- [ ] Test end-to-end message flow
- [ ] Remove Service Broker configuration (queues, services, contracts)

### Phase 4: Deploy to Azure SQL Database (Week 4)

- [ ] Provision Azure SQL Database Premium P2
- [ ] Run EF Core migrations
- [ ] Deploy CLR SAFE assemblies (vector math only)
- [ ] Verify In-Memory OLTP, Columnstore, Spatial, Temporal Tables
- [ ] Load test with blob storage integration

---

## Conclusion

**FILESTREAM is NOT required!** By using **Azure Blob Storage** for large payloads and **Azure Storage Queues** for messaging, we can:

1. ✅ Use **Azure SQL Database** (fully managed, no VM)
2. ✅ Eliminate FILESTREAM dependency
3. ✅ Replace Service Broker with Storage Queues (minimal code changes)
4. ✅ Keep 95% of SQL Server features (In-Memory OLTP, Columnstore, Spatial, Temporal Tables, Query Store)
5. ✅ Similar cost (~$961/month vs $915/month)
6. ✅ Better scalability (unlimited blob storage)

**Next Steps**: Proceed with Blob Storage migration and Storage Queue implementation?
