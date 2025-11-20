# SQL Server 2025 Integration

**Status**: Design Phase  
**Last Updated**: November 18, 2025  
**Owner**: CLR Refactoring Team

## Overview

This document specifies how to integrate SQL Server 2025 native features with the Hartonomous universal file system while **preserving the existing geometric AI architecture**. The vector type is used **selectively** for external embeddings only - it does NOT replace the 1998-dimensional landmark projection system.

### Key Principles

1. **Preserve Geometric AI**: R-Tree spatial indexing, Gram-Schmidt orthogonalization, Hilbert curves, landmark projection remain unchanged
2. **Selective Vector Usage**: Use vector type ONLY for external embeddings from OpenAI/Azure that arrive as vectors
3. **sp_invoke_external_rest_endpoint**: Primary method for external API calls (in-process, in-memory)
4. **SQL Service Broker**: Alternative for async message-based patterns
5. **Row-Level Security**: Multi-tenant isolation for pay-to-upload/pay-to-hide business model
6. **Native JSON**: Use json type for metadata, configuration, and flexible schemas

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    SQL Server 2025 Features                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────────┐         ┌───────────────────────┐       │
│  │ Geometric AI       │         │ External Embeddings   │       │
│  │ (PRESERVED)        │         │ (NEW - Vector Type)   │       │
│  ├────────────────────┤         ├───────────────────────┤       │
│  │ • R-Tree Spatial   │         │ • OpenAI Embeddings   │       │
│  │ • Gram-Schmidt     │         │ • Azure Embeddings    │       │
│  │ • Hilbert Curves   │         │ • vector(1536)        │       │
│  │ • 1998-dim         │         │ • Cosine Similarity   │       │
│  │ • Landmark Proj    │         │ • Dot Product         │       │
│  └────────────────────┘         └───────────────────────┘       │
│           ↓                              ↓                       │
│  ┌─────────────────────────────────────────────────────┐        │
│  │    sp_invoke_external_rest_endpoint                 │        │
│  │    (In-process, in-memory HTTP calls)               │        │
│  └─────────────────────────────────────────────────────┘        │
│           ↓                              ↓                       │
│  ┌─────────────────────────────────────────────────────┐        │
│  │         SQL Service Broker (Async Messages)         │        │
│  └─────────────────────────────────────────────────────┘        │
│           ↓                              ↓                       │
│  ┌─────────────────────────────────────────────────────┐        │
│  │     Row-Level Security (Multi-Tenant Isolation)     │        │
│  └─────────────────────────────────────────────────────┘        │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Vector Type Usage (SELECTIVE)

### DO Use Vector Type For:

✅ **External Model Embeddings**: OpenAI, Azure OpenAI embeddings that arrive as vectors  
✅ **Similarity Search**: When embedding already exists and needs cosine/dot product  
✅ **Hybrid Search**: Combining keyword search with vector similarity  
✅ **Cross-Modal Search**: Text-to-image, image-to-text using external embeddings  

### DO NOT Use Vector Type For:

❌ **Geometric AI**: 1998-dimensional landmark projection (use existing FLOAT columns)  
❌ **Spatial Indexing**: R-Tree queries (use spatial_index table)  
❌ **Gram-Schmidt**: Orthogonalization (use existing CLR functions)  
❌ **Hilbert Curves**: Space-filling curves (use existing geometry columns)  

### Schema Example: Coexistence

