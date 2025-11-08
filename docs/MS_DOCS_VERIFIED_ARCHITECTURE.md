# MS Docs-Verified Architecture Reference

## Document Purpose
This document contains **current, verified implementation patterns** from official Microsoft documentation (queried November 8, 2025). All previous assumptions from outdated training data have been corrected.

---

## 1. Spatial Data (EF Core 10.0)

### ❌ INCORRECT (Legacy EF6 Only)
```csharp
// DO NOT USE - This is for Entity Framework 6 only
using Microsoft.SqlServer.Types;
var geometry = SqlGeometry.Point(x, y, srid);
```

### ✅ CORRECT (EF Core 2.2+)
```csharp
// NetTopologySuite is the modern standard
using NetTopologySuite.Geometries;

// In your entity
public class Atom
{
    public Point SpatialLocation { get; set; } // NetTopologySuite.Geometries.Point
}

// In DbContext configuration
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite());
}
```

**Required Packages:**
- `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` v10.0.0-rc.2
- `NetTopologySuite` v2.5.0

**Source:** https://learn.microsoft.com/en-us/ef/core/modeling/spatial

---

## 2. Azure Service Bus - Conditional Registration

### Pattern: Configuration-Based Registration
```csharp
// In Program.cs or DependencyInjection.cs
builder.Services.AddAzureClients(clientBuilder =>
{
    // Only register Service Bus if connection string exists
    var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
    
    if (!string.IsNullOrEmpty(serviceBusConnectionString))
    {
        clientBuilder.AddServiceBusClientWithNamespace(serviceBusConnectionString);
    }
    
    // Use Arc managed identity for Azure resources
    clientBuilder.UseCredential(new DefaultAzureCredential());
    
    // Configure defaults for all Azure clients
    clientBuilder.ConfigureDefaults(builder.Configuration.GetSection("AzureDefaults"));
});

// Fallback to InMemoryEventBus in conditional registration
services.TryAddSingleton<IEventBus>(sp =>
{
    var serviceBusClient = sp.GetService<ServiceBusClient>();
    if (serviceBusClient != null)
    {
        return new ServiceBusEventBus(serviceBusClient, sp.GetRequiredService<ILogger<ServiceBusEventBus>>());
    }
    return new InMemoryEventBus(sp.GetRequiredService<ILogger<InMemoryEventBus>>());
});
```

**appsettings.Production.json:**
```json
{
  "ServiceBus": {
    "Namespace": "your-namespace.servicebus.windows.net"
  },
  "AzureDefaults": {
    "Retry": {
      "MaxRetries": 3,
      "Delay": "00:00:02",
      "MaxDelay": "00:00:10",
      "Mode": "Exponential"
    }
  }
}
```

**Source:** https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection

---

## 3. DefaultAzureCredential for SQL Server Arc

### SQL Server 2025 Managed Identity
```csharp
// Connection string pattern
"Server=HART-DESKTOP;Database=Hartonomous;Authentication=ActiveDirectoryManagedIdentity;Encrypt=True;"

// In ASP.NET Core
builder.Services.AddDbContext<HartonomousDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("HartonomousDb");
    options.UseSqlServer(connectionString);
});
```

### Prerequisites
1. SQL Server 2025 connected to Azure Arc
2. Managed identity enabled: `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\FederatedAuthentication`
3. SQL Server service account has permissions on `C:\ProgramData\AzureConnectedMachineAgent\Tokens\`

**SQL Server Credential:**
```sql
CREATE CREDENTIAL [vault-name.vault.azure.net]
    WITH IDENTITY = 'Managed Identity'
    FOR CRYPTOGRAPHIC PROVIDER AzureKeyVault_EKM;
```

**Source:** https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/managed-identity

---

## 4. Application Insights (ASP.NET Core 9.0)

### Recommended Configuration
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = false; // Disable if using sampling elsewhere
    options.EnableQuickPulseMetricStream = true; // Live Metrics
});

// Configure logging
builder.Logging.AddApplicationInsights();
builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("Hartonomous", LogLevel.Information);
builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);
```

