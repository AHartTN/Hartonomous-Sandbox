# HARTONOMOUS SQL DATABASE PROJECT - COMPREHENSIVE AUDIT PART 7
**Generated:** 2025-11-20 02:30:00  
**Continuation:** Parts 1-6 complete (45 files analyzed, 14.3%)  
**Focus:** Utility procedures, reconstruction, views, functions  

---

## PART 7: UTILITY PROCEDURES & RECONSTRUCTION PATTERNS

### FILES ANALYZED IN PART 7

1. **dbo.sp_GenerateWithAttention** (Procedures/) - Attention-based text generation
2. **dbo.sp_ExtractMetadata** (Procedures/) - Atom metadata extraction
3. **dbo.sp_ComputeAllSemanticFeatures** (Procedures/) - Batch semantic analysis
4. **dbo.sp_TokenizeText** (Procedures/) - Text tokenization wrapper
5. **dbo.sp_AtomizeText_Governed** (Procedures/) - Governed text atomization
6. **dbo.sp_ReconstructText** (StoredProcedures/) - Text reconstruction from atoms
7. **dbo.sp_ReconstructImage** (StoredProcedures/) - Image reconstruction from pixels
8. **dbo.vw_ModelPerformance** (Views/) - Model performance metrics view
9. **dbo.vw_ModelDetails** (Views/) - Model detail view
10. **dbo.fn_VectorCosineSimilarity** (Functions/) - Vector similarity calculation
11. **dbo.fn_ComputeSpatialBucket** (Functions/) - Spatial bucketing for R-Tree

**Total This Part:** 11 files  
**Cumulative Total:** 56 of 315+ files (17.8%)

---

## 1. PROCEDURE: dbo.sp_GenerateWithAttention

**File:** `Procedures/dbo.sp_GenerateWithAttention.sql`  
**Lines:** 106  
**Purpose:** Multi-head attention-based text generation using CLR function  

**Quality Score: 68/100** ⚠️

### Schema Analysis

```sql
CREATE PROCEDURE dbo.sp_GenerateWithAttention
    @ModelId INT,
    @InputAtomIds NVARCHAR(MAX), -- Comma-separated atom IDs
    @ContextJson NVARCHAR(MAX) = '{}',
    @MaxTokens INT = 100,
    @Temperature FLOAT = 1.0,
    @TopK INT = 50,
    @TopP FLOAT = 0.9,
    @AttentionHeads INT = 8,
    @TenantId INT = 0,
    @Debug BIT = 0
```

**Dependencies:**
- ✅ `dbo.fn_GenerateWithAttention` (CLR function) - **MISSING** ❌
- ✅ `dbo.AttentionGenerationLog` (table) - **MISSING** ❌

**Implementation Pattern:**
```sql
-- Calls CLR function for actual generation
SELECT @GenerationStreamId = dbo.fn_GenerateWithAttention(
    @ModelId, @InputAtomIds, @ContextJson,
    @MaxTokens, @Temperature, @TopK, @TopP,
    @AttentionHeads, @TenantId
);

-- Logs to AttentionGenerationLog table
INSERT INTO dbo.AttentionGenerationLog (...) VALUES (...);
```

### Issues Found

1. **❌ BLOCKING: Missing CLR Function**
   - Calls `dbo.fn_GenerateWithAttention` which doesn't exist
   - Procedure will fail at runtime with "Invalid object name"
   - **Impact:** CRITICAL - Cannot generate text with attention

2. **❌ BLOCKING: Missing Log Table**
   - References `AttentionGenerationLog` table which doesn't exist
   - INSERT will fail at runtime
   - **Impact:** HIGH - Logging fails, transaction rollback

3. **⚠️ Inconsistent Naming**
   - Parameter: `@InputAtomIds` (plural)
   - Other procedures use singular `@AtomId`
   - **Impact:** LOW - Confusing API

4. **⚠️ No Multi-Tenancy Validation**
   - Accepts `@TenantId` but doesn't validate atom ownership
   - Missing: `WHERE TenantId = @TenantId` in atom lookups
   - **Impact:** MEDIUM - Potential cross-tenant data leakage

5. **⚠️ No Result Verification**
   - Checks `@GenerationStreamId` for NULL but doesn't verify stream contents
   - **Impact:** LOW - MUST return empty stream as success

### REQUIRED FIXES

**Priority 1 (BLOCKING):**
- Create `dbo.fn_GenerateWithAttention` CLR function
- Create `dbo.AttentionGenerationLog` table with schema:
  ```sql
  CREATE TABLE dbo.AttentionGenerationLog (
      LogId BIGINT IDENTITY PRIMARY KEY,
      ModelId INT NOT NULL,
      InputAtomIds NVARCHAR(MAX),
      ContextJson NVARCHAR(MAX),
      MaxTokens INT,
      Temperature FLOAT,
      TopK INT,
      TopP FLOAT,
      AttentionHeads INT,
      GenerationStreamId BIGINT,
      DurationMs INT,
      TenantId INT,
      CreatedAt DATETIME2(7) DEFAULT SYSUTCDATETIME()
  );
  ```

**URGENT:**
- Add tenant validation:
  ```sql
  IF NOT EXISTS (SELECT 1 FROM Atom WHERE AtomId IN (
      SELECT value FROM STRING_SPLIT(@InputAtomIds, ',')
  ) AND TenantId = @TenantId)
  BEGIN
      RAISERROR('Atoms not found for tenant', 16, 1);
      RETURN;
  END
  ```

---

## 2. PROCEDURE: dbo.sp_ExtractMetadata

**File:** `Procedures/dbo.sp_ExtractMetadata.sql`  
**Lines:** 66  
**Purpose:** Extract basic metadata from atoms based on modality  

**Quality Score: 75/100** ✅

### Schema Analysis

```sql
CREATE PROCEDURE dbo.sp_ExtractMetadata
    @AtomId BIGINT,
    @TenantId INT = 0
```

**Dependencies:**
- ✅ `dbo.Atom` table - EXISTS
- ✅ Uses `JSON_MODIFY` for metadata updates