```sql
-- Existing geometric AI table (UNCHANGED)
CREATE TABLE ModelEmbeddings (
    ModelId INT PRIMARY KEY,
    TenantId INT NOT NULL,
    
    -- EXISTING: 1998-dimensional landmark projection
    Embedding1 FLOAT NOT NULL,
    Embedding2 FLOAT NOT NULL,
    -- ... (1998 columns total)
    Embedding1998 FLOAT NOT NULL,
    
    -- EXISTING: Spatial indexing
    SpatialHash BIGINT NOT NULL,
    HilbertCurveIndex BIGINT NOT NULL,
    
    -- EXISTING: Gram-Schmidt metadata
    OrthogonalizationVersion INT NOT NULL,
    LandmarkSetId INT NOT NULL,
    
    INDEX IX_ModelEmbeddings_Spatial (SpatialHash, HilbertCurveIndex)
);

-- NEW: External embeddings table (vector type)
CREATE TABLE ExternalEmbeddings (
    EmbeddingId BIGINT PRIMARY KEY IDENTITY(1,1),
    ModelId INT NOT NULL REFERENCES Models(ModelId),
    TenantId INT NOT NULL,
    
    -- NEW: Vector type for external embeddings only
    OpenAIEmbedding vector(1536) NULL,      -- text-embedding-3-small
    AzureEmbedding vector(3072) NULL,       -- text-embedding-3-large
    
    Provider NVARCHAR(50) NOT NULL,         -- 'OpenAI', 'Azure', 'Cohere'
    Model NVARCHAR(100) NOT NULL,           -- 'text-embedding-3-small'
    
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_ExternalEmbeddings_Tenant (TenantId),
    INDEX IX_ExternalEmbeddings_Model (ModelId)
);

-- Hybrid query: Combine geometric AI with external embeddings
SELECT 
    m.ModelId,
    m.ModelName,
    -- Geometric AI similarity (existing CLR function)
    dbo.CalculateCosineSimilarity(@queryEmbedding, m.Embedding1, ..., m.Embedding1998) AS GeometricSimilarity,
    -- External embedding similarity (new vector function)
    VECTOR_DISTANCE('cosine', e.OpenAIEmbedding, @openAIQueryEmbedding) AS OpenAISimilarity,
    -- Combined score
    (0.7 * dbo.CalculateCosineSimilarity(...) + 0.3 * VECTOR_DISTANCE(...)) AS HybridScore
FROM Models m
LEFT JOIN ExternalEmbeddings e ON m.ModelId = e.ModelId
WHERE m.TenantId = @tenantId
ORDER BY HybridScore DESC;
```

### Vector Type Functions (SQL Server 2025)

```sql
-- Cosine distance (0 = identical, 2 = opposite)
SELECT VECTOR_DISTANCE('cosine', @vec1, @vec2) AS CosineDist;

-- Dot product (higher = more similar)
SELECT VECTOR_DISTANCE('dot', @vec1, @vec2) AS DotProduct;

-- Euclidean distance (L2)
SELECT VECTOR_DISTANCE('euclidean', @vec1, @vec2) AS L2Dist;

-- Vector normalization
DECLARE @normalized vector(1536) = VECTOR_NORMALIZE(@vec);

-- Vector addition/subtraction
DECLARE @result vector(1536) = VECTOR_ADD(@vec1, @vec2);
DECLARE @diff vector(1536) = VECTOR_SUBTRACT(@vec1, @vec2);
```

## sp_invoke_external_rest_endpoint

### Primary External Call Method

Use `sp_invoke_external_rest_endpoint` for:
- OpenAI API calls (embeddings, completions)
- Azure OpenAI endpoints
- HuggingFace Inference API
- Ollama API
- External model serving endpoints

**Benefits**:
- In-process execution (no external process overhead)
- In-memory data transfer (no disk I/O)
- Managed authentication (Managed Identity support)
- Automatic retry logic
- Connection pooling

### Configuration

```sql
-- Enable external REST endpoint feature
EXEC sp_configure 'external scripts enabled', 1;
RECONFIGURE;

-- Grant permissions
GRANT EXECUTE ON sp_invoke_external_rest_endpoint TO HartonomousApp;
```

### Usage Examples

#### OpenAI Embeddings