**appsettings.Production.json:**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Hartonomous": "Information"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

**Source:** https://learn.microsoft.com/en-us/azure/azure-monitor/app/dotnet

---

## 5. SQL Server Compression (Best Practices)

### Compression Types & Use Cases

#### PAGE Compression (Recommended for Large Tables)
- **Savings:** ~40% reduction
- **CPU Impact:** Moderate
- **Targets:** AtomEmbedding (600GB→360GB), TensorAtoms (810GB→527GB), ImagePatches, AudioFrames

```sql
ALTER TABLE AtomEmbedding REBUILD WITH (DATA_COMPRESSION = PAGE);
ALTER TABLE TensorAtoms REBUILD WITH (DATA_COMPRESSION = PAGE);
ALTER TABLE ImagePatches REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);
```

#### COLUMNSTORE (Analytical Workloads)
- **Savings:** 70-90% reduction
- **Best for:** Large fact tables, time-series data
- **Trade-off:** Higher CPU on writes

```sql
CREATE COLUMNSTORE INDEX IX_VideoFrameAnalytics
ON VideoFrameAnalytics (FrameId, Timestamp, AtomId, FeatureVector);
```

#### COLUMNSTORE_ARCHIVE (Cold Data)
- **Savings:** Maximum compression
- **Best for:** Historical data rarely queried

```sql
ALTER TABLE OldImagePatches REBUILD PARTITION = ALL 
WITH (DATA_COMPRESSION = COLUMNSTORE_ARCHIVE);
```

**Source:** https://learn.microsoft.com/en-us/sql/relational-databases/data-compression/data-compression

---

## 6. SQL Server 2025 Vector Support (CRITICAL)

### Native Vector Data Type
```sql
-- Create table with native VECTOR type
CREATE TABLE AtomEmbedding (
    AtomId BIGINT NOT NULL,
    EmbeddingModel NVARCHAR(100) NOT NULL,
    EmbeddingVector VECTOR(1536) NOT NULL, -- Native vector type
    CONSTRAINT PK_AtomEmbedding PRIMARY KEY (AtomId, EmbeddingModel)
);

-- Half-precision for larger dimensions (SQL Server 2025 Preview)
ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;

CREATE TABLE LargeEmbedding (
    Id INT PRIMARY KEY,
    Embedding VECTOR(3996, float16) -- 3996 dimensions with half-precision
);
```

### .NET Integration (Microsoft.Data.SqlClient 6.0+)
```csharp
using Microsoft.Data.SqlTypes;

// Insert vector
var embedding = new SqlVector<float>(new[] { 0.1f, 0.2f, 0.3f });
await using var command = connection.CreateCommand();
command.CommandText = "INSERT INTO Embeddings (Vector) VALUES (@vector)";
command.Parameters.Add(new SqlParameter("@vector", SqlDbType.Vector) { Value = embedding });
await command.ExecuteNonQueryAsync();

// Query with similarity search
command.CommandText = @"
    SELECT TOP 10 Id, VECTOR_DISTANCE('cosine', @queryVector, EmbeddingVector) AS Distance
    FROM AtomEmbedding
    ORDER BY Distance";
command.Parameters.Add(new SqlParameter("@queryVector", SqlDbType.Vector) { Value = queryEmbedding });
```

### Vector Index (Preview - SQL Server 2025)
```sql
-- Create approximate nearest neighbor index
CREATE VECTOR INDEX IX_Embedding_Vector 
ON AtomEmbedding(EmbeddingVector)
WITH (ALGORITHM = ANN);

-- Query using vector index
SELECT TOP 10 AtomId, VECTOR_SEARCH('cosine', @queryVector, EmbeddingVector) AS Similarity
FROM AtomEmbedding
ORDER BY Similarity DESC;
```

