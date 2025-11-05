# SQL Server 2025 Feature Implementation

Hartonomous leverages SQL Server 2025 capabilities for vector search, performance optimization, and operational analytics.

## VECTOR Data Type

Native 1998-dimensional vector support with spatial hybrid search.

**Implementation**:
- `VECTOR(1998)` columns in `dbo.Atoms`, `dbo.AtomEmbeddings`
- Spatial GEOMETRY indexes for O(log n) candidate filtering
- Exact vector distance reranking via `VECTOR_DISTANCE()`
- Multi-resolution search: SpatialCoarse → SpatialGeometry → exact vector

**Stored Procedures**:
- `sp_ExactVectorSearch` - Brute force vector similarity
- `sp_HybridVectorSpatialSearch` - Spatial filtering + vector reranking
- `sp_SemanticFilteredSearch` - Filtered semantic search with automatic strategy selection

**Performance**: Spatial index filtering provides logarithmic complexity before exact distance calculation.

## Query Store

Automatic query performance monitoring and regression detection.

**Configuration** (`sql/EnableQueryStore.sql`):
```sql
ALTER DATABASE Hartonomous SET QUERY_STORE = ON
(
    OPERATION_MODE = READ_WRITE,
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 60,
    MAX_STORAGE_SIZE_MB = 1000,
    QUERY_CAPTURE_MODE = AUTO,
    SIZE_BASED_CLEANUP_MODE = AUTO
);
```

**Usage**:
- Slow query identification for autonomous improvement system
- Execution plan regression detection
- Historical performance analysis
- Plan forcing for stable performance

## Service Broker

Internal messaging infrastructure for event-driven architecture.

**Components**:
- Queue: `HartonomousQueue`
- Conversation-scoped messages with automatic retry
- Dead-letter routing via `MessageDeadLetters` table
- Resilience strategy with circuit breaker

**Implementation**:
- `SqlMessageBroker` - Message publisher
- `ServiceBrokerMessagePump` - Message consumer
- `ServiceBrokerResilienceStrategy` - Retry/circuit breaker patterns

**Event Flow**: CDC → CesConsumer → Service Broker → Neo4jSync → Neo4j

## In-Memory OLTP

Memory-optimized tables and natively compiled procedures for high-throughput operations.

**Design** (`sql/tables/dbo.BillingUsageLedger_InMemory.sql`):
```sql
CREATE TABLE dbo.BillingUsageLedger
(
    -- Schema definition with MEMORY_OPTIMIZED = ON
    -- DURABILITY = SCHEMA_AND_DATA
) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
```

**Natively Compiled Procedure** (`sql/procedures/Billing.InsertUsageRecord_Native.sql`):
```sql
CREATE PROCEDURE sp_InsertBillingUsageRecord_Native
WITH NATIVE_COMPILATION, SCHEMABINDING
AS BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = SNAPSHOT, LANGUAGE = N'English')
    -- Lock-free, latch-free billing inserts
END
```

**Configuration Requirements**:
1. Add memory-optimized filegroup
2. Create memory-optimized table
3. Deploy natively compiled procedure
4. Update `SqlBillingUsageSink.cs` to use native procedure

**Characteristics**: Lock-free writes, latch-free data structures, RAM-speed performance for append-only ledger.

## FILESTREAM

Binary large object storage with file system integration.

**Design** (`sql/Setup_FILESTREAM.sql`):
- FILESTREAM-enabled filegroup for atom payloads
- `PayloadData` column with FILESTREAM attribute
- T-SQL access to file system blobs via streaming API

**Requirements**:
1. Enable FILESTREAM at instance level (Windows Server configuration)
2. Create FILESTREAM filegroup
3. Add FILESTREAM-enabled column to `dbo.Atoms`
4. Update CLR functions for file I/O operations

**Use Cases**: Large audio samples, video frames, SCADA binary data, model weight files.

## PREDICT Integration

Machine Learning Services integration for model-based scoring.

**Design** (`sql/Predict_Integration.sql`):
```sql
-- Example: Autonomous improvement success prediction
SELECT 
    ImprovementId,
    PREDICT(MODEL = @ModelName, DATA = AnalysisResults) AS SuccessProbability
FROM AutonomousImprovementHistory;
```

**Requirements**:
1. Enable SQL Server Machine Learning Services
2. Train models (ONNX format)
3. Register models in database
4. Integrate `PREDICT()` calls in stored procedures

**Use Cases**: Change success prediction, cost estimation, performance forecasting.

## Temporal Tables

System-versioned tables for automatic history tracking.

**Evaluation** (`sql/Temporal_Tables_Evaluation.sql`):
```sql
ALTER TABLE dbo.Models
ADD 
    SysStartTime DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    SysEndTime DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime);

ALTER TABLE dbo.Models
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ModelsHistory));
```

**Use Cases**: Model version tracking, audit trails, point-in-time queries, compliance requirements.

**Benefits**: Automatic history capture, transparent to application code, built-in temporal queries.

## Change Data Capture (CDC)

Database change tracking for event sourcing.

**Implementation**:
- CDC enabled on core tables: `Atoms`, `Models`, `Inferences`
- `CdcRepository` reads CDC tables
- `CdcEventProcessor` transforms to CloudEvents
- Published to Service Broker for downstream processing

**Data Flow**: SQL CDC → CesConsumer → CloudEvents → Service Broker → Event Handlers

## CLR Integration

Custom CLR functions for specialized operations.

**Components** (`src/SqlClr/`):
- **File I/O**: `clr_WriteFileBytes`, `clr_ReadFileBytes`, `clr_ExecuteShellCommand`
- **Aggregates**: `VectorAvg`, `VectorSum`, `VectorMedian`, `VectorWeightedAvg`, `CosineSimilarityAvg`
- **UDTs**: `AtomicStream` (generation provenance), `ComponentStream` (bill-of-materials)

**Deployment** (requires `PERMISSION_SET = UNSAFE`):
```powershell
dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release
sqlcmd -S . -d master -Q "EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
sqlcmd -S . -d Hartonomous -i sql/procedures/Common.ClrBindings.sql
```

**Use Cases**: Vector operations, file system access for autonomous deployment, git operations, spatial calculations.

---

**See also**: [Deployment & Operations](deployment-and-operations.md), [Technical Architecture](technical-architecture.md)