**Implementation Pattern:**
```sql
-- Load atom data
SELECT @Modality = Modality, @CanonicalText = CanonicalText
FROM dbo.Atom WHERE AtomId = @AtomId AND TenantId = @TenantId;

-- Extract modality-specific metadata
IF @Modality = 'text' AND @CanonicalText IS NOT NULL
BEGIN
    -- Count words, characters
    SET @ExtractedMetadata = JSON_OBJECT(...);
END

-- Update atom metadata
UPDATE dbo.Atom
SET Metadata = JSON_MODIFY(ISNULL(Metadata, '{}'), '$.extracted', @ExtractedMetadata)
WHERE AtomId = @AtomId AND TenantId = @TenantId;
```

### Issues Found

1. **⚠️ Limited Modality Support**
   - Only implements text modality extraction
   - Other modalities get generic "requires payload loading" message
   - **Impact:** MEDIUM - Image/audio/code metadata not extracted

2. **⚠️ Simplistic Text Analysis**
   - Word count: `LEN(@CanonicalText) - LEN(REPLACE(@CanonicalText, ' ', '')) + 1`
   - Hardcoded language: `'en'`
   - No actual language detection
   - **Impact:** LOW - Inaccurate word counts, wrong language detection

3. **✅ Good: Multi-Tenancy**
   - Properly filters by `TenantId` in SELECT and UPDATE
   - Prevents cross-tenant metadata leakage

4. **✅ Good: JSON Metadata Pattern**
   - Uses `JSON_MODIFY` to append extracted metadata
   - Preserves existing metadata via `ISNULL(Metadata, '{}')`

### REQUIRED FIXES

**CRITICAL:**
- Implement image metadata extraction:
  ```sql
  IF @Modality = 'image'
  BEGIN
      DECLARE @Width INT, @Height INT;
      SELECT @Width = JSON_VALUE(Metadata, '$.width'),
             @Height = JSON_VALUE(Metadata, '$.height')
      FROM Atom WHERE AtomId = @AtomId;
      
      SET @ExtractedMetadata = JSON_OBJECT(
          'width': @Width,
          'height': @Height,
          'totalPixels': @Width * @Height,
          'extractedAt': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ss.fffZ')
      );
  END
  ```

**URGENT:**
- Add language detection for text (call CLR function or external service)
- Improve word count algorithm (handle multiple spaces, newlines)

---

## 3. PROCEDURE: dbo.sp_ComputeAllSemanticFeatures

**File:** `Procedures/dbo.sp_ComputeAllSemanticFeatures.sql`  
**Lines:** 69  
**Purpose:** Batch compute semantic features for all text embeddings  

**Quality Score: 72/100** ✅

### Schema Analysis

```sql
CREATE PROCEDURE dbo.sp_ComputeAllSemanticFeatures
```

**Dependencies:**
- ✅ `dbo.AtomEmbedding` table - EXISTS
- ✅ `dbo.Atom` table - EXISTS
- ✅ `dbo.sp_ComputeSemanticFeatures` procedure - **VERIFY** ⚠️
- ✅ `dbo.SemanticFeatures` table - **MISSING** ❌

**Implementation Pattern:**
```sql
DECLARE cursor_embeddings CURSOR FOR
    SELECT ae.AtomEmbeddingId
    FROM dbo.AtomEmbedding AS ae
    INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
    WHERE a.Modality = 'text' AND a.AtomicValue IS NOT NULL;

OPEN cursor_embeddings;
FETCH NEXT FROM cursor_embeddings INTO @atom_embedding_id;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.sp_ComputeSemanticFeatures @atom_embedding_id;
    -- Progress tracking every 100 rows
    FETCH NEXT FROM cursor_embeddings INTO @atom_embedding_id;
END;
```

### Issues Found

1. **❌ BLOCKING: Missing SemanticFeatures Table**
   - Final SELECT queries `dbo.SemanticFeatures` table
   - Table doesn't exist (not found in schema)
   - **Impact:** CRITICAL - Procedure fails on final aggregation

2. **❌ BLOCKING: Missing Child Procedure**
   - Calls `sp_ComputeSemanticFeatures` which will not exist
   - Need to verify existence and schema
   - **Impact:** CRITICAL - Cursor loop fails

3. **⚠️ Performance: CURSOR Pattern**
   - Uses CURSOR for batch processing (row-by-row)
   - MUST be replaced with set-based INSERT...SELECT
   - **Impact:** HIGH - Slow for large datasets (100K+ embeddings)

4. **⚠️ No Error Handling**
   - If `sp_ComputeSemanticFeatures` fails, cursor continues
   - No TRY/CATCH block
   - **Impact:** MEDIUM - Silent failures, incomplete results

5. **✅ Good: Progress Tracking**
   - Prints progress every 100 embeddings
   - Helps monitor long-running batches

6. **✅ Good: Modality Filter**
   - Correctly filters `WHERE a.Modality = 'text'`
   - Uses atomic decomposition pattern

### REQUIRED FIXES

**Priority 1 (BLOCKING):**
- Create `dbo.SemanticFeatures` table:
  ```sql
  CREATE TABLE dbo.SemanticFeatures (
      AtomEmbeddingId BIGINT PRIMARY KEY,
      TopicTechnical REAL,
      TopicBusiness REAL,
      TopicScientific REAL,
      TopicCreative REAL,
      Sentiment REAL,
      Complexity REAL,
      CreatedAt DATETIME2(7) DEFAULT SYSUTCDATETIME(),
      CONSTRAINT FK_SemanticFeatures_AtomEmbedding
          FOREIGN KEY (AtomEmbeddingId) REFERENCES AtomEmbedding(AtomEmbeddingId)
  );
  ```

**URGENT:**
- Replace CURSOR with set-based operation:
  ```sql
  INSERT INTO SemanticFeatures (AtomEmbeddingId, TopicTechnical, ...)
  SELECT ae.AtomEmbeddingId,
         dbo.fn_ComputeTopic(ae.EmbeddingVector, 'technical') AS TopicTechnical,
         ...
  FROM AtomEmbedding ae
  JOIN Atom a ON ae.AtomId = a.AtomId
  WHERE a.Modality = 'text' AND a.AtomicValue IS NOT NULL;
  ```

**REQUIRED:**
- Add error handling with TRY/CATCH
- Log failed embedding IDs to error table

---

## 4. PROCEDURE: dbo.sp_TokenizeText