```sql
-- Create credential for OpenAI
CREATE DATABASE SCOPED CREDENTIAL OpenAICredential
WITH IDENTITY = 'APIKey',
     SECRET = '<your-openai-api-key>';

-- Create external data source
CREATE EXTERNAL DATA SOURCE OpenAIAPI
WITH (
    TYPE = REST,
    LOCATION = 'https://api.openai.com/v1',
    CREDENTIAL = OpenAICredential
);

-- Call OpenAI embeddings API
DECLARE @response NVARCHAR(MAX);
DECLARE @inputText NVARCHAR(MAX) = 'Sample text for embedding';

EXEC @response = sp_invoke_external_rest_endpoint
    @url = 'https://api.openai.com/v1/embeddings',
    @method = 'POST',
    @headers = '{"Content-Type": "application/json", "Authorization": "Bearer <key>"}',
    @payload = JSON_OBJECT(
        'model': 'text-embedding-3-small',
        'input': @inputText
    );

-- Parse response and store embedding
DECLARE @embedding vector(1536) = CAST(
    JSON_QUERY(@response, '$.data[0].embedding') AS vector(1536)
);

INSERT INTO ExternalEmbeddings (ModelId, TenantId, OpenAIEmbedding, Provider, Model)
VALUES (@modelId, @tenantId, @embedding, 'OpenAI', 'text-embedding-3-small');
```

#### Azure OpenAI with Managed Identity

```sql
-- Create credential with Managed Identity
CREATE DATABASE SCOPED CREDENTIAL AzureOpenAICredential
WITH IDENTITY = 'Managed Identity';

-- Create external data source
CREATE EXTERNAL DATA SOURCE AzureOpenAIAPI
WITH (
    TYPE = REST,
    LOCATION = 'https://<resource-name>.openai.azure.com',
    CREDENTIAL = AzureOpenAICredential
);

-- Call Azure OpenAI
DECLARE @response NVARCHAR(MAX);
EXEC @response = sp_invoke_external_rest_endpoint
    @url = 'https://<resource-name>.openai.azure.com/openai/deployments/<deployment-name>/embeddings?api-version=2024-02-15-preview',
    @method = 'POST',
    @credential = AzureOpenAICredential,
    @payload = JSON_OBJECT(
        'input': @inputText
    );
```

#### HuggingFace Inference API

```sql
-- Create credential for HuggingFace
CREATE DATABASE SCOPED CREDENTIAL HuggingFaceCredential
WITH IDENTITY = 'Token',
     SECRET = '<your-hf-token>';

-- Call HuggingFace model
DECLARE @response NVARCHAR(MAX);
EXEC @response = sp_invoke_external_rest_endpoint
    @url = 'https://api-inference.huggingface.co/models/sentence-transformers/all-MiniLM-L6-v2',
    @method = 'POST',
    @headers = JSON_OBJECT('Authorization': CONCAT('Bearer ', '<token>')),
    @payload = JSON_OBJECT(
        'inputs': @inputText,
        'options': JSON_OBJECT('wait_for_model': CAST(1 AS BIT))
    );
```

#### Ollama Local API

```sql
-- No credential needed for local Ollama
DECLARE @response NVARCHAR(MAX);
EXEC @response = sp_invoke_external_rest_endpoint
    @url = 'http://localhost:11434/api/embeddings',
    @method = 'POST',
    @headers = '{"Content-Type": "application/json"}',
    @payload = JSON_OBJECT(
        'model': 'llama2',
        'prompt': @inputText
    );

-- Parse Ollama response
DECLARE @embedding vector(4096) = CAST(
    JSON_QUERY(@response, '$.embedding') AS vector(4096)
);
```

### Wrapper Functions (CLR)

```csharp
namespace Hartonomous.Clr.ExternalAPIs
{
    public static class ExternalModelAPI
    {
        /// <summary>
        /// Call OpenAI embeddings API via sp_invoke_external_rest_endpoint.
        /// </summary>
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static SqlString GetOpenAIEmbedding(
            SqlString text,
            SqlString model,
            SqlString apiKey)
        {
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();

                string payload = JsonConvert.SerializeObject(new
                {
                    model = model.Value,
                    input = text.Value
                });

                string headers = JsonConvert.SerializeObject(new
                {
                    ContentType = "application/json",
                    Authorization = $"Bearer {apiKey.Value}"
                });

                using (SqlCommand cmd = new SqlCommand(@"
                    DECLARE @response NVARCHAR(MAX);
                    EXEC sp_invoke_external_rest_endpoint
                        @url = 'https://api.openai.com/v1/embeddings',
                        @method = 'POST',
                        @headers = @headers,
                        @payload = @payload,
                        @response = @response OUTPUT;
                    SELECT @response;
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@headers", headers);
                    cmd.Parameters.AddWithValue("@payload", payload);

                    string response = (string)cmd.ExecuteScalar();
                    return new SqlString(response);
                }
            }
        }
    }
}
```

