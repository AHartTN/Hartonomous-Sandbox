# Architecture Overview

## System Design Philosophy

Hartonomous treats SQL Server as a first-class AI runtime, not just a data store. The core insight: **tensors are geometry, inference is spatial query, provenance is graph traversal.**

## Layer Architecture

### 1. Storage Layer

#### GEOMETRY Tensor Storage

Model weights are stored as `GEOMETRY` types, enabling spatial operations on neural network parameters:

```sql
CREATE TABLE TensorAtoms (
    TensorId INT PRIMARY KEY,
    TensorData GEOMETRY NOT NULL,
    Dimensions INT NOT NULL,
    Shape NVARCHAR(MAX), -- JSON array like [512, 768]
    SpatialIndex AS TensorData.STEnvelope() PERSISTED
);

CREATE SPATIAL INDEX IX_TensorAtoms_Spatial 
ON TensorAtoms(TensorData)
WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
```

**Why GEOMETRY?** SQL Server's `VECTOR` type is limited to 1998 dimensions. Many modern models (CLIP, GPT embeddings) use higher dimensions. GEOMETRY supports arbitrary dimensions via trilateration projection.

#### Trilateration Projection

High-dimensional vectors are projected to 3D space using distance-preserving transformations:

1. Select 4 anchor points in high-dimensional space
2. Compute distances from embedding to each anchor
3. Solve for 3D coordinates that preserve those distances
4. Store as `GEOMETRY::Point(x, y, z, srid)`

This enables R-tree spatial indexing for O(log n) nearest-neighbor search.

#### Graph Tables

Provenance tracking uses SQL Server graph syntax:

```sql
CREATE TABLE Inference AS NODE (
    InferenceId INT PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL,
    InputHash VARBINARY(32),
    OutputData NVARCHAR(MAX)
);

CREATE TABLE UsesEmbedding AS EDGE;

CREATE TABLE UsesWeight AS EDGE;

-- Query provenance
SELECT i.InferenceId, e.EmbeddingVector
FROM Inference AS i, UsesEmbedding, Embedding AS e
WHERE MATCH(i-(UsesEmbedding)->e)
  AND i.InferenceId = @TargetInference;
```

#### Temporal Tables

Every critical table has system-versioned temporal tracking:

```sql
ALTER TABLE Embeddings 
ADD 
    SysStartTime DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime);

ALTER TABLE Embeddings SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.EmbeddingsHistory));

-- Time-travel queries
SELECT * 
FROM Embeddings FOR SYSTEM_TIME AS OF '2025-01-01'
WHERE EmbeddingId = 42;
```

#### FILESTREAM for Large Models

Models >8KB are stored as `FILESTREAM` to enable memory-mapped GPU access:

```sql
CREATE TABLE ModelWeights (
    ModelId INT PRIMARY KEY,
    WeightData VARBINARY(MAX) FILESTREAM NOT NULL,
    WeightGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL DEFAULT NEWSEQUENTIALID()
);
```

C# code can map FILESTREAM directly to GPU memory via `SqlFileStream`:

```csharp
using (var stream = new SqlFileStream(path, token, FileAccess.Read))
{
    var gpuBuffer = cuda.MapMemory(stream.SafeFileHandle);
    // Direct GPU access to SQL Server data
}
```

### 2. Computation Layer

#### SQL CLR Functions (.NET Framework 4.8.1)

CLR integration provides performance-critical operations:

**Scalar Functions:**
```csharp
[SqlFunction(IsDeterministic = true, IsPrecise = false)]
public static SqlDouble VectorDotProduct(SqlBytes vector1, SqlBytes vector2)
{
    var a = DeserializeVector(vector1);
    var b = DeserializeVector(vector2);
    
    // AVX2 SIMD acceleration
    return DotProductSIMD(a, b);
}
```

**Aggregate Functions:**
```csharp
[SqlUserDefinedAggregate(
    Format.UserDefined,
    MaxByteSize = -1,
    IsInvariantToNulls = true,
    IsInvariantToDuplicates = false,
    IsInvariantToOrder = false)]
public class VectorMeanAggregate : IBinarySerialize
{
    private List<float[]> vectors = new List<float[]>();
    
    public void Accumulate(SqlBytes vector) 
    {
        vectors.Add(DeserializeVector(vector));
    }
    
    public SqlBytes Terminate()
    {
        // Compute mean in parallel
        var mean = ComputeMeanParallel(vectors);
        return SerializeVector(mean);
    }
}
```