**File:** `Procedures/dbo.sp_TokenizeText.sql`  
**Lines:** 50  
**Purpose:** Tokenize text into token IDs using vocabulary lookup  

**Quality Score: 80/100** ✅

### Schema Analysis

```sql
CREATE PROCEDURE dbo.sp_TokenizeText
    @text NVARCHAR(MAX),
    @tokenIdsJson NVARCHAR(MAX) OUTPUT
```

**Dependencies:**
- ✅ `dbo.TokenVocabulary` table - **VERIFY** ⚠️
- ✅ Uses `STRING_SPLIT` for whitespace tokenization
- ✅ Uses `FOR JSON PATH` for output

**Implementation Pattern:**
```sql
-- Normalize: lowercase, remove punctuation
DECLARE @normalized NVARCHAR(MAX) = LOWER(@text);
SET @normalized = TRANSLATE(@normalized, @punctuation, REPLICATE(' ', LEN(@punctuation)));

-- Split tokens, preserve order
INSERT INTO @OrderedTokens (TokenValue)
SELECT LTRIM(RTRIM(value))
FROM STRING_SPLIT(@normalized, ' ', 1);

-- Lookup tokens in vocabulary
SELECT @tokenIdsJson = (
    SELECT tv.TokenId
    FROM @OrderedTokens ot
    JOIN dbo.TokenVocabulary tv ON ot.TokenValue = tv.Token
    ORDER BY ot.OrderId
    FOR JSON PATH
);
```

### Issues Found

1. **⚠️ Missing TokenVocabulary Table**
   - References `dbo.TokenVocabulary` which will not exist
   - Need to verify table schema
   - **Impact:** MEDIUM - Procedure fails if table missing

2. **⚠️ Lossy Normalization**
   - Removes ALL punctuation via `TRANSLATE`
   - Loses semantic meaning (e.g., "can't" → "can t")
   - **Impact:** MEDIUM - Inaccurate tokenization for contractions, URLs

3. **⚠️ No Unknown Token Handling**
   - Tokens not in vocabulary are silently dropped
   - Missing: UNK/OOV token handling
   - **Impact:** MEDIUM - Information loss for rare words

4. **✅ Good: Order Preservation**
   - Uses `STRING_SPLIT(@normalized, ' ', 1)` with enable_ordinal=1
   - `IDENTITY(1,1)` preserves token order
   - Correct `ORDER BY ot.OrderId` in final output

5. **✅ Good: NULL Handling**
   - Returns `'[]'` for empty input
   - Handles NULL gracefully

### REQUIRED FIXES

**CRITICAL:**
- Verify `TokenVocabulary` table exists, or create:
  ```sql
  CREATE TABLE dbo.TokenVocabulary (
      TokenId INT IDENTITY PRIMARY KEY,
      Token NVARCHAR(100) UNIQUE NOT NULL,
      Frequency BIGINT DEFAULT 0,
      CreatedAt DATETIME2(7) DEFAULT SYSUTCDATETIME()
  );
  ```

**URGENT:**
- Add unknown token handling:
  ```sql
  DECLARE @UNK_TOKEN_ID INT = 0;  -- Reserved for unknown tokens
  
  SELECT @tokenIdsJson = (
      SELECT ISNULL(tv.TokenId, @UNK_TOKEN_ID) AS TokenId
      FROM @OrderedTokens ot
      LEFT JOIN dbo.TokenVocabulary tv ON ot.TokenValue = tv.Token
      ORDER BY ot.OrderId
      FOR JSON PATH
  );
  ```

**REQUIRED:**
- Improve normalization (preserve contractions, URLs)
- IMPLEMENT subword tokenization (BPE, WordPiece) for OOV handling

---

## 5. PROCEDURE: dbo.sp_AtomizeText_Governed

**File:** `Procedures/dbo.sp_AtomizeText_Governed.sql`  
**Lines:** 208  
**Purpose:** Governed text atomization with chunking, resumability, quota enforcement  

**Quality Score: 85/100** ✅

### Schema Analysis

```sql
CREATE PROCEDURE [dbo].[sp_AtomizeText_Governed]
    @IngestionJobId BIGINT,
    @TextData NVARCHAR(MAX)
```

**Dependencies:**
- ✅ `dbo.IngestionJobs` table - **VERIFY** ⚠️
- ✅ `dbo.Atom` table - EXISTS
- ✅ `dbo.AtomComposition` table - EXISTS

**Implementation Pattern:**
```sql
-- Load job state (resumability)
SELECT @JobStatus, @AtomChunkSize, @CurrentAtomOffset, @AtomQuota, @TenantId
FROM dbo.IngestionJobs WHERE IngestionJobId = @IngestionJobId;

-- State machine loop
WHILE (1 = 1)
BEGIN
    -- Governance check
    IF @TotalAtomsProcessed > @AtomQuota
        RAISERROR('Atom quota exceeded.', 16, 1);
    
    -- Tokenize chunk
    -- (Simplified whitespace tokenization - production uses CLR)
    
    -- Deduplicate tokens
    MERGE Atom AS T USING #UniqueTokens AS S
    ON T.ContentHash = S.ContentHash
    WHEN NOT MATCHED THEN INSERT (Modality, Subtype, ContentHash, AtomicValue, TenantId)
    VALUES ('text', 'token', S.ContentHash, S.AtomicValue, @TenantId);
    
    -- Build AtomComposition with spatial keys
    INSERT INTO AtomComposition (ParentAtomId, ComponentAtomId, SequenceIndex, SpatialKey)
    SELECT @ParentAtomId, tta.AtomId, ct.SequenceIndex,
           geometry::Point(ct.SequenceIndex, tta.AtomId % 10000, 0)
    FROM #ChunkTokens ct JOIN #TokenToAtomId tta ...;
    
    -- Update progress
    UPDATE IngestionJobs SET CurrentAtomOffset = @CurrentAtomOffset + @AtomChunkSize;
END;
```

### Issues Found

1. **⚠️ Simplified Tokenization**
   - Uses whitespace split: `CHARINDEX(' ', @ChunkText, @Pos)`
   - Comment says "production would use proper tokenizer"
   - **Impact:** MEDIUM - Placeholder implementation, not production-ready

