# SQL Server 2025 Requirements Analysis - Hartonomous Project

## Executive Summary

This is a **comprehensive deep-dive analysis** of the SQL Server requirements for the Hartonomous project. The system requires **Windows-based SQL Server 2025** with multiple enterprise features that are **NOT available in Azure SQL Database**. This analysis was conducted after investigating the complete codebase (94 SQL files, 896 C# files, CLR assemblies, Service Broker implementation, etc.).

---

## Critical Finding: Azure SQL Database is NOT Compatible

**IMPORTANT**: This project **CANNOT** be deployed to Azure SQL Database because it requires the following Windows-only features:

1. **FILESTREAM** (Windows file system integration)
2. **CLR Assemblies with UNSAFE permission** (file I/O, shell execution)
3. **SQL Server Service Broker** (built-in messaging)
4. **In-Memory OLTP** (memory-optimized tables)
5. **Spatial Data Types** (GEOMETRY, GEOGRAPHY)
6. **Columnstore Indexes** (analytical performance)
7. **Temporal Tables** (system-versioned history)
8. **Query Store** (query performance tracking)

---

## SQL Server Features Inventory

### 1. FILESTREAM (Windows File System Integration)

**Purpose**: Store large binary model files (62.81 GB Llama4, 17.28 GB Qwen3-Coder) on disk with transactional consistency.

**Implementation Details**:
- **File**: `sql/Setup_FILESTREAM.sql`
- **Table**: `dbo.Atoms` with `Payload VARBINARY(MAX) FILESTREAM`
- **Filegroup**: `HartonomousFileStream` (type 'FD')
- **Path**: `D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\HartonomousFileStream`
- **Migration Procedure**: `sp_MigratePayloadLocatorToFileStream` (uses CLR function `dbo.clr_ReadFileBytes`)

**Why FILESTREAM**:
- Model files are too large to store as standard VARBINARY(MAX) in database
- FILESTREAM provides:
  - Transactional consistency (ACID guarantees)
  - File system performance for large BLOBs
  - Streaming access via T-SQL or Win32 API
  - Backup/restore integration

**Azure SQL Database**: **NOT SUPPORTED** ❌

**Deployment Impact**: Requires Windows SQL Server with FILESTREAM enabled at instance level via SQL Server Configuration Manager.

---

### 2. CLR Assemblies with UNSAFE Permission

**Purpose**: Execute file I/O, shell commands, and advanced vector mathematics beyond T-SQL capabilities.

**CLR Projects**: 27 C# files in `src/SqlClr/`:
- `VectorAggregates.cs` - Streaming vector statistics (mean, variance, geometric median, softmax)
- `AdvancedVectorAggregates.cs` - Advanced vector operations
- `FileSystemFunctions.cs` - **UNSAFE**: File I/O (read/write), shell command execution, directory operations
- `AtomicStream.cs`, `ComponentStream.cs` - Custom UDT streaming types
- `SpatialOperations.cs` - 3D spatial calculations
- `ImageProcessing.cs`, `AudioProcessing.cs` - Media processing
- `GraphVectorAggregates.cs`, `NeuralVectorAggregates.cs`, `TimeSeriesVectorAggregates.cs`
- `AnomalyDetectionAggregates.cs`, `BehavioralAggregates.cs`, `ReasoningFrameworkAggregates.cs`
- `RecommenderAggregates.cs`, `ResearchToolAggregates.cs`, `SemanticAnalysis.cs`

**Critical CLR Functions (UNSAFE permission required)**:
```csharp
// FileSystemFunctions.cs
SqlInt64 WriteFileBytes(SqlString filePath, SqlBytes content)
SqlInt64 WriteFileText(SqlString filePath, SqlString content)
SqlBytes ReadFileBytes(SqlString filePath)
SqlString ReadFileText(SqlString filePath)
IEnumerable ExecuteShellCommand(SqlString command, SqlString workingDirectory, SqlInt32 timeoutSeconds)
SqlBoolean FileExists(SqlString filePath)
SqlBoolean DirectoryExists(SqlString directoryPath)
SqlBoolean DeleteFile(SqlString filePath)
```

**Deployment Script**: `scripts/deploy-clr-unsafe.sql`
- Drops all CLR functions, aggregates, types, and assemblies
- Creates assembly with `PERMISSION_SET = UNSAFE`
- Recreates types: `provenance.AtomicStream`, `provenance.ComponentStream`
- Assembly path: `d:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll`

**Why CLR with UNSAFE**:
- **Autonomous Deployment**: Write generated SQL code to disk, execute git commands (add, commit, push)
- **FILESTREAM Migration**: Read files from `PayloadLocator` paths into FILESTREAM columns
- **Vector Operations**: T-SQL cannot do streaming variance, geometric median, dimensionality reduction
- **File I/O**: Access model files on disk for on-demand loading (62.81 GB Llama4, 17.28 GB Qwen3-Coder)

**Azure SQL Database**: **NOT SUPPORTED** ❌
- Azure SQL Database allows SAFE CLR assemblies only
- UNSAFE permission (file I/O, network access, shell execution) is blocked

**Deployment Impact**: 
- Requires Windows SQL Server 
- Must enable CLR integration: `sp_configure 'clr enabled', 1`
- Requires TRUSTWORTHY database property or assembly signing with certificate

---

### 3. SQL Server Service Broker (Built-in Messaging)

**Purpose**: Asynchronous messaging for Neo4j graph synchronization, autonomous improvement events, and inference jobs.

**Implementation Details**:
- **Service**: `SqlMessageBroker.cs` (implements `IMessageBroker`)
- **Command Builder**: `ServiceBrokerCommandBuilder.cs` (generates BEGIN DIALOG, SEND ON CONVERSATION, RECEIVE)
- **Resilience**: `ServiceBrokerResilienceStrategy.cs`, `ServiceBrokerCommandBuilder.cs`
- **Dead Letter**: `SqlMessageDeadLetterSink.cs`
- **Consumer**: `Neo4jSync/Program.cs` hosts `ServiceBrokerMessagePump` background service

**Service Broker Components**:
```sql
-- BEGIN DIALOG CONVERSATION (publish message)
BEGIN DIALOG CONVERSATION @conversationHandle
    FROM SERVICE [InitiatorServiceName]
    TO SERVICE 'TargetServiceName'
    ON CONTRACT [ContractName]
    WITH ENCRYPTION = OFF, LIFETIME = @lifetimeSeconds;

SEND ON CONVERSATION @conversationHandle
    MESSAGE TYPE [MessageTypeName] (@messageBody);

END CONVERSATION @conversationHandle;

-- RECEIVE (consume message)
WAITFOR (
    RECEIVE TOP(1)
        conversation_handle,
        message_type_name,
        CAST(message_body AS NVARCHAR(MAX)) AS message_body,
        message_enqueue_time
    FROM [QueueName]
),
TIMEOUT @timeoutMs;
```

**Message Flow**:
1. **Hartonomous.Api** publishes `BrokeredMessage` via `SqlMessageBroker.PublishAsync<T>()`
2. Service Broker queues message with transactional durability
3. **Neo4jSync** worker polls queue via `SqlMessageBroker.ReceiveAsync()`
4. Event handlers process: `ModelEventHandler`, `InferenceEventHandler`, `KnowledgeEventHandler`, `GenericEventHandler`
5. Messages written to Neo4j graph via `ProvenanceGraphBuilder`

**Why Service Broker**:
- **No Azure Service Bus**: User explicitly clarified "We're using sql server service bus" (Service Broker, not Azure Service Bus)
- **Transactional messaging**: ACID guarantees with database operations
- **Built-in**: No external infrastructure (no Service Bus namespace, no connection strings)
- **Cost**: $0 (included with SQL Server license)
- **Latency**: Sub-millisecond message delivery (local database queue)

**Azure SQL Database**: **PARTIAL SUPPORT** ⚠️
- Service Broker is available in Azure SQL Database BUT:
  - Cannot communicate between instances (cross-database messaging disabled)
  - Limited to single database scope
  - No cross-server routing
  - Recommended to use Azure Service Bus instead (but user rejected this approach)

**Deployment Impact**: SQL Server Service Broker should work in Azure SQL Managed Instance, but NOT in Azure SQL Database (single-database limitation).

---

### 4. In-Memory OLTP (Memory-Optimized Tables)

**Purpose**: Eliminate latch contention for high-frequency billing insert workloads.

**Implementation Details**:
- **File**: `sql/tables/dbo.BillingUsageLedger_InMemory.sql`
- **Filegroup**: `HartonomousMemoryOptimized CONTAINS MEMORY_OPTIMIZED_DATA`
- **File Path**: `D:\Hartonomous\HartonomousMemoryOptimized`
- **Table**: `dbo.BillingUsageLedger_InMemory` with `MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA`
- **Indexes**: 
  - Hash index: `IX_TenantId_Hash HASH (TenantId) WITH (BUCKET_COUNT = 10000)`
  - Range index: `IX_Timestamp_Range NONCLUSTERED (TimestampUtc DESC)`

**Performance Benefits**:
- 10-30x faster inserts (no latch contention)
- Lock-free concurrency (optimistic multi-version concurrency control)
- Eliminates page latch waits for billing ledger

**Azure SQL Database**: **SUPPORTED** ✅
- Available in Premium and Business Critical tiers
- Requires sufficient memory allocation

**Deployment Impact**: Works in Azure SQL Database Premium/Business Critical, but requires memory allocation planning.

---

### 5. Spatial Data Types (GEOMETRY, GEOGRAPHY)

**Purpose**: 3D vector embeddings, spatial search, nearest-neighbor queries.

**Implementation Details**:
- **SQL**: `sql/procedures/Common.CreateSpatialIndexes.sql`, `sql/procedures/Graph.AtomSurface.sql`
- **Tables**: `dbo.AtomEmbeddings` with GEOMETRY columns for 3D spatial indexing
- **Spatial Indexes**: `CREATE SPATIAL INDEX` for fast KNN search
- **CLR**: `SpatialOperations.cs` for advanced 3D calculations

**Sample Data** (from test fixtures):
```sql
INSERT INTO dbo.AtomEmbeddings (TokenText, RepresentationVector, SpatialPoint3D)
VALUES 
    ('database', GEOMETRY::STGeomFromText('POINT (-3.1 4.2 -1.5)', 0)),
    ('server', GEOMETRY::STGeomFromText('POINT (1.0 2.0 3.0)', 0));
```

**Azure SQL Database**: **SUPPORTED** ✅

---

### 6. Columnstore Indexes (Analytical Performance)

**Purpose**: 5-10x compression and batch-mode execution for analytical queries.

**Implementation Details**:
- **File**: `sql/Optimize_ColumnstoreCompression.sql`
- **Tables**:
  - `dbo.BillingUsageLedger`: Nonclustered columnstore index `NCCI_BillingUsageLedger_Analytics`
  - `dbo.AutonomousImprovementHistory`: Nonclustered columnstore index `NCCI_AutonomousImprovementHistory_Analytics`
- **Compression**: ROW compression on OLTP tables, PAGE compression on archival tables

**Benefits**:
- 5-10x compression for analytical data
- Batch mode execution for columnar scans
- Reduced I/O for aggregate queries
- 20-40% space savings with row/page compression

**Azure SQL Database**: **SUPPORTED** ✅

---

### 7. Temporal Tables (System-Versioned History)

**Purpose**: Automatic model version tracking, point-in-time queries, instant rollback.

**Implementation Details**:
- **File**: `sql/Temporal_Tables_Evaluation.sql`
- **Candidate Tables**: `dbo.ModelLayers`, `dbo.TensorAtoms`
- **Design**:
  ```sql
  ALTER TABLE dbo.ModelLayers
  ADD 
      ValidFrom DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL,
      ValidTo DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL,
      PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

  ALTER TABLE dbo.ModelLayers
  SET (SYSTEM_VERSIONING = ON (
      HISTORY_TABLE = dbo.ModelLayersHistory,
      DATA_CONSISTENCY_CHECK = ON,
      HISTORY_RETENTION_PERIOD = 2 YEARS
  ));
  ```

**Use Cases**:
- Model drift analysis: Compare current weights to 30 days ago
- Audit trail: Track all weight updates for regulatory compliance
- Rollback: Restore model to previous state instantly
- Point-in-time queries: `FOR SYSTEM_TIME AS OF '2024-01-01'`

**Azure SQL Database**: **SUPPORTED** ✅

---

### 8. Query Store (Query Performance Tracking)

**Purpose**: Track query performance over time, identify regressions, force execution plans.

**Implementation Details**:
- **File**: `sql/EnableQueryStore.sql`
- **Configuration**:
  ```sql
  ALTER DATABASE Hartonomous
  SET QUERY_STORE = ON (
      OPERATION_MODE = READ_WRITE,
      QUERY_CAPTURE_MODE = AUTO,
      DATA_FLUSH_INTERVAL_SECONDS = 900,
      MAX_STORAGE_SIZE_MB = 1024,
      INTERVAL_LENGTH_MINUTES = 60
  );
  ```

**Azure SQL Database**: **SUPPORTED** ✅
- Enabled by default in Azure SQL Database

---

## SQL Server Service Broker Deep Dive

### Service Broker Configuration

**Configuration Options** (from `MessageBrokerOptions.cs`):
- `QueueName`: SQL queue name (e.g., `dbo.Neo4jSyncQueue`)
- `InitiatorServiceName`: Sender service (e.g., `HartonomousApi`)
- `TargetServiceName`: Receiver service (e.g., `Neo4jSyncService`)
- `ContractName`: Message contract (e.g., `//Hartonomous/EventContract`)
- `MessageTypeName`: Message type (e.g., `//Hartonomous/ModelEvent`)
- `ReceiveWaitTimeoutMilliseconds`: WAITFOR timeout (default 250ms)
- `ConversationLifetimeSeconds`: Dialog lifetime (default 60s)
- `MaxMessageCharacters`: Max message size (default 1M characters)

### Message Processing

**Publish Flow** (`SqlMessageBroker.PublishAsync<T>`):
1. Serialize payload to JSON via `IJsonSerializer`
2. Guard message length (max 1M characters)
3. Execute with resilience strategy (exponential backoff, circuit breaker)
4. Open SQL connection via `ISqlServerConnectionFactory`
5. Execute `BEGIN DIALOG` → `SEND ON CONVERSATION` → `END CONVERSATION`
6. Record telemetry (OpenTelemetry ActivitySource)

**Receive Flow** (`SqlMessageBroker.ReceiveAsync`):
1. Open SQL connection and begin transaction (ReadCommitted isolation)
2. Execute `WAITFOR RECEIVE` with timeout
3. Parse conversation handle, message type, body, enqueue time
4. Skip system messages (EndDialog, DialogTimer, Error)
5. Return `BrokeredMessage` with:
   - `CompleteAsync`: Commit transaction, END CONVERSATION
   - `AbandonAsync`: Rollback transaction (message reappears in queue)
   - `Deserialize<T>()`: Deserialize JSON body to type T

**Resilience Strategy** (`ServiceBrokerResilienceStrategy`):
- **Publish**: Exponential backoff (base 250ms, max 5s), circuit breaker (5 failures → 30s break)
- **Receive**: Exponential backoff (base 500ms, max 10s), circuit breaker (3 failures → 10s break)
- **Transient Error Detection**: `SqlServerTransientErrorDetector` (deadlocks, timeouts, network errors)

### Consumer Implementation (Neo4jSync)

**Background Worker** (`ServiceBrokerMessagePump`):
```csharp
// Neo4jSync/Program.cs
builder.Services.AddSingleton<IMessageBroker, SqlMessageBroker>();
builder.Services.AddSingleton<IBaseEventHandler, ModelEventHandler>();
builder.Services.AddSingleton<IBaseEventHandler, InferenceEventHandler>();
builder.Services.AddSingleton<IBaseEventHandler, KnowledgeEventHandler>();
builder.Services.AddSingleton<IBaseEventHandler, GenericEventHandler>();
builder.Services.AddSingleton<IMessageDispatcher, EventDispatcher>();
builder.Services.AddHostedService<ServiceBrokerMessagePump>();
```

**Message Pump Loop**:
1. Poll queue: `await _messageBroker.ReceiveAsync(TimeSpan.FromSeconds(5))`
2. Dispatch to handlers: `await _dispatcher.DispatchAsync(message)`
3. Handlers write to Neo4j: `ProvenanceGraphBuilder.CreateModelNode()`
4. Complete message: `await message.CompleteAsync()`
5. On error: `await message.AbandonAsync()` or `await _deadLetterSink.SendAsync()`

---

## Model Storage Strategy

### Large Model Files

**Current Approach** (`sql/Ingest_Models.sql`):
- **Llama4**: 62.81 GB (67,436,800,960 bytes)
  - Path: `D:\Models\blobs\sha256-9d507a36062c2845dd3bb3e93364e9abc1607118acd8650727a700f72fb126e5`
  - Capabilities: reasoning, analysis, generation, autonomous_improvement
- **Qwen3-Coder**: 17.28 GB (18,556,688,736 bytes)
  - Path: `D:\Models\blobs\sha256-1194192cf2a187eb02722edcc3f77b11d21f537048ce04b67ccf8ba78863006a`
  - Capabilities: code_generation, code_analysis, debugging, autonomous_code_gen

**Storage Design**:
- **dbo.Atoms.PayloadLocator**: Store file path (NVARCHAR(500))
- **dbo.Atoms.Metadata**: JSON with `file_path`, `size_gb`, `load_on_demand: 1`
- **On-Demand Loading**: Models loaded into memory only when needed for inference
- **FILESTREAM Migration**: Future migration to FILESTREAM for transactional consistency

**Why Not Store in Database**:
- 62.81 GB + 17.28 GB = 80.09 GB for 2 models
- Database size bloat
- Backup/restore overhead
- Better to reference files on disk and load on-demand

---

## Deployment Architecture

### Current State: Local Development

**Environment**: Windows laptop with:
- SQL Server 2025 (localhost)
- FILESTREAM enabled via SQL Server Configuration Manager
- CLR enabled (`sp_configure 'clr enabled', 1`)
- Service Broker enabled (default)
- Models stored in `D:\Models\blobs\`
- FILESTREAM path: `D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\HartonomousFileStream`
- Memory-optimized filegroup: `D:\Hartonomous\HartonomousMemoryOptimized`

### Deployment Options for Azure

#### Option 1: SQL Server on Azure VM (Windows) ⭐ **RECOMMENDED**

**Why**:
- ✅ **Full SQL Server 2025 feature set** (FILESTREAM, CLR UNSAFE, Service Broker, In-Memory OLTP, Spatial, Columnstore, Temporal, Query Store)
- ✅ **Windows file system** for model storage (`D:\Models\blobs\`)
- ✅ **Full control** over SQL Server Configuration Manager (FILESTREAM, CLR)
- ✅ **Azure Hybrid Benefit** (bring your own SQL Server license for significant savings)

**VM Sizing** (based on requirements):
- **Compute**: Standard_E8s_v5 (8 vCPUs, 64 GB RAM)
  - Rationale: In-Memory OLTP requires significant memory (billing ledger, embedding cache)
  - SQL Server + OS overhead: ~20 GB
  - Available for SQL Server: ~44 GB
- **Storage**: 
  - **OS Disk**: 128 GB Premium SSD (P10)
  - **Data Disk**: 1 TB Premium SSD (P30) for database files (MDF, LDF, NDF)
  - **FILESTREAM Disk**: 2 TB Premium SSD (P40) for FILESTREAM files (model storage, atom payloads)
  - **Memory-Optimized Disk**: 512 GB Premium SSD (P20) for In-Memory OLTP checkpoint files

**Estimated Monthly Cost** (East US region):
- **VM**: Standard_E8s_v5 (8 vCPUs, 64 GB RAM) = ~$400/month
- **Storage**: 
  - OS Disk (128 GB P10): $20/month
  - Data Disk (1 TB P30): $135/month
  - FILESTREAM Disk (2 TB P40): $270/month
  - Memory-Optimized Disk (512 GB P20): $90/month
- **Total**: ~$915/month

**Cost Optimization**:
- **Stop/Deallocate when not in use**: Pay only for storage (~$515/month)
- **Use Azure Hybrid Benefit**: Save up to 49% on VM compute
- **Reserved Instances**: Save 40-72% on VM compute with 1-year or 3-year commitment
- **After optimization**: ~$300-500/month (with stop/deallocate, hybrid benefit, reserved instance)

#### Option 2: Azure SQL Managed Instance

**Why**:
- ✅ **Most SQL Server features** (CLR SAFE only, Service Broker limited, In-Memory OLTP, Spatial, Columnstore, Temporal, Query Store)
- ❌ **NO FILESTREAM** (not supported)
- ❌ **NO CLR UNSAFE** (file I/O, shell execution blocked)
- ⚠️ **Service Broker limited** (cross-instance messaging available but complex)

**Sizing**: General Purpose (8 vCores, 64 GB RAM)

**Estimated Monthly Cost**: ~$1,800-2,400/month

**Verdict**: **NOT COMPATIBLE** due to FILESTREAM and CLR UNSAFE requirements.

#### Option 3: Azure SQL Database

**Why**:
- ❌ **NO FILESTREAM** (not supported)
- ❌ **NO CLR UNSAFE** (SAFE only)
- ❌ **Service Broker limited** (single database scope only, no cross-database messaging)
- ⚠️ **In-Memory OLTP** (Premium/Business Critical only)

**Verdict**: **NOT COMPATIBLE** due to FILESTREAM, CLR UNSAFE, and Service Broker limitations.

---

## Recommended Deployment Plan

### Phase 1: Provision SQL Server on Azure VM (Week 1)

1. **Create VM**:
   - Image: Windows Server 2022 with SQL Server 2025 Developer (or bring your own license)
   - Size: Standard_E8s_v5 (8 vCPUs, 64 GB RAM)
   - Region: East US (or same region as other resources)
   - Managed Disks:
     - OS: 128 GB Premium SSD (P10)
     - Data: 1 TB Premium SSD (P30)
     - FILESTREAM: 2 TB Premium SSD (P40)
     - Memory-Optimized: 512 GB Premium SSD (P20)

2. **Configure SQL Server**:
   - Enable FILESTREAM via SQL Server Configuration Manager
   - Enable CLR integration: `sp_configure 'clr enabled', 1`
   - Verify Service Broker enabled: `SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous'`
   - Create memory-optimized filegroup on Memory-Optimized disk

3. **Network Configuration**:
   - NSG: Allow inbound 1433 from App Service subnet
   - Private Endpoint: Create Private Endpoint for SQL Server in VNet
   - DNS: Configure Private DNS zone for private endpoint

4. **Backup Strategy**:
   - Azure Backup: Enable Azure VM backup (daily snapshots, 30-day retention)
   - SQL Server Backup: Automated backups to Azure Blob Storage
   - Transaction Log Backup: Every 15 minutes to Azure Blob Storage

### Phase 2: Deploy Database Schema (Week 2)

1. **Create Database**:
   ```powershell
   # Run deployment script with default FILESTREAM path
   .\scripts\deploy-database.ps1
   ```

2. **Deploy CLR Assemblies**:
   ```powershell
   # Build CLR project
   dotnet build src\SqlClr\SqlClrFunctions.csproj -c Release

   # Deploy assembly with UNSAFE permission
   sqlcmd -S <vm-sql-server> -d Hartonomous -i scripts\deploy-clr-unsafe.sql
   ```

3. **Execute Schema Scripts** (in order):
   - `sql/Setup_FILESTREAM.sql`
   - `sql/tables/*.sql`
   - `sql/types/*.sql`
   - `sql/procedures/*.sql`
   - `sql/EnableQueryStore.sql`
   - `sql/Optimize_ColumnstoreCompression.sql`

4. **Verify Features**:
   ```sql
   -- Verify FILESTREAM
   SELECT name, type_desc FROM sys.filegroups WHERE type = 'FD';

   -- Verify CLR assemblies
   SELECT name, permission_set_desc FROM sys.assemblies WHERE name = 'SqlClrFunctions';

   -- Verify Service Broker
   SELECT name, is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';

   -- Verify In-Memory OLTP
   SELECT name FROM sys.tables WHERE is_memory_optimized = 1;
   ```

### Phase 3: Migrate Model Files (Week 3)

1. **Copy Model Files to VM**:
   ```powershell
   # From local machine
   azcopy copy "D:\Models\blobs\*" "https://<storage-account>.blob.core.windows.net/models/" --recursive

   # On VM, download from storage
   azcopy copy "https://<storage-account>.blob.core.windows.net/models/*" "D:\Models\blobs\" --recursive
   ```

2. **Register Models**:
   ```sql
   -- Execute model ingestion script
   sqlcmd -S localhost -d Hartonomous -i sql\Ingest_Models.sql
   ```

3. **Migrate to FILESTREAM** (optional):
   ```sql
   -- Execute FILESTREAM migration procedure
   EXEC sp_MigratePayloadLocatorToFileStream;
   ```

### Phase 4: Deploy Neo4jSync Worker (Week 4)

1. **Deploy as Azure Container Instance** or **Azure App Service (Linux)**:
   ```yaml
   # docker-compose.yml
   services:
     neo4jsync:
       image: hartonomous/neo4jsync:latest
       environment:
         - HARTONOMOUS_SQL_CONNECTION=Server=<vm-sql-server>;Database=Hartonomous;...
         - Neo4j__Uri=bolt://<neo4j-server>:7687
         - Neo4j__Username=neo4j
         - Neo4j__Password=<password>
   ```

2. **Verify Service Broker Message Flow**:
   ```sql
   -- Check queue depth
   SELECT COUNT(*) FROM dbo.Neo4jSyncQueue;

   -- Monitor message processing
   SELECT * FROM sys.transmission_queue;
   ```

### Phase 5: Deploy Hartonomous.Api (Week 5)

1. **Deploy to App Service (Windows)** for SQL Server connectivity:
   - Region: Same as SQL Server VM
   - Plan: Standard S1 (or higher)
   - Runtime: .NET 8
   - Managed Identity: Enabled

2. **Configure Connection String**:
   ```json
   {
     "ConnectionStrings": {
       "HartonomousDb": "Server=<vm-sql-server>;Database=Hartonomous;User ID=<user>;Password=<password>;TrustServerCertificate=True;"
     }
   }
   ```

3. **Verify Service Broker Publishing**:
   - POST to `/api/models` → Should publish message to Service Broker queue
   - Check Neo4jSync logs for message consumption

---

## Cost Comparison: ahdev vs hartonomous SQL Server

### Current Setup: ahdev SQL Server

**Assumptions**:
- Azure SQL Database (not SQL Server on VM)
- Tier: Standard S3 (100 DTUs)
- Region: East US

**Monthly Cost**: ~$150-200/month

**Compatibility**: ❌ **NOT COMPATIBLE** with Hartonomous requirements (no FILESTREAM, CLR UNSAFE, Service Broker)

### Recommended Setup: hartonomous SQL Server on VM

**Configuration**:
- Standard_E8s_v5 (8 vCPUs, 64 GB RAM)
- Premium SSD storage (3.6 TB total)
- Managed Disks: OS (128 GB), Data (1 TB), FILESTREAM (2 TB), Memory-Optimized (512 GB)

**Monthly Cost** (before optimization): ~$915/month

**Cost Optimization**:
- **Stop/Deallocate when not in use**: ~$515/month (storage only)
- **Azure Hybrid Benefit** (BYOL): Save ~$200/month on VM compute
- **Reserved Instance (1-year)**: Save ~$160/month on VM compute
- **After optimization**: ~$300-500/month

**Verdict**: 
- **Delete ahdev**: Save $150-200/month
- **Create hartonomous**: $300-500/month (with optimization)
- **Net increase**: ~$150-350/month
- **Benefit**: Full SQL Server 2025 feature set, 100% compatible with Hartonomous requirements

---

## Alternative: Decouple Service Broker → Azure Service Bus

### Why This is NOT Recommended

User explicitly stated: **"We're using sql server service bus"** (Service Broker, not Azure Service Bus).

However, for completeness, here's the alternative:

**Change Required**:
- Replace `SqlMessageBroker` with `AzureServiceBusMessageBroker`
- Create Azure Service Bus namespace: ~$10-50/month (Basic/Standard tier)
- Refactor Neo4jSync to consume from Service Bus queue instead of SQL Server queue

**Benefits**:
- Could use Azure SQL Database (cheaper than SQL Server on VM)
- Decoupled messaging infrastructure

**Drawbacks**:
- **User rejected this approach**: User confirmed using Service Broker, not Azure Service Bus
- Additional infrastructure: Service Bus namespace
- Operational complexity: Two systems (SQL + Service Bus) vs one (SQL Server)
- No transactional messaging: Cannot commit database + message in same transaction

**Verdict**: **NOT RECOMMENDED** - User explicitly chose Service Broker for transactional messaging and zero external dependencies.

---

## Summary & Recommendations

### Key Findings

1. **Azure SQL Database is NOT compatible** with Hartonomous requirements due to:
   - No FILESTREAM support
   - No CLR UNSAFE support
   - Limited Service Broker support (single database scope)

2. **SQL Server on Azure VM is REQUIRED** for:
   - FILESTREAM (62.81 GB Llama4 + 17.28 GB Qwen3-Coder model storage)
   - CLR UNSAFE (file I/O, shell execution for autonomous deployment)
   - SQL Server Service Broker (asynchronous messaging for Neo4j sync)
   - In-Memory OLTP (billing ledger performance)
   - Full SQL Server 2025 feature set

3. **Service Broker is intentional** - User confirmed using "sql server service bus" (Service Broker) for:
   - Transactional messaging (ACID guarantees with database operations)
   - Zero external dependencies (no Azure Service Bus namespace)
   - Sub-millisecond latency (local database queue)

### Recommended Actions

1. **Delete ahdev SQL Server** ($150-200/month savings)
   - Not compatible with Hartonomous requirements
   - Migrate any critical data to hartonomous SQL Server

2. **Create hartonomous SQL Server on Azure VM** ($300-500/month with optimization)
   - Size: Standard_E8s_v5 (8 vCPUs, 64 GB RAM)
   - Storage: 3.6 TB Premium SSD (OS, Data, FILESTREAM, Memory-Optimized)
   - Image: Windows Server 2022 with SQL Server 2025 Developer (or BYOL)
   - Region: East US (or same region as App Service)

3. **Enable cost optimizations**:
   - Stop/deallocate VM when not in use (~$515/month → $515/month storage only)
   - Apply Azure Hybrid Benefit (BYOL): Save ~$200/month
   - Purchase Reserved Instance (1-year): Save ~$160/month
   - **Net cost after optimization**: ~$300-500/month

4. **Deploy in phases** (Weeks 1-5):
   - Week 1: Provision VM and configure SQL Server
   - Week 2: Deploy database schema and CLR assemblies
   - Week 3: Migrate model files
   - Week 4: Deploy Neo4jSync worker
   - Week 5: Deploy Hartonomous.Api

5. **Monitor and optimize**:
   - Track Query Store performance
   - Monitor Service Broker queue depth
   - Optimize In-Memory OLTP bucket counts
   - Review FILESTREAM storage growth

### Next Steps

Would you like me to:
1. **Create PowerShell deployment scripts** for provisioning the Azure VM and SQL Server?
2. **Update the database deployment script** (`scripts/deploy-database.ps1`) to accept custom FILESTREAM paths?
3. **Create ARM/Bicep templates** for infrastructure-as-code deployment?
4. **Set up Azure DevOps pipeline** for automated database schema deployment?

Let me know and I'll proceed!