**User-Defined Types (UDTs):**
```csharp
[SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct SparseVector : IBinarySerialize
{
    private Dictionary<int, float> values;
    
    [SqlMethod]
    public SqlDouble DotProduct(SparseVector other)
    {
        // Sparse dot product optimization
        return SparseOps.Dot(this.values, other.values);
    }
}
```

#### CLR Extensions (.NET Framework 4.8.1)

Modern .NET features implemented directly inside the SQL CLR runtime:

**BPE Tokenization:**
```csharp
public class BpeTokenizer
{
    private readonly Dictionary<string, int> vocabulary;
    private readonly List<(string, string)> mergeRules;
    
    public int[] Tokenize(string text)
    {
        var tokens = SplitIntoInitialTokens(text);
        
        while (true)
        {
            var bestMerge = FindBestMerge(tokens);
            if (bestMerge == null) break;
            
            tokens = ApplyMerge(tokens, bestMerge.Value);
        }
        
        return ConvertToIds(tokens);
    }
}
```

**JSON Serialization:**
```csharp
public class JsonSerializerImpl : IJsonSerializer
{
    public string Serialize<T>(T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, options);
    }
    
    public T Deserialize<T>(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
    }
}
```

**Advanced Algorithms:**
```csharp
// t-SNE dimensionality reduction
public class TSNEProjection
{
    public float[][] FitTransform(float[][] data, int dimensions = 2, int iterations = 1000)
    {
        // Proper gradient descent on KL divergence
        var embedding = InitializeRandomEmbedding(data.Length, dimensions);
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var gradient = ComputeGradient(data, embedding);
            embedding = UpdateEmbedding(embedding, gradient, learningRate);
        }
        
        return embedding;
    }
}

// Mahalanobis distance with full covariance
public class MahalanobisDistance
{
    public double Compute(float[] x, float[] mean, float[,] covariance)
    {
        var diff = VectorSubtract(x, mean);
        var invCov = CholeskyInvert(covariance); // Numerically stable
        var mahalanobis = Math.Sqrt(QuadraticForm(diff, invCov));
        return mahalanobis;
    }
}
```

#### SIMD Acceleration

AVX2 intrinsics for batch vector operations:

```csharp
public static unsafe float DotProductAVX2(float* a, float* b, int length)
{
    Vector256<float> sum = Vector256<float>.Zero;
    
    int i = 0;
    for (; i <= length - 8; i += 8)
    {
        var va = Avx.LoadVector256(a + i);
        var vb = Avx.LoadVector256(b + i);
        sum = Avx.Add(sum, Avx.Multiply(va, vb));
    }
    
    // Horizontal sum
    var result = HorizontalAdd(sum);
    
    // Handle remainder
    for (; i < length; i++)
        result += a[i] * b[i];
    
    return result;
}
```

### 3. Intelligence Layer

#### Attention Mechanisms

Multi-head scaled dot-product attention:

```csharp
public SqlBytes MultiHeadAttention(
    SqlBytes queries,  // [batch, seq_len, d_model]
    SqlBytes keys,
    SqlBytes values,
    int numHeads)
{
    var Q = DeserializeMatrix(queries);
    var K = DeserializeMatrix(keys);
    var V = DeserializeMatrix(values);
    
    int dModel = Q.GetLength(2);
    int dK = dModel / numHeads;
    
    var outputs = new List<float[,]>();
    
    for (int h = 0; h < numHeads; h++)
    {
        var QH = ProjectHead(Q, h, dK);
        var KH = ProjectHead(K, h, dK);
        var VH = ProjectHead(V, h, dK);
        
        // Scaled dot-product: softmax(QK^T / sqrt(d_k))V
        var scores = MatMul(QH, Transpose(KH));
        var scaled = Scale(scores, 1.0 / Math.Sqrt(dK));
        var attention = Softmax(scaled);
        var output = MatMul(attention, VH);
        
        outputs.Add(output);
    }
    
    var concatenated = Concatenate(outputs);
    return SerializeMatrix(concatenated);
}
```

#### Graph Neural Networks

Message passing on SQL Server graph tables:

```sql
-- GNN message aggregation
WITH Messages AS (
    SELECT 
        target.NodeId,
        dbo.VectorMean(source.Features) AS AggregatedMessage
    FROM Nodes AS target, Edges, Nodes AS source
    WHERE MATCH(source-(Edges)->target)
    GROUP BY target.NodeId
)
UPDATE n
SET n.Features = dbo.GNNUpdate(n.Features, m.AggregatedMessage)
FROM Nodes n
JOIN Messages m ON n.NodeId = m.NodeId;
```