2. **⚠️ Spatial Key Scaling**
   - Uses `tta.AtomId % 10000` for Y dimension
   - Modulo can cause collisions, poor spatial distribution
   - **Impact:** LOW - Suboptimal spatial index performance

3. **✅ EXCELLENT: Resumability Pattern**
   - Tracks `CurrentAtomOffset` for resume after failure
   - State machine loop with checkpoint updates
   - Chunked processing (`@AtomChunkSize`)

4. **✅ EXCELLENT: Governance**
   - Enforces `@AtomQuota` to prevent runaway ingestion
   - Updates job status: 'Processing', 'Complete', 'Failed'
   - Error logging via `ErrorMessage` column

5. **✅ EXCELLENT: Deduplication**
   - Uses `MERGE` on `ContentHash` to deduplicate tokens
   - Increments `ReferenceCount` for shared atoms
   - Correct atomic pattern

6. **✅ EXCELLENT: Multi-Tenancy**
   - Retrieves `@TenantId` from job
   - Inserts atoms with `TenantId` for isolation

### REQUIRED FIXES

**CRITICAL:**
- Replace whitespace tokenization with CLR tokenizer:
  ```sql
  -- Call CLR function for proper tokenization
  EXEC dbo.clr_TokenizeText @ChunkText, @TokensTable OUTPUT;
  ```

**URGENT:**
- Improve spatial key generation:
  ```sql
  -- Use hash-based Y dimension for better distribution
  geometry::Point(
      ct.SequenceIndex,  -- X = Position
      HASHBYTES('SHA2_256', CAST(tta.AtomId AS VARBINARY)) % 10000,  -- Y = Hash
      0  -- Z = unused
  )
  ```

**REQUIRED:**
- Add parallel chunk processing (Service Broker queue)
- Add resumability testing (simulate failures mid-job)

---

## 6. PROCEDURE: dbo.sp_ReconstructText

**File:** `StoredProcedures/dbo.sp_ReconstructText.sql`  
**Lines:** 50  
**Purpose:** Reconstruct text from atomic tokens using spatial ordering  

**Quality Score: 78/100** ✅

### Schema Analysis

```sql
CREATE PROCEDURE [dbo].[sp_ReconstructText]
    @textAtomId BIGINT,
    @startPosition INT = 0,
    @length INT = NULL
```

**Dependencies:**
- ✅ `dbo.Atom` table - EXISTS
- ✅ `dbo.AtomCompositions` table - **MUST BE** `AtomComposition` (singular) ❌
- ⚠️ Uses `DimensionX` column - **DEPRECATED** (MUST use `SequenceIndex`)

**Implementation Pattern:**
```sql
-- Get total length from metadata
SELECT @totalLength = JSON_VALUE(Metadata, '$.length')
FROM dbo.Atom WHERE AtomId = @textAtomId;

-- Reconstruct text in order
SELECT 
    ac.DimensionX AS Position,
    a.CanonicalText AS Character,
    STRING_AGG(a.CanonicalText, '') WITHIN GROUP (ORDER BY ac.DimensionX) 
        OVER (ORDER BY ac.DimensionX ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CumulativeText
FROM dbo.AtomCompositions ac
JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
WHERE ac.SourceAtomId = @textAtomId
  AND ac.ComponentType = 'text-token'
  AND ac.DimensionX >= @startPosition
ORDER BY ac.DimensionX;
```

### Issues Found

1. **❌ CRITICAL: Wrong Table Name**
   - References `dbo.AtomCompositions` (plural)
   - Actual table: `dbo.AtomComposition` (singular)
   - **Impact:** CRITICAL - Procedure fails at runtime

2. **❌ CRITICAL: Deprecated Schema**
   - Uses `DimensionX`, `SourceAtomId`, `ComponentType` columns
   - AtomComposition schema: `ParentAtomId`, `ComponentAtomId`, `SequenceIndex`
   - **Impact:** CRITICAL - Schema mismatch, columns don't exist

3. **⚠️ Inefficient Windowing**
   - Uses `STRING_AGG ... OVER (ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)`
   - Recalculates cumulative text for every row
   - **Impact:** MEDIUM - O(N²) performance for long texts

4. **✅ Good: Pagination Support**
   - Supports `@startPosition` and `@length` for substring extraction
   - Useful for large documents

### REQUIRED FIXES

**Priority 1 (BLOCKING):**
- Fix schema to match AtomComposition:
  ```sql
  SELECT 
      ac.SequenceIndex AS Position,
      a.CanonicalText AS Character,
      STRING_AGG(a.CanonicalText, '') WITHIN GROUP (ORDER BY ac.SequenceIndex) AS ReconstructedText
  FROM dbo.AtomComposition ac
  JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
  WHERE ac.ParentAtomId = @textAtomId
    AND ac.SequenceIndex >= @startPosition
    AND ac.SequenceIndex < (@startPosition + @length)
  ORDER BY ac.SequenceIndex;
  ```

**URGENT:**
- Remove inefficient windowing, use simple `STRING_AGG`:
  ```sql
  SELECT STRING_AGG(a.CanonicalText, '') WITHIN GROUP (ORDER BY ac.SequenceIndex) AS Text
  FROM dbo.AtomComposition ac
  JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
  WHERE ac.ParentAtomId = @textAtomId ...;
  ```

---

## 7. PROCEDURE: dbo.sp_ReconstructImage

**File:** `StoredProcedures/dbo.sp_ReconstructImage.sql`  
**Lines:** 60  
**Purpose:** Reconstruct image from atomic pixels using spatial ordering  

**Quality Score: 70/100** ⚠️

### Schema Analysis

```sql
CREATE PROCEDURE [dbo].[sp_ReconstructImage]
    @imageAtomId BIGINT,
    @includeMetadata BIT = 0
```

**Dependencies:**
- ✅ `dbo.Atom` table - EXISTS
- ✅ `dbo.AtomCompositions` table - **WRONG** (MUST be `AtomComposition`)
- ❌ `dbo.AtomicPixels` table - **MISSING** ❌
- ⚠️ Uses `DimensionX`, `DimensionY`, `ComponentType` - **DEPRECATED**

### Issues Found