## SQL Service Broker (Async Patterns)

### When to Use Service Broker

Use SQL Service Broker for:
- Long-running model inference
- Batch embedding generation
- Async file processing
- Streaming data ingestion
- Background indexing operations

### Configuration

```sql
-- Enable Service Broker
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Create message types
CREATE MESSAGE TYPE [ModelInferenceRequest]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [ModelInferenceResponse]
VALIDATION = WELL_FORMED_XML;

-- Create contract
CREATE CONTRACT [ModelInferenceContract]
(
    [ModelInferenceRequest] SENT BY INITIATOR,
    [ModelInferenceResponse] SENT BY TARGET
);

-- Create queues
CREATE QUEUE ModelInferenceRequestQueue;
CREATE QUEUE ModelInferenceResponseQueue;

-- Create services
CREATE SERVICE [ModelInferenceRequestService]
ON QUEUE ModelInferenceRequestQueue
([ModelInferenceContract]);

CREATE SERVICE [ModelInferenceResponseService]
ON QUEUE ModelInferenceResponseQueue
([ModelInferenceContract]);
```

### Send Inference Request

```sql
-- Stored procedure to send inference request
CREATE PROCEDURE SendModelInferenceRequest
    @modelId INT,
    @inputData NVARCHAR(MAX),
    @tenantId INT
AS
BEGIN
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageBody XML = (
        SELECT 
            @modelId AS ModelId,
            @inputData AS InputData,
            @tenantId AS TenantId
        FOR XML PATH('InferenceRequest'), TYPE
    );

    BEGIN DIALOG CONVERSATION @conversationHandle
    FROM SERVICE [ModelInferenceRequestService]
    TO SERVICE 'ModelInferenceResponseService'
    ON CONTRACT [ModelInferenceContract]
    WITH ENCRYPTION = OFF;

    SEND ON CONVERSATION @conversationHandle
    MESSAGE TYPE [ModelInferenceRequest](@messageBody);

    -- Store conversation handle for tracking
    INSERT INTO InferenceRequests (ConversationHandle, ModelId, TenantId, Status, CreatedDate)
    VALUES (@conversationHandle, @modelId, @tenantId, 'Pending', GETUTCDATE());
END;
```

### Process Requests (Activation)

```sql
-- Activation procedure
CREATE PROCEDURE ProcessModelInferenceRequests
AS
BEGIN
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageBody XML;
    DECLARE @messageType NVARCHAR(256);

    WHILE (1=1)
    BEGIN
        BEGIN TRANSACTION;

        WAITFOR (
            RECEIVE TOP(1)
                @conversationHandle = conversation_handle,
                @messageBody = message_body,
                @messageType = message_type_name
            FROM ModelInferenceRequestQueue
        ), TIMEOUT 5000;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            BREAK;
        END;

        -- Parse request
        DECLARE @modelId INT = @messageBody.value('(/InferenceRequest/ModelId)[1]', 'INT');
        DECLARE @inputData NVARCHAR(MAX) = @messageBody.value('(/InferenceRequest/InputData)[1]', 'NVARCHAR(MAX)');
        DECLARE @tenantId INT = @messageBody.value('(/InferenceRequest/TenantId)[1]', 'INT');

        -- Perform inference (call external API, CLR function, etc.)
        DECLARE @result NVARCHAR(MAX);
        
        -- Example: Call OpenAI via sp_invoke_external_rest_endpoint
        EXEC @result = sp_invoke_external_rest_endpoint
            @url = 'https://api.openai.com/v1/chat/completions',
            @method = 'POST',
            @payload = JSON_OBJECT(
                'model': 'gpt-4',
                'messages': JSON_ARRAY(
                    JSON_OBJECT('role': 'user', 'content': @inputData)
                )
            );

        -- Send response
        DECLARE @responseBody XML = (
            SELECT 
                @modelId AS ModelId,
                @result AS Result
            FOR XML PATH('InferenceResponse'), TYPE
        );

        SEND ON CONVERSATION @conversationHandle
        MESSAGE TYPE [ModelInferenceResponse](@responseBody);

        -- Update request status
        UPDATE InferenceRequests
        SET Status = 'Completed',
            CompletedDate = GETUTCDATE(),
            Result = @result
        WHERE ConversationHandle = @conversationHandle;

        END CONVERSATION @conversationHandle;

        COMMIT TRANSACTION;
    END;
END;
```