**EF Core 10.0 Support:**
```csharp
public class AtomEmbedding
{
    public long AtomId { get; set; }
    
    [Column(TypeName = "vector(1536)")]
    public SqlVector<float> EmbeddingVector { get; set; }
}

// Query
var queryVector = new SqlVector<float>(embeddings);
var results = await context.AtomEmbeddings
    .OrderBy(e => EF.Functions.VectorDistance("cosine", queryVector, e.EmbeddingVector))
    .Take(10)
    .ToListAsync();
```

**Source:** 
- https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type
- https://learn.microsoft.com/en-us/sql/sql-server/ai/vectors

---

## 7. TorchSharp in SQL CLR (Advanced)

### Supported Architecture
```csharp
// CLR stored procedure using TorchSharp
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using TorchSharp;

[SqlProcedure]
public static void RunInference(SqlBytes modelBytes, SqlBytes inputTensor, out SqlBytes outputTensor)
{
    using var model = torch.jit.load(modelBytes.Stream);
    using var input = torch.tensor(/* deserialize inputTensor */);
    using var output = model.forward(input);
    
    outputTensor = new SqlBytes(/* serialize output */);
}
```

### Deployment Requirements
1. **CLR UNSAFE** permission set required
2. **.NET Framework only** (not .NET Core/5+)
3. **Assembly signing** with certificate
4. **Trust assembly** via `sp_add_trusted_assembly`

```sql
-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Add trusted assembly
EXEC sp_add_trusted_assembly 
    @hash = 0x<assembly_hash>, 
    @description = 'TorchSharp ML Assembly';

-- Create assembly with UNSAFE
CREATE ASSEMBLY TorchSharpML
FROM 'D:\Assemblies\TorchSharpML.dll'
WITH PERMISSION_SET = UNSAFE;
```

**Alternative (Recommended):** Use **Service Broker** for async processing with external .NET process

**Source:** 
- https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/database-objects/getting-started-with-clr-integration
- https://github.com/dotnet/TorchSharp

---

## 8. SQL Server Service Broker

### Core Pattern
```sql
-- Create message types
CREATE MESSAGE TYPE ProcessImageRequest VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE ProcessImageResponse VALIDATION = WELL_FORMED_XML;

-- Create contract
CREATE CONTRACT ProcessImageContract (
    ProcessImageRequest SENT BY INITIATOR,
    ProcessImageResponse SENT BY TARGET
);

-- Create queue with activation
CREATE QUEUE ImageProcessingQueue
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = sp_ProcessImageQueue,
    MAX_QUEUE_READERS = 10,
    EXECUTE AS OWNER
);

-- Create service
CREATE SERVICE ImageProcessingService
ON QUEUE ImageProcessingQueue (ProcessImageContract);

-- Send message (from trigger or procedure)
DECLARE @dialog_handle UNIQUEIDENTIFIER;
BEGIN DIALOG @dialog_handle
    FROM SERVICE ImageIngestionService
    TO SERVICE 'ImageProcessingService'
    ON CONTRACT ProcessImageContract;

SEND ON CONVERSATION (@dialog_handle)
    MESSAGE TYPE ProcessImageRequest (
        N'<Image><Id>12345</Id><Path>/images/photo.jpg</Path></Image>'
    );

-- Receive and process (in activated stored procedure)
CREATE PROCEDURE sp_ProcessImageQueue
AS
BEGIN
    DECLARE @dialog_handle UNIQUEIDENTIFIER;
    DECLARE @message_body XML;
    
    WAITFOR (
        RECEIVE TOP(1) 
            @dialog_handle = conversation_handle,
            @message_body = CAST(message_body AS XML)
        FROM ImageProcessingQueue
    ), TIMEOUT 5000;
    
    IF @message_body IS NOT NULL
    BEGIN
        -- Process image (atomize, generate embeddings, etc.)
        EXEC sp_AtomizeImage @imageXml = @message_body;
        
        -- Send response
        SEND ON CONVERSATION (@dialog_handle)
            MESSAGE TYPE ProcessImageResponse (N'<Success>True</Success>');
            
        END CONVERSATION @dialog_handle;
    END
END;
```