1. **❌ CRITICAL: Wrong Table Names**
   - References `AtomCompositions` (plural) - MUST be `AtomComposition`
   - References `AtomicPixels` table which doesn't exist
   - **Impact:** CRITICAL - Procedure fails at runtime

2. **❌ CRITICAL: Deprecated Schema**
   - Uses `DimensionX`, `DimensionY`, `SourceAtomId`, `ComponentType`
   - AtomComposition doesn't have these columns
   - **Impact:** CRITICAL - Schema mismatch

3. **❌ CRITICAL: Missing AtomicPixels Table**
   - Joins `dbo.AtomicPixels p ON p.PixelHash = a.ContentHash`
   - Table doesn't exist in schema
   - **Impact:** CRITICAL - Cannot retrieve RGBA pixel data

4. **⚠️ Inefficient Pixel Storage**
   - Expects separate `AtomicPixels` table for pixel data
   - Violates atomic pattern (pixels MUST be in `Atom.AtomicValue`)
   - **Impact:** HIGH - Architectural violation (like CodeAtom)

### REQUIRED FIXES

**Priority 1 (BLOCKING):**
- Rewrite to use correct schema:
  ```sql
  SELECT 
      -- Extract X/Y from SpatialKey
      ac.SpatialKey.STX AS X,
      ac.SpatialKey.STY AS Y,
      -- Extract RGBA from Atom.AtomicValue (4 bytes)
      CAST(SUBSTRING(a.AtomicValue, 1, 1) AS INT) AS R,
      CAST(SUBSTRING(a.AtomicValue, 2, 1) AS INT) AS G,
      CAST(SUBSTRING(a.AtomicValue, 3, 1) AS INT) AS B,
      CAST(SUBSTRING(a.AtomicValue, 4, 1) AS INT) AS A
  FROM dbo.AtomComposition ac
  JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
  WHERE ac.ParentAtomId = @imageAtomId
    AND a.Modality = 'image'
    AND a.Subtype = 'rgba-pixel'
  ORDER BY ac.SpatialKey.STY, ac.SpatialKey.STX;  -- Row-major order
  ```

**URGENT:**
- Delete `AtomicPixels` table if it exists (architectural violation)
- All pixel data MUST be in `Atom.AtomicValue` (max 64 bytes = 16 pixels RGBA)

---

## 8. VIEW: dbo.vw_ModelPerformance

**File:** `Views/dbo.vw_ModelPerformance.sql`  
**Lines:** 20  
**Purpose:** Consumer-friendly model performance metrics (wrapper over indexed view)  

**Quality Score: 82/100** ✅

### Schema Analysis

```sql
CREATE VIEW [dbo].[vw_ModelPerformance]
AS
SELECT 
    ModelId, ModelName,
    ISNULL(TotalInferences, 0) AS TotalInferences,
    LastUsed,
    CASE WHEN CountInferenceTimeMs > 0 
        THEN SumInferenceTimeMs / CountInferenceTimeMs 
        ELSE 0.0 
    END AS AvgInferenceTimeMs,
    CASE WHEN CountLayers > 0 
        THEN SumCacheHitRate / CountLayers 
        ELSE 0.0 
    END AS CacheHitRate,
    CAST(0.0 AS FLOAT) AS AvgConfidenceScore,  -- Placeholder
    CAST(0 AS BIGINT) AS TotalTokensGenerated  -- Placeholder
FROM dbo.vw_ModelPerformanceMetrics;
```

**Dependencies:**
- ✅ `dbo.vw_ModelPerformanceMetrics` (indexed view) - **VERIFY** ⚠️

### Issues Found

1. **⚠️ Missing Base View**
   - References `dbo.vw_ModelPerformanceMetrics` which will not exist
   - Need to verify indexed view exists
   - **Impact:** MEDIUM - View fails if base missing

2. **⚠️ Hardcoded Placeholders**
   - `AvgConfidenceScore` = 0.0 (not implemented)
   - `TotalTokensGenerated` = 0 (not implemented)
   - **Impact:** LOW - Missing metrics, misleading to API consumers

3. **✅ Good: Divide-by-Zero Protection**
   - `CASE WHEN CountInferenceTimeMs > 0 THEN ...`
   - Prevents divide-by-zero errors

4. **✅ Good: Wrapper Pattern**
   - Comment explains this wraps indexed view
   - Controllers use this view, not the base indexed view directly
   - Correct separation of concerns

### REQUIRED FIXES

**CRITICAL:**
- Verify `vw_ModelPerformanceMetrics` exists, or create materialized view
- Implement missing metrics (AvgConfidenceScore, TotalTokensGenerated)

**URGENT:**
- Add view documentation:
  ```sql
  -- Purpose: Consumer-friendly model performance metrics
  -- Dependencies: vw_ModelPerformanceMetrics (indexed view)
  -- Used by: API Controllers (ModelsController.GetPerformance)
  ```

---

## 9. VIEW: dbo.vw_ModelDetails

**File:** `Views/dbo.vw_ModelDetails.sql`  
**Lines:** 24  
**Purpose:** Model detail view with metadata and layer count  

**Quality Score: 88/100** ✅

### Schema Analysis

```sql
CREATE VIEW [dbo].[vw_ModelDetails]
WITH SCHEMABINDING
AS
SELECT 
    m.ModelId, m.ModelName, m.ModelType, m.ParameterCount,
    m.IngestionDate, m.Architecture, m.UsageCount, m.LastUsed,
    mm.SupportedTasks, mm.SupportedModalities,
    mm.MaxInputLength, mm.MaxOutputLength, mm.EmbeddingDimension,
    (SELECT COUNT_BIG(*) FROM dbo.ModelLayer ml WHERE ml.ModelId = m.ModelId) AS LayerCount
FROM dbo.Model m
LEFT JOIN dbo.ModelMetadata mm ON mm.ModelId = m.ModelId;
```

**Dependencies:**
- ✅ `dbo.Model` table - EXISTS (verified in Part 2)
- ✅ `dbo.ModelMetadata` table - **VERIFY** ⚠️
- ✅ `dbo.ModelLayer` table - EXISTS (verified in Part 2)

### Issues Found

1. **⚠️ Missing ModelMetadata Table**
   - LEFT JOINs `dbo.ModelMetadata` which will not exist
   - Need to verify table schema
   - **Impact:** MEDIUM - Metadata columns will be NULL if table missing