### Enable Activation

```sql
-- Configure queue activation
ALTER QUEUE ModelInferenceRequestQueue
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = ProcessModelInferenceRequests,
    MAX_QUEUE_READERS = 5,  -- Parallel processing
    EXECUTE AS SELF
);
```

## Row-Level Security (Multi-Tenant)

### Security Predicate Function

```sql
-- Create security schema
CREATE SCHEMA Security;
GO

-- Tenant access predicate
CREATE FUNCTION Security.fn_TenantAccessPredicate(@TenantId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS AccessResult
    WHERE
        -- User can see their own content
        @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS INT)
        -- OR content is public
        OR EXISTS (
            SELECT 1 FROM dbo.ContentMetadata cm
            WHERE cm.TenantId = @TenantId AND cm.IsPublic = 1
        )
        -- OR content is part of global model (pay-to-upload)
        OR EXISTS (
            SELECT 1 FROM dbo.ContentMetadata cm
            WHERE cm.TenantId = @TenantId AND cm.IsGlobalContributor = 1
        )
        -- OR user has premium/enterprise tier
        OR EXISTS (
            SELECT 1 FROM dbo.Tenants t
            WHERE t.TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS INT)
              AND t.SubscriptionTier IN ('Premium', 'Enterprise')
        );
GO

-- Apply security policy to all tables
CREATE SECURITY POLICY Security.TenantAccessPolicy
ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.Models,
ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ModelEmbeddings,
ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ExternalEmbeddings,
ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ModelFiles,
ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.Models AFTER INSERT,
ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ModelEmbeddings AFTER INSERT;
GO
```

### Application-Side Context Setup

```csharp
// Set tenant context after authentication
public class DatabaseContext
{
    public static void SetTenantContext(SqlConnection connection, int tenantId)
    {
        using (SqlCommand cmd = new SqlCommand(
            "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId", 
            connection))
        {
            cmd.Parameters.AddWithValue("@tenantId", tenantId);
            cmd.ExecuteNonQuery();
        }
    }

    public static void ClearTenantContext(SqlConnection connection)
    {
        using (SqlCommand cmd = new SqlCommand(
            "EXEC sp_set_session_context @key = N'TenantId', @value = NULL", 
            connection))
        {
            cmd.ExecuteNonQuery();
        }
    }
}

// Usage
using (SqlConnection conn = new SqlConnection(connectionString))
{
    conn.Open();
    
    // Set tenant context
    DatabaseContext.SetTenantContext(conn, authenticatedTenantId);
    
    // All queries now automatically filtered by RLS
    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Models", conn))
    {
        // Only returns models for authenticatedTenantId (or public/global)
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            // Process results
        }
    }
}
```

## Native JSON Type

### Schema Design with JSON