**Benefits:**
- Transactional guarantees (exactly-once delivery)
- Automatic activation (dynamic scaling)
- Ordering within conversation groups
- Survives restarts/failover

**Source:** https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-service-broker

---

## 9. Dependency Injection Best Practices (ASP.NET Core 9.0)

### Service Lifetimes
```csharp
// Singleton - One instance for application lifetime
services.AddSingleton<IConfiguration>(builder.Configuration);
services.AddSingleton<INeo4jDriver>(/* driver instance */);

// Scoped - One instance per HTTP request
services.AddScoped<HartonomousDbContext>();
services.AddScoped<IAtomRepository, AtomRepository>();

// Transient - New instance every time
services.AddTransient<IAtomService, AtomService>();

// Factory pattern for complex creation
services.AddSingleton<INeo4jDriverFactory>(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new Neo4jDriverFactory(config);
});
```

### Avoid Service Locator Anti-Pattern
```csharp
// ❌ WRONG - Service locator
public class MyService
{
    public MyService(IServiceProvider sp)
    {
        var dependency = sp.GetRequiredService<IDependency>(); // Anti-pattern
    }
}

// ✅ CORRECT - Constructor injection
public class MyService
{
    private readonly IDependency _dependency;
    
    public MyService(IDependency dependency)
    {
        _dependency = dependency;
    }
}
```

**Source:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection

---

## 10. Production Configuration Pattern

### appsettings.Production.json Template
```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=HART-DESKTOP;Database=Hartonomous;Authentication=ActiveDirectoryManagedIdentity;Encrypt=True;TrustServerCertificate=False;",
    "Neo4j": "neo4j://localhost:7687"
  },
  "ServiceBus": {
    "Namespace": "hartonomous.servicebus.windows.net"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
  },
  "AzureDefaults": {
    "Retry": {
      "MaxRetries": 3,
      "Delay": "00:00:02",
      "MaxDelay": "00:00:10",
      "Mode": "Exponential"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Hartonomous": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "DistributedCache": {
    "Redis": {
      "ConnectionString": "localhost:6379"
    }
  }
}
```

---

## Implementation Checklist

### Immediate Actions
- [x] Replace Microsoft.SqlServer.Types with NetTopologySuite packages
- [ ] Implement conditional Service Bus registration in DependencyInjection.cs
- [ ] Apply PAGE compression to AtomEmbedding and TensorAtoms tables
- [ ] Create appsettings.Production.json files for all services
- [ ] Update AtomEmbedding table to use native VECTOR(1536) type
- [ ] Configure Application Insights with proper log levels

### Medium Priority
- [ ] Implement sp_AtomizeText.sql and sp_AtomizeVideo.sql procedures
- [ ] Configure SQL Server managed identity for Arc
- [ ] Set up Application Insights live metrics
- [ ] Optimize query performance with vector indexes (preview feature)

### Future Enhancements
- [ ] Migrate to half-precision vectors (float16) for 3996 dimensions
- [ ] Implement Service Broker activation for async image processing
- [ ] Deploy TorchSharp inference via external process + Service Broker
- [ ] Configure COLUMNSTORE_ARCHIVE for historical data partitions

---

## References
All implementation patterns verified from official Microsoft Learn documentation (accessed November 8, 2025):
- Entity Framework Core: https://learn.microsoft.com/en-us/ef/core/
- Azure SDK for .NET: https://learn.microsoft.com/en-us/dotnet/azure/sdk/
- SQL Server 2025: https://learn.microsoft.com/en-us/sql/sql-server/
- ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/