2. **⚠️ Correlated Subquery Performance**
   - Uses `(SELECT COUNT_BIG(*) FROM ModelLayer WHERE ModelId = m.ModelId)`
   - Executes once per model row
   - **Impact:** MEDIUM - Slower than LEFT JOIN with GROUP BY

3. **✅ EXCELLENT: WITH SCHEMABINDING**
   - Enables indexed views
   - Prevents underlying table schema changes
   - Query optimizer can use view statistics

4. **✅ Good: LEFT JOIN**
   - Uses LEFT JOIN for optional metadata
   - Models without metadata still appear in results

### REQUIRED FIXES

**CRITICAL:**
- Verify `ModelMetadata` table exists, or create:
  ```sql
  CREATE TABLE dbo.ModelMetadata (
      ModelId INT PRIMARY KEY,
      SupportedTasks NVARCHAR(MAX),
      SupportedModalities NVARCHAR(MAX),
      MaxInputLength INT,
      MaxOutputLength INT,
      EmbeddingDimension INT,
      CONSTRAINT FK_ModelMetadata_Model FOREIGN KEY (ModelId) REFERENCES Model(ModelId)
  );
  ```

**URGENT:**
- Optimize layer count (if view becomes indexed):
  ```sql
  -- Option 1: Indexed view with GROUP BY
  CREATE VIEW vw_ModelDetails WITH SCHEMABINDING AS
  SELECT m.ModelId, ..., COUNT_BIG(ml.LayerId) AS LayerCount
  FROM dbo.Model m
  LEFT JOIN dbo.ModelMetadata mm ON mm.ModelId = m.ModelId
  LEFT JOIN dbo.ModelLayer ml ON ml.ModelId = m.ModelId
  GROUP BY m.ModelId, m.ModelName, ...;
  
  -- Option 2: Computed column in Model table
  ALTER TABLE Model ADD LayerCount AS (
      SELECT COUNT_BIG(*) FROM ModelLayer WHERE ModelId = Model.ModelId
  ) PERSISTED;
  ```

---

## 10. FUNCTION: dbo.fn_VectorCosineSimilarity

**File:** `Functions/dbo.fn_VectorCosineSimilarity.sql`  
**Lines:** 14  
**Purpose:** Calculate cosine similarity between two embedding vectors  

**Quality Score: 92/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_VectorCosineSimilarity(
    @vec1 VECTOR(1998),
    @vec2 VECTOR(1998)
)
RETURNS FLOAT
AS
BEGIN
    IF @vec1 IS NULL OR @vec2 IS NULL
        RETURN NULL;

    RETURN 1.0 - VECTOR_DISTANCE('cosine', @vec1, @vec2);
END;
```

**Dependencies:**
- ✅ SQL Server 2025+ `VECTOR` data type - **REQUIRES** SQL Server 2025 ❌
- ✅ Built-in `VECTOR_DISTANCE` function

### Issues Found

1. **❌ BLOCKING: SQL Server 2025 Dependency**
   - Uses `VECTOR(1998)` data type (SQL Server 2025+)
   - Hartonomous will be targeting SQL Server 2019/2022
   - **Impact:** CRITICAL - Function won't compile on older versions

2. **⚠️ No Vector Validation**
   - Assumes vectors are same dimension (1998)
   - Doesn't validate vector normalization (L2 norm = 1)
   - **Impact:** LOW - VECTOR_DISTANCE handles dimension mismatch

3. **✅ EXCELLENT: NULL Safety**
   - Returns NULL for NULL inputs
   - Prevents downstream errors

4. **✅ EXCELLENT: Built-in Function**
   - Uses native `VECTOR_DISTANCE` (optimized C++ implementation)
   - Avoids T-SQL loops for vector math
   - Correct conversion: `1.0 - cosine_distance = cosine_similarity`

### REQUIRED FIXES

**Priority 1 (BLOCKING):**
- Verify SQL Server version compatibility
- If SQL Server < 2025, use VARBINARY fallback:
  ```sql
  CREATE FUNCTION dbo.fn_VectorCosineSimilarity(
      @vec1 VARBINARY(8000),  -- 2000 floats × 4 bytes
      @vec2 VARBINARY(8000)
  )
  RETURNS FLOAT
  AS EXTERNAL NAME [SqlClrFunctions].[UserDefinedFunctions].[CosineSimilarity];
  -- Implement in CLR using optimized SIMD
  ```

**URGENT:**
- Add version comment:
  ```sql
  -- REQUIRES: SQL Server 2025+ for VECTOR data type
  -- Fallback: Use CLR function for SQL Server 2019/2022
  ```

---

## 11. FUNCTION: dbo.fn_ComputeSpatialBucket

**File:** `Functions/dbo.fn_ComputeSpatialBucket.sql`  
**Lines:** 16  
**Purpose:** Compute locality-sensitive hash for spatial bucketing (R-Tree optimization)  

**Quality Score: 90/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_ComputeSpatialBucket (
    @X FLOAT,
    @Y FLOAT,
    @Z FLOAT
)
RETURNS BIGINT
AS
BEGIN
    -- Locality-sensitive hash for spatial bucketing
    -- Buckets are 0.01 units (1% of normalized space)
    RETURN (
        (CAST(FLOOR(@X * 100) AS BIGINT) * 1000000) +
        (CAST(FLOOR(@Y * 100) AS BIGINT) * 1000) +
        (CAST(FLOOR(@Z * 100) AS BIGINT))
    );
END
```

**Dependencies:**
- None (pure scalar function)

**Used By:**
- ✅ Spatial indexing queries (R-Tree partitioning)
- ✅ Locality-sensitive hashing for KNN

### Issues Found

1. **⚠️ Limited to 100×100×100 Grid**
   - `@X * 100` → 100 buckets per dimension
   - `@Y * 100` → 100 buckets per dimension
   - `@Z * 100` → 100 buckets per dimension
   - Total: 1,000,000 buckets
   - **Impact:** LOW - will be insufficient for dense embeddings (1,998D)

2. **⚠️ Assumes Normalized Input**
   - Expects X, Y, Z in [0, 1] range
   - No validation for out-of-range values
   - **Impact:** LOW - Negative values cause incorrect buckets