```sql
-- Use json type for flexible metadata
CREATE TABLE Models (
    ModelId INT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    ModelName NVARCHAR(255) NOT NULL,
    
    -- Flexible metadata as JSON
    Metadata json NOT NULL,
    
    -- Specific indexed properties
    Architecture AS JSON_VALUE(Metadata, '$.architecture') PERSISTED,
    Framework AS JSON_VALUE(Metadata, '$.framework') PERSISTED,
    
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_Models_Architecture (Architecture),
    INDEX IX_Models_Framework (Framework)
);

-- Insert with JSON
INSERT INTO Models (TenantId, ModelName, Metadata)
VALUES (
    @tenantId,
    'llama-2-7b',
    JSON_OBJECT(
        'architecture': 'transformer',
        'framework': 'pytorch',
        'parameters': 7000000000,
        'layers': 32,
        'vocabulary_size': 32000,
        'context_length': 4096,
        'quantization': JSON_OBJECT(
            'method': 'GGUF',
            'bits': 4
        ),
        'tags': JSON_ARRAY('llama', 'causal-lm', 'text-generation')
    )
);

-- Query JSON fields
SELECT 
    ModelId,
    ModelName,
    JSON_VALUE(Metadata, '$.architecture') AS Architecture,
    JSON_VALUE(Metadata, '$.parameters') AS Parameters,
    JSON_QUERY(Metadata, '$.quantization') AS Quantization
FROM Models
WHERE JSON_VALUE(Metadata, '$.framework') = 'pytorch'
  AND CAST(JSON_VALUE(Metadata, '$.parameters') AS BIGINT) < 10000000000;

-- Update JSON property
UPDATE Models
SET Metadata = JSON_MODIFY(Metadata, '$.version', '2.1')
WHERE ModelId = @modelId;

-- Add array element
UPDATE Models
SET Metadata = JSON_MODIFY(Metadata, 'append $.tags', 'fine-tuned')
WHERE ModelId = @modelId;
```

### JSON Indexing

```sql
-- Create computed columns for indexing
ALTER TABLE Models
ADD ParameterCount AS CAST(JSON_VALUE(Metadata, '$.parameters') AS BIGINT) PERSISTED;

CREATE INDEX IX_Models_ParameterCount ON Models(ParameterCount);

-- Full-text index on JSON
CREATE FULLTEXT CATALOG ModelMetadataCatalog;

CREATE FULLTEXT INDEX ON Models(Metadata)
KEY INDEX PK_Models
ON ModelMetadataCatalog;

-- Full-text search
SELECT * FROM Models
WHERE CONTAINS(Metadata, 'transformer AND pytorch');
```

## Performance Optimization

### Batch Operations

```sql
-- Batch insert external embeddings
CREATE PROCEDURE BatchInsertEmbeddings
    @embeddings dbo.EmbeddingTableType READONLY
AS
BEGIN
    INSERT INTO ExternalEmbeddings (ModelId, TenantId, OpenAIEmbedding, Provider, Model)
    SELECT ModelId, TenantId, Embedding, Provider, Model
    FROM @embeddings;
END;

-- Table type
CREATE TYPE dbo.EmbeddingTableType AS TABLE (
    ModelId INT,
    TenantId INT,
    Embedding vector(1536),
    Provider NVARCHAR(50),
    Model NVARCHAR(100)
);
```

### Parallel Query Execution

```sql
-- Enable parallel execution for vector operations
SELECT 
    ModelId,
    VECTOR_DISTANCE('cosine', OpenAIEmbedding, @queryEmbedding) AS Similarity
FROM ExternalEmbeddings
WHERE TenantId = @tenantId
OPTION (MAXDOP 8);  -- Use 8 cores
```

### Materialized Views

```sql
-- Precompute popular queries
CREATE VIEW vw_TopModels
WITH SCHEMABINDING
AS
SELECT 
    m.ModelId,
    m.ModelName,
    m.TenantId,
    COUNT_BIG(*) AS DownloadCount
FROM dbo.Models m
INNER JOIN dbo.ModelDownloads d ON m.ModelId = d.ModelId
GROUP BY m.ModelId, m.ModelName, m.TenantId;

CREATE UNIQUE CLUSTERED INDEX IX_vw_TopModels 
ON vw_TopModels(ModelId);
```

## Migration Strategy

### Phase 1: Add Vector Columns (Non-Breaking)