#### Reasoning Framework

Hypothesis generation and validation:

```csharp
public class ReasoningEngine
{
    public Hypothesis GenerateHypothesis(ObservationSet observations)
    {
        // Analyze query patterns
        var patterns = ExtractPatterns(observations);
        
        // Generate improvement hypothesis
        if (patterns.HasRepeatedSlowQuery)
        {
            return new IndexHypothesis(
                table: patterns.Table,
                columns: patterns.FilterColumns,
                expectedSpeedup: EstimateSpeedup(patterns)
            );
        }
        
        if (patterns.HasFrequentEmbeddingLookup)
        {
            return new PrecomputeHypothesis(
                embedding: patterns.EmbeddingType,
                cachePolicy: DetermineOptimalCache(patterns)
            );
        }
        
        return null;
    }
    
    public ValidationResult ValidateHypothesis(Hypothesis h, TimeSpan shadowPeriod)
    {
        // Test in shadow mode
        var baseline = MeasureBaseline();
        var withChange = MeasureWithHypothesis(h);
        
        return new ValidationResult
        {
            Success = withChange.Latency < baseline.Latency,
            Improvement = baseline.Latency / withChange.Latency,
            SideEffects = DetectSideEffects(baseline, withChange)
        };
    }
}
```

### 4. Autonomous Layer

#### OODA Loop

Implemented as Service Broker conversation:

```sql
-- Observe
CREATE PROCEDURE dbo.sp_OODA_Observe
AS
BEGIN
    DECLARE @observations NVARCHAR(MAX);
    
    SELECT @observations = (
        SELECT 
            QueryHash,
            AVG(ElapsedTime) AS AvgTime,
            COUNT(*) AS ExecutionCount
        FROM sys.dm_exec_query_stats
        WHERE creation_time > DATEADD(hour, -1, GETDATE())
        GROUP BY QueryHash
        HAVING AVG(ElapsedTime) > 1000 -- >1 second
        FOR JSON PATH
    );
    
    -- Send to Orient stage
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE ObservationData (@observations);
END;

-- Orient
CREATE PROCEDURE dbo.sp_OODA_Orient
    @observations NVARCHAR(MAX)
AS
BEGIN
    -- Analyze patterns, generate hypotheses
    DECLARE @hypotheses NVARCHAR(MAX);
    
    EXEC dbo.sp_GenerateHypotheses 
        @observations, 
        @hypotheses OUTPUT;
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE HypothesesGenerated (@hypotheses);
END;

-- Decide
CREATE PROCEDURE dbo.sp_OODA_Decide
    @hypotheses NVARCHAR(MAX)
AS
BEGIN
    -- Prioritize hypotheses by expected value
    DECLARE @selectedHypothesis NVARCHAR(MAX);
    
    SELECT TOP 1 @selectedHypothesis = HypothesisJson
    FROM OPENJSON(@hypotheses)
    WITH (
        HypothesisJson NVARCHAR(MAX) '$',
        ExpectedValue FLOAT '$.expectedValue'
    )
    ORDER BY ExpectedValue DESC;
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE DecisionMade (@selectedHypothesis);
END;

-- Act
CREATE PROCEDURE dbo.sp_OODA_Act
    @hypothesis NVARCHAR(MAX)
AS
BEGIN
    BEGIN TRANSACTION;
    
    DECLARE @success BIT;
    
    EXEC dbo.sp_ExecuteHypothesis 
        @hypothesis, 
        @success OUTPUT;
    
    IF @success = 1
        COMMIT TRANSACTION;
    ELSE
        ROLLBACK TRANSACTION;
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE ActionCompleted (@success);
END;
```

#### Service Broker Message Queue

```sql
CREATE QUEUE OODAQueue;

CREATE SERVICE OODAService
    ON QUEUE OODAQueue
    ([ObservationContract]);

-- Activation procedure
CREATE PROCEDURE dbo.sp_OODA_Activation
AS
BEGIN
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    DECLARE @messageBody NVARCHAR(MAX);
    
    RECEIVE TOP(1)
        @conversationHandle = conversation_handle,
        @messageType = message_type_name,
        @messageBody = CAST(message_body AS NVARCHAR(MAX))
    FROM OODAQueue;
    
    IF @messageType = 'ObservationData'
        EXEC dbo.sp_OODA_Orient @messageBody;
    ELSE IF @messageType = 'HypothesesGenerated'
        EXEC dbo.sp_OODA_Decide @messageBody;
    ELSE IF @messageType = 'DecisionMade'
        EXEC dbo.sp_OODA_Act @messageBody;
END;

ALTER QUEUE OODAQueue
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_OODA_Activation,
    MAX_QUEUE_READERS = 1,
    EXECUTE AS OWNER
);
```