3. **⚠️ Overflow Risk**
   - Max bucket ID: `(99 * 1000000) + (99 * 1000) + 99 = 99,099,099`
   - BIGINT max: 9,223,372,036,854,775,807
   - **Impact:** NONE - No overflow risk for 100×100×100 grid

4. **✅ EXCELLENT: Locality Preservation**
   - Nearby points → same bucket
   - Correct LSH implementation (grid-based)
   - Supports fast range queries

5. **✅ Good: Documentation**
   - Comments explain bucket size (0.01 units = 1%)
   - Clear formula

### REQUIRED FIXES

**CRITICAL:**
- Add input validation:
  ```sql
  IF @X < 0 OR @X > 1 OR @Y < 0 OR @Y > 1 OR @Z < 0 OR @Z > 1
      RETURN NULL;  -- Out of range
  ```

**URGENT:**
- IMPLEMENT adaptive bucket size based on data distribution:
  ```sql
  -- For high-dimensional embeddings, use finer granularity
  -- Buckets are 0.001 units (0.1%)
  RETURN (
      (CAST(FLOOR(@X * 1000) AS BIGINT) * 1000000000) +
      (CAST(FLOOR(@Y * 1000) AS BIGINT) * 1000000) +
      (CAST(FLOOR(@Z * 1000) AS BIGINT))
  );
  ```

---

## SUMMARY & CUMULATIVE FINDINGS

### Files Analyzed

**Part 7 Total:** 11 files  
**Cumulative (Parts 1-7):** 56 of 315+ files (17.8%)

**Average Quality Score This Part:** 80.0/100  
**Cumulative Average (Parts 1-7):** 80.5/100

### Quality Distribution

| Score Range | Count | Files |
|-------------|-------|-------|
| 90-100 | 2 | fn_ComputeSpatialBucket (90), fn_VectorCosineSimilarity (92) |
| 80-89 | 4 | sp_TokenizeText (80), vw_ModelPerformance (82), sp_AtomizeText_Governed (85), vw_ModelDetails (88) |
| 70-79 | 3 | sp_ExtractMetadata (75), sp_ComputeAllSemanticFeatures (72), sp_ReconstructText (78), sp_ReconstructImage (70) |
| 60-69 | 1 | sp_GenerateWithAttention (68) |
| Below 60 | 0 | — |

### Critical Issues Found (BLOCKING)

1. **sp_GenerateWithAttention (2 blockers)**
   - Missing CLR function: `dbo.fn_GenerateWithAttention`
   - Missing table: `dbo.AttentionGenerationLog`

2. **sp_ComputeAllSemanticFeatures (2 blockers)**
   - Missing table: `dbo.SemanticFeatures`
   - Missing procedure: `dbo.sp_ComputeSemanticFeatures` (verify)

3. **sp_ReconstructText (2 blockers)**
   - Wrong table name: `AtomCompositions` (plural) → `AtomComposition`
   - Deprecated schema: `DimensionX`, `SourceAtomId`, `ComponentType`

4. **sp_ReconstructImage (3 blockers)**
   - Wrong table name: `AtomCompositions` (plural)
   - Missing table: `dbo.AtomicPixels`
   - Deprecated schema: `DimensionX`, `DimensionY`, `ComponentType`

5. **fn_VectorCosineSimilarity (1 blocker)**
   - Requires SQL Server 2025+ for `VECTOR` data type
   - will not compile on SQL Server 2019/2022

**Total Blockers This Part:** 10

### Architectural Findings

#### ✅ CORRECT PATTERNS OBSERVED

1. **sp_AtomizeText_Governed** - **EXCELLENT EXAMPLE**
   - Uses `Atom` table with `Modality='text', Subtype='token'`
   - Correct atomic decomposition pattern
   - Multi-tenancy via `TenantId`
   - Resumable chunked processing
   - Quota enforcement
   - Deduplication via `MERGE` on `ContentHash`

2. **View Design Patterns**
   - `vw_ModelPerformance`: Wrapper pattern (consumer-friendly)
   - `vw_ModelDetails`: WITH SCHEMABINDING (indexed view optimization)

3. **Spatial Bucketing**
   - `fn_ComputeSpatialBucket`: Locality-sensitive hashing for R-Tree

#### ❌ ARCHITECTURAL VIOLATIONS

1. **AtomicPixels Table (sp_ReconstructImage)**
   - Separate table for pixel RGBA data
   - **SAME ANTI-PATTERN AS CodeAtom**
   - Violates atomic decomposition pattern
   - **MUST use:** `Atom.AtomicValue` (4 bytes RGBA)
   - **Impact:** Breaks modality uniformity, prevents cross-modal queries

2. **Schema Drift**
   - Multiple procedures use deprecated schema:
     - `AtomCompositions` (plural) → MUST be `AtomComposition`
     - `DimensionX/Y/Z`, `SourceAtomId`, `ComponentType` → MUST be `SequenceIndex`, `ParentAtomId`
   - **Root Cause:** Incomplete schema migration (likely 2-3 schema versions exist)
   - **Impact:** HIGH - Procedures fail at runtime

### Missing Objects Identified

**New Additions to Missing List:**
1. `dbo.fn_GenerateWithAttention` (CLR) - Referenced by sp_GenerateWithAttention
2. `dbo.AttentionGenerationLog` (table) - Logging table for attention generation
3. `dbo.SemanticFeatures` (table) - Stores topic scores, sentiment, complexity
4. `dbo.sp_ComputeSemanticFeatures` (procedure) - Called by sp_ComputeAllSemanticFeatures
5. `dbo.TokenVocabulary` (table) - Token → TokenId mapping (verify)
6. `dbo.ModelMetadata` (table) - Extended model attributes (verify)
7. `dbo.vw_ModelPerformanceMetrics` (view) - Base indexed view (verify)
8. `dbo.AtomicPixels` (table) - **MUST NOT EXIST** (architectural violation)

**Cumulative Missing Objects:**
- CLR Functions: 16+ (added fn_GenerateWithAttention)
- Tables: 18+ (added AttentionGenerationLog, SemanticFeatures, 6 to verify)
- Procedures: 5+ (added sp_ComputeSemanticFeatures, sp_EvictCacheLRU)
- Functions: 2 (fn_EstimateModelSize, fn_CalculateMemoryFootprint)
- Views: 1 to verify (vw_ModelPerformanceMetrics)