```sql
-- Add vector columns to existing tables
ALTER TABLE ModelEmbeddings
ADD OpenAIEmbedding vector(1536) NULL;

ALTER TABLE ModelEmbeddings
ADD AzureEmbedding vector(3072) NULL;

-- Existing queries continue to work
```

### Phase 2: Populate External Embeddings

```sql
-- Background job to populate embeddings
CREATE PROCEDURE PopulateExternalEmbeddings
    @batchSize INT = 100
AS
BEGIN
    DECLARE @modelIds TABLE (ModelId INT);

    -- Get models without external embeddings
    INSERT INTO @modelIds
    SELECT TOP (@batchSize) m.ModelId
    FROM Models m
    LEFT JOIN ExternalEmbeddings e ON m.ModelId = e.ModelId
    WHERE e.EmbeddingId IS NULL
      AND m.TenantId = @tenantId;

    -- Generate embeddings via API
    DECLARE @modelId INT;
    DECLARE cursor CURSOR FOR SELECT ModelId FROM @modelIds;
    OPEN cursor;

    FETCH NEXT FROM cursor INTO @modelId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Call OpenAI API
        DECLARE @response NVARCHAR(MAX);
        EXEC @response = sp_invoke_external_rest_endpoint
            @url = 'https://api.openai.com/v1/embeddings',
            @method = 'POST',
            @payload = /* ... */;

        -- Store embedding
        INSERT INTO ExternalEmbeddings (ModelId, TenantId, OpenAIEmbedding, Provider, Model)
        VALUES (@modelId, @tenantId, CAST(/* parse response */ AS vector(1536)), 'OpenAI', 'text-embedding-3-small');

        FETCH NEXT FROM cursor INTO @modelId;
    END;

    CLOSE cursor;
    DEALLOCATE cursor;
END;
```

### Phase 3: Hybrid Queries

```sql
-- Combine geometric AI with external embeddings
CREATE PROCEDURE SearchModels
    @queryText NVARCHAR(MAX),
    @queryEmbedding dbo.EmbeddingType,  -- 1998-dim geometric
    @tenantId INT,
    @top INT = 20
AS
BEGIN
    -- Get OpenAI embedding for query
    DECLARE @openAIQueryEmbedding vector(1536);
    EXEC @openAIQueryEmbedding = dbo.GetOpenAIEmbedding(@queryText);

    -- Hybrid search
    SELECT TOP (@top)
        m.ModelId,
        m.ModelName,
        m.Metadata,
        -- Geometric AI similarity (existing)
        dbo.CalculateCosineSimilarity(@queryEmbedding, me.Embedding1, ..., me.Embedding1998) AS GeometricScore,
        -- External embedding similarity (new)
        VECTOR_DISTANCE('cosine', ee.OpenAIEmbedding, @openAIQueryEmbedding) AS VectorScore,
        -- Weighted hybrid score
        (0.7 * dbo.CalculateCosineSimilarity(...) + 0.3 * (1 - VECTOR_DISTANCE(...))) AS HybridScore
    FROM Models m
    INNER JOIN ModelEmbeddings me ON m.ModelId = me.ModelId
    LEFT JOIN ExternalEmbeddings ee ON m.ModelId = ee.ModelId
    WHERE m.TenantId = @tenantId
    ORDER BY HybridScore DESC;
END;
```

## Summary

SQL Server 2025 integration provides:

✅ **Geometric AI Preserved**: 1998-dim landmark projection, R-Tree, Gram-Schmidt, Hilbert curves unchanged  
✅ **Selective Vector Usage**: vector type for external embeddings only (OpenAI, Azure)  
✅ **sp_invoke_external_rest_endpoint**: In-process, in-memory external API calls  
✅ **SQL Service Broker**: Async message-based processing  
✅ **Row-Level Security**: Multi-tenant isolation for pay-to-upload/pay-to-hide  
✅ **Native JSON**: Flexible metadata with indexing and full-text search  
✅ **Hybrid Search**: Combine geometric AI with external embeddings  
✅ **Non-Breaking Migration**: Add features incrementally without disruption  

**Philosophy**: Use SQL Server 2025 features to enhance, not replace, existing capabilities.