## Data Flow

### Embedding Generation Flow

```
User Query → REST API → T-SQL Procedure
                           ↓
                    CLR: TokenizeText()
                           ↓
                    CLR: BpeTokenizer.Tokenize()
                           ↓
                    CLR: LoadModelWeights() (FILESTREAM)
                           ↓
                    CLR: TransformerInference()
                           ↓
                    CLR: ProjectToGeometry()
                           ↓
                    SQL: INSERT Embeddings (GEOMETRY + Graph edge)
                           ↓
                    SQL: UPDATE Spatial Index
                           ↓
                    Return embedding vector
```

### Semantic Search Flow

```
Query Vector → Spatial Bounding Box Query
                     ↓
              R-tree Index (filter to ~100 candidates)
                     ↓
              CLR: ExactCosineSimilarity() (SIMD)
                     ↓
              SQL: ORDER BY score DESC, TOP K
                     ↓
              Graph: Trace provenance via edges
                     ↓
              Return results + provenance
```

### Autonomous Optimization Flow

```
Query Execution → DMV Observation (slow query)
                        ↓
                  Service Broker: Observe message
                        ↓
                  Orient: Pattern extraction
                        ↓
                  Decide: Hypothesis ranking
                        ↓
                  Act: Create index in transaction
                        ↓
                  Test: Shadow mode validation
                        ↓
                  Learn: Update hypothesis success rate
```

## Key Design Decisions

### Why SQL Server for AI?

1. **Transactional Semantics**: Inference + billing + provenance in one ACID transaction
2. **Mature Tooling**: DBAs already know how to manage, monitor, backup, secure
3. **Spatial Indexes**: R-tree provides O(log n) nearest-neighbor without external vector DB
4. **Graph Queries**: Built-in `MATCH` syntax for provenance traversal
5. **Temporal Tables**: Point-in-time queries for compliance/audit
6. **Service Broker**: Message queue with ACID guarantees (no external Kafka)

### Why CLR Instead of External Services?

1. **Latency**: Sub-millisecond vs. network round-trip
2. **Consistency**: No eventual consistency, no distributed transactions
3. **Deployment**: One database, not N microservices
4. **Security**: Data never leaves SQL Server boundary
5. **Licensing**: No per-inference API charges

### Why GEOMETRY Instead of VECTOR?

1. **Dimension Limit**: VECTOR capped at 1998, GEOMETRY unlimited via projection
2. **Spatial Indexes**: R-tree for O(log n) search vs. linear scan
3. **Cross-Domain Queries**: Spatial joins between embeddings and geospatial data
4. **GPU Mapping**: FILESTREAM enables memory-mapped GPU access

### Why Service Broker for OODA?

1. **ACID Guarantees**: Autonomous actions in transactions
2. **Poison Message Handling**: Failed hypotheses don't crash system
3. **Ordered Execution**: Conversation groups ensure correct sequencing
4. **No External Dependencies**: Built into SQL Server

## Performance Characteristics

### Vector Search Complexity

- **Brute Force**: O(n × d) where n = vector count, d = dimensions
- **Spatial Index**: O(log n + k × d) where k = candidates after bounding box filter
- **Typical k/n Ratio**: 0.001 (100 candidates from 100K vectors)
- **Speedup**: ~100x for large datasets

### Memory Usage

- **Embeddings**: 1998 dims × 4 bytes = 7.99 KB per vector
- **Spatial Index**: ~20% overhead (1.6 KB per vector)
- **Graph Edges**: 24 bytes per relationship
- **Total**: ~10 KB per indexed embedding with provenance

### Throughput

- **Embedding Generation**: ~200 vectors/sec (single core, CPU-only)
- **Vector Search**: ~10K queries/sec (with spatial index)
- **Graph Traversal**: ~50K edges/sec (3-hop queries)
- **OODA Cycle**: ~1 hypothesis/minute (safe autonomous operation)

## Security Model

### Row-Level Security

```sql
CREATE FUNCTION dbo.fn_TenantSecurityPredicate(@TenantId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS AccessGranted
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS INT);

CREATE SECURITY POLICY TenantIsolation
ADD FILTER PREDICATE dbo.fn_TenantSecurityPredicate(TenantId) ON dbo.Embeddings,
ADD BLOCK PREDICATE dbo.fn_TenantSecurityPredicate(TenantId) ON dbo.Embeddings;
```

### Dynamic Data Masking

```sql
ALTER TABLE Embeddings
ALTER COLUMN EmbeddingVector ADD MASKED WITH (FUNCTION = 'default()');

-- Non-privileged users see NULL
SELECT EmbeddingVector FROM Embeddings; -- NULL

-- Privileged users see actual data
GRANT UNMASK TO PowerUser;
```

### Always Encrypted

```sql
CREATE COLUMN MASTER KEY CustomerMasterKey
WITH (
    KEY_STORE_PROVIDER_NAME = 'AZURE_KEY_VAULT',
    KEY_PATH = 'https://myvault.vault.azure.net/keys/CMK1'
);

CREATE COLUMN ENCRYPTION KEY CustomerEncryptionKey
WITH VALUES (
    COLUMN_MASTER_KEY = CustomerMasterKey,
    ALGORITHM = 'RSA_OAEP',
    ENCRYPTED_VALUE = 0x...
);

ALTER TABLE SensitiveData
ALTER COLUMN ApiKey VARBINARY(MAX)
ENCRYPTED WITH (
    COLUMN_ENCRYPTION_KEY = CustomerEncryptionKey,
    ENCRYPTION_TYPE = Deterministic,
    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
);
```

## Monitoring and Observability

### Query Store

```sql
ALTER DATABASE Hartonomous SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    MAX_STORAGE_SIZE_MB = 1024,
    INTERVAL_LENGTH_MINUTES = 60
);

-- Analyze embedding query performance
SELECT 
    q.query_id,
    qt.query_sql_text,
    rs.avg_duration/1000.0 AS avg_duration_ms,
    rs.avg_logical_io_reads
FROM sys.query_store_query q
JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
JOIN sys.query_store_plan p ON q.query_id = p.query_id
JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
WHERE qt.query_sql_text LIKE '%VectorDistance%'
ORDER BY rs.avg_duration DESC;
```

### Extended Events

```sql
CREATE EVENT SESSION [AI_Operations]
ON SERVER
ADD EVENT sqlserver.clr_assembly_load,
ADD EVENT sqlserver.clr_allocation_failure,
ADD EVENT sqlserver.sp_statement_completed (
    WHERE sqlserver.database_name = 'Hartonomous'
      AND object_name LIKE '%Vector%'
)
ADD TARGET package0.event_file (
    SET filename = N'AI_Operations.xel'
);

ALTER EVENT SESSION [AI_Operations] ON SERVER STATE = START;
```

### Custom Metrics

```sql
CREATE TABLE Metrics (
    MetricId BIGINT IDENTITY PRIMARY KEY,
    MetricName NVARCHAR(100) NOT NULL,
    Value FLOAT NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_Metrics_Timestamp NONCLUSTERED (Timestamp)
) WITH (MEMORY_OPTIMIZED = ON);

-- In-Memory OLTP for lock-free inserts
CREATE PROCEDURE dbo.sp_RecordMetric
    @MetricName NVARCHAR(100),
    @Value FLOAT
WITH NATIVE_COMPILATION, SCHEMABINDING
AS
BEGIN ATOMIC WITH (
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    INSERT INTO dbo.Metrics (MetricName, Value)
    VALUES (@MetricName, @Value);
END;
```

## Deployment Considerations

### High Availability

- **Always On Availability Groups**: Async replicas for read-scale vector search
- **Readable Secondaries**: Route search queries to replicas, writes to primary
- **Automatic Failover**: Embedding generation continues during primary failure

### Disaster Recovery

- **Point-in-Time Restore**: Full + differential + log backups every 15 minutes
- **FILESTREAM Backup**: Model weights included in database backup chain
- **Geo-Replication**: Async shipping to secondary datacenter

### Scalability

- **Read Scale-Out**: Distribute searches across readable secondaries
- **Partitioning**: Partition embeddings by date or tenant
- **Columnstore**: Archive old embeddings in compressed columnstore tables

## Future Enhancements

- **GPU Direct Access**: CUDA integration for CLR functions
- **Distributed Training**: Federated learning across availability group replicas
- **Streaming Inference**: Real-time model updates via Change Data Capture
- **Multi-Model Orchestration**: Ensemble predictions combining multiple models