### Performance Observations

1. **Cursor Usage**
   - sp_ComputeAllSemanticFeatures uses CURSOR for batch processing
   - **required implementation:** Set-based INSERT...SELECT
   - **Impact:** O(N) vs O(N²) for large datasets

2. **Correlated Subqueries**
   - vw_ModelDetails: `(SELECT COUNT_BIG(*) FROM ModelLayer WHERE ...)`
   - **required implementation:** LEFT JOIN + GROUP BY for indexed views
   - **Impact:** MEDIUM - Executes once per row

3. **SQL Server 2025 VECTOR**
   - fn_VectorCosineSimilarity uses native VECTOR type
   - **Fallback needed:** CLR implementation for SQL Server < 2025

### Schema Migration Debt

**Discovered:** 2-3 schema versions coexist  
**Evidence:**
- `AtomComposition` (current) vs `AtomCompositions` (old)
- `SequenceIndex` (current) vs `DimensionX/Y/Z` (old)
- `ParentAtomId/ComponentAtomId` (current) vs `SourceAtomId/ComponentType` (old)

**Affected Procedures:**
- sp_ReconstructText (uses old schema)
- sp_ReconstructImage (uses old schema)
- Likely 10+ more procedures referencing old schema

**Migration Plan Needed:**
1. Audit all procedures for `AtomCompositions` references
2. Rewrite to use `AtomComposition` + `SequenceIndex`
3. Drop deprecated columns (or add INSTEAD OF triggers for backward compatibility)

---

## REQUIRED FIXES FOR NEXT STEPS

### Immediate Actions (This Week)

1. **Fix Blocking Issues (10 blockers)**
   - Create missing tables (AttentionGenerationLog, SemanticFeatures)
   - Verify missing objects (TokenVocabulary, ModelMetadata, vw_ModelPerformanceMetrics)
   - Implement missing CLR function (fn_GenerateWithAttention) or stub
   - Fix schema drift (AtomCompositions → AtomComposition)

2. **SQL Server Version Check**
   - Verify target SQL Server version (2019? 2022? 2025?)
   - If < 2025, create CLR fallback for fn_VectorCosineSimilarity

3. **Delete AtomicPixels Table**
   - If exists, migrate pixel data to `Atom.AtomicValue`
   - Same pattern as CodeAtom migration

### Medium-Term (This Month)

1. **Schema Migration Audit**
   - Search all procedures for `AtomCompositions` (plural)
   - Search for `DimensionX/Y/Z`, `SourceAtomId`, `ComponentType` usage
   - Create migration script to rewrite to current schema

2. **Performance Optimization**
   - Replace cursor in sp_ComputeAllSemanticFeatures with set-based
   - Optimize vw_ModelDetails (indexed view or computed column)

3. **Complete Missing Objects**
   - Implement sp_ComputeSemanticFeatures (child of sp_ComputeAllSemanticFeatures)
   - Create TokenVocabulary table (used by sp_TokenizeText)

### Long-Term (Next Quarter)

1. **Continue Manual Audit**
   - **Part 8:** Service Broker activation procedures, more views
   - **Part 9:** Remaining scalar functions (fn_SoftmaxTemperature, etc.)
   - **Parts 10-15:** 260+ remaining files

2. **Architectural Cleanup**
   - Consolidate schema versions (drop old columns)
   - Document deprecation timeline
   - Add schema validation tests

---

## CONTINUATION PLAN FOR PART 8

### Proposed Files for Part 8 (Target: 10-12 files)

1. Service Broker activation procedures (sp_ActivateService, etc.)
2. More views (vw_IngestionJobStatus, vw_AtomStatistics, etc.)
3. Remaining scalar functions (fn_SoftmaxTemperature, fn_SelectModelsForTask, etc.)
4. CLR functions documentation (if implementations exist)

**Target Lines:** 650-750  
**Target Quality:** Continue architectural depth analysis  
**Focus Areas:** Service Broker infrastructure, remaining utility functions

---

## ARCHITECTURAL LESSONS FROM PART 7

### What's Working Well ✅

1. **Atomic Decomposition Pattern**
   - sp_AtomizeText_Governed is a **gold standard** implementation
   - Correct use of `Atom` table with `Modality/Subtype`
   - Proper deduplication, multi-tenancy, governance

2. **View Layering**
   - Consumer-friendly wrappers (vw_ModelPerformance)
   - Performance-optimized base views (vw_ModelDetails WITH SCHEMABINDING)

3. **Spatial Bucketing**
   - Locality-sensitive hashing for R-Tree optimization
   - Correct LSH implementation

### What Needs Attention ⚠️

1. **Schema Drift**
   - Multiple schema versions coexist
   - Need migration audit + cleanup plan

2. **Missing Dependencies**
   - 10 blocking dependencies in this part alone
   - Need systematic dependency tracking

3. **Architectural Violations**
   - AtomicPixels table (if exists) = same anti-pattern as CodeAtom
   - Need comprehensive audit for content-specific tables

### Pattern Recognition

**✅ CORRECT:** `sp_AtomizeText_Governed`
```sql
-- Text tokenization → Atom table
INSERT INTO Atom (Modality, Subtype, AtomicValue, ContentHash, TenantId)
VALUES ('text', 'token', @token, HASHBYTES('SHA2_256', @token), @TenantId);
```

**❌ WRONG:** `sp_ReconstructImage` (if AtomicPixels exists)
```sql
-- Pixel data in separate table
SELECT * FROM AtomicPixels WHERE PixelHash = ...;
```

**✅ MUST BE:**
```sql
-- Pixel data in Atom.AtomicValue (4 bytes RGBA)
SELECT AtomicValue FROM Atom 
WHERE Modality='image' AND Subtype='rgba-pixel';
```

---

**END OF PART 7**

**Next:** SQL_AUDIT_PART8.md (Service Broker procedures, views, scalar functions)  
**Progress:** 56 of 315+ files (17.8%)  
**Estimated Completion:** 12-15 more parts (Parts 8-22)