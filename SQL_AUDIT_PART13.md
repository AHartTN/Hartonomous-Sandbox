# SQL Audit Part 13: Atomization & Generation Procedures

## Overview
Part 13 analyzes 5 procedures focusing on atomization (image, code, model) and text generation capabilities.

---

## 1. sp_AtomizeImage_Governed

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_AtomizeImage_Governed.sql`  
**Type:** Stored Procedure  
**Lines:** ~230  
**Quality Score:** 85/100

### Purpose
Governed, resumable image pixel atomization with XYZM structural storage. Implements chunked processing with quota enforcement.

### Parameters
- `@IngestionJobId BIGINT` - Job tracking ID
- `@ImageData VARBINARY(MAX)` - Raw image binary
- `@ImageWidth INT` - Image width in pixels
- `@ImageHeight INT` - Image height in pixels

### Architecture

**State Machine Loop:**
1. Load job state from `IngestionJobs`
2. Extract pixel chunk (RGBA values)
3. Deduplicate unique pixel values
4. MERGE into `Atom` table (Modality='image', Subtype='rgba-pixel')
5. Update reference counts
6. Store spatial structure in `AtomComposition` (X=PixelX, Y=PixelY, Z=unused)
7. Update job progress
8. Repeat until image complete

**Governance Features:**
- Atom quota enforcement (`@AtomQuota`)
- Resumable processing (`@CurrentAtomOffset`)
- Chunk size control (`@AtomChunkSize`)
- Transaction safety with rollback
- Error tracking in `IngestionJobs`

**Multi-Tenancy:** ✅ V3 upgrade - TenantId retrieved from job

### Key Operations

**Pixel Deduplication:**
```sql
MERGE [dbo].[Atom] AS T
USING #UniquePixels AS S
ON T.[ContentHash] = S.[ContentHash]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Modality], [Subtype], [ContentHash], [AtomicValue], [ReferenceCount], [TenantId])
    VALUES ('image', 'rgba-pixel', S.[ContentHash], S.[AtomicValue], 0, @TenantId);
```

**Spatial Structure Storage:**
```sql
INSERT INTO [dbo].[AtomComposition] (
    [ParentAtomId], [ComponentAtomId], [SequenceIndex], [SpatialKey]
)
SELECT 
    @ParentAtomId, pta.[AtomId], cp.[SequenceIndex],
    [sys].[geometry]::Point(cp.[PositionX], cp.[PositionY], 0)
FROM #ChunkPixels cp
JOIN #PixelToAtomId pta ON cp.[R] = pta.[R] AND cp.[G] = pta.[G] AND cp.[B] = pta.[B] AND cp.[A] = pta.[A];
```

### Dependencies
- Tables: `IngestionJobs`, `Atom`, `AtomComposition`
- CLR Functions: None (currently uses simplified T-SQL extraction - comment notes production would use CLR)
- Indexes: `IX_Atom_ContentHash`, spatial indexes on `AtomComposition`

### Quality Assessment

**Strengths:**
- ✅ **Excellent governance** - Full state machine with quota, resume, chunking
- ✅ **Proper deduplication** - ContentHash-based MERGE
- ✅ **Multi-tenancy support** - V3 upgrade complete
- ✅ **Transaction safety** - TRY/CATCH with rollback
- ✅ **Spatial structure** - GEOMETRY Point storage for reconstruction
- ✅ **Reference counting** - Tracks pixel reuse across images

**Weaknesses:**
- ⚠️ **Simplified pixel extraction** - Comment acknowledges production needs CLR for proper image decoding (PNG/JPEG/etc)
- ⚠️ **WHILE loop for pixel extraction** - Could be replaced with CLR streaming function
- ⚠️ **No color space handling** - Assumes raw RGBA, no sRGB/AdobeRGB metadata
- ⚠️ **Hardcoded Z=0** - Could use Z for layers/channels (alpha, depth maps)
- ⚠️ **Missing image metadata** - No EXIF, format, compression info stored

**Performance:**
- Chunk-based processing prevents large transactions
- ContentHash deduplication is O(1) with proper index
- Reference count updates are batched per chunk
- Spatial inserts could benefit from bulk operations

**Security:**
- ✅ Multi-tenant isolation via TenantId
- ⚠️ No validation of image data (malformed binary could cause issues)
- ✅ Quota enforcement prevents runaway ingestion

### Improvement Recommendations
1. **Priority 1:** Implement CLR image decoding function (PNG, JPEG, BMP, TIFF support)
2. **Priority 2:** Add image metadata storage (format, dimensions, color space, EXIF)
3. **Priority 3:** Use Z dimension for alpha channel or layer separation
4. **Priority 4:** Add image validation (magic bytes, size limits)
5. **Priority 5:** Consider spatial bucketing for very large images (tiling)

---

## 2. sp_AtomizeCode

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_AtomizeCode.sql`  
**Type:** Stored Procedure  
**Lines:** ~110  
**Quality Score:** 72/100

### Purpose
AST-as-GEOMETRY pipeline for source code ingestion. Parses code using Roslyn CLR, generates structural vector, projects to 3D GEOMETRY.

### Parameters
- `@AtomId BIGINT` - Existing Atom containing source code
- `@TenantId INT = 0` - Tenant isolation
- `@Language NVARCHAR(50) = 'csharp'` - Programming language (future: multiple languages)
- `@Debug BIT = 0` - Debug output flag

### Architecture

**5-Phase Pipeline:**

1. **Phase 1:** Retrieve source code from `Atom.Content`
2. **Phase 2:** Generate AST structural vector using `dbo.clr_GenerateCodeAstVector` (512-dimensional)
3. **Phase 3:** Project 512D vector to 3D GEOMETRY using `dbo.clr_ProjectToPoint` (landmark projection)
4. **Phase 4:** Store in `CodeAtom` table (UPDATE if exists, INSERT if new)
5. **Phase 5:** Update parent `Atom.SpatialKey`

**Key Innovation:** AST-as-GEOMETRY enables spatial queries for similar code structures.

### Key Operations

**AST Vector Generation:**
```sql
SET @AstVectorJson = dbo.clr_GenerateCodeAstVector(@SourceCode);
-- Returns JSON with 512-dimensional vector representing AST structure
```

**3D Projection:**
```sql
SET @ProjectedPoint = dbo.clr_ProjectToPoint(@AstVectorJson);
-- Projects high-dimensional AST to 3D for GEOMETRY storage
DECLARE @X FLOAT = CAST(JSON_VALUE(@ProjectedPoint, '$.X') AS FLOAT);
DECLARE @Y FLOAT = CAST(JSON_VALUE(@ProjectedPoint, '$.Y') AS FLOAT);
DECLARE @Z FLOAT = CAST(JSON_VALUE(@ProjectedPoint, '$.Z') AS FLOAT);
```

**Storage:**
```sql
SET @EmbeddingGeometry = geometry::STPointFromText(
    'POINT(' + CAST(@X AS NVARCHAR(50)) + ' ' + 
            CAST(@Y AS NVARCHAR(50)) + ' ' + 
            CAST(@Z AS NVARCHAR(50)) + ')', 4326
);
```

### Dependencies
- Tables: `Atom`, `CodeAtom`
- CLR Functions: 
  - `dbo.clr_GenerateCodeAstVector(@SourceCode)` - **MISSING** (not found in audit)
  - `dbo.clr_ProjectToPoint(@AstVectorJson)` - **MISSING** (not found in audit)
- Indexes: Spatial index on `CodeAtom.Embedding`

### Quality Assessment

**Strengths:**
- ✅ **Innovative approach** - AST-as-GEOMETRY enables spatial code similarity
- ✅ **Multi-phase pipeline** - Clear separation of concerns
- ✅ **Error handling** - JSON error checking, TRY/CATCH
- ✅ **Multi-tenancy** - TenantId parameter (though not used in queries)
- ✅ **Idempotent** - UPDATE existing or INSERT new CodeAtom

**Weaknesses:**
- 🔴 **Missing CLR functions** - Both `clr_GenerateCodeAstVector` and `clr_ProjectToPoint` not implemented
- ⚠️ **No TenantId filtering** - Atom retrieval doesn't check TenantId
- ⚠️ **Duplicate CreatedAt columns** - INSERT has `CreatedAt` twice (typo)
- ⚠️ **Hardcoded SRID** - 4326 is WGS84 Earth coordinates (odd choice for AST space)
- ⚠️ **Language parameter unused** - Only 'csharp' supported, no validation
- ⚠️ **No AST metadata** - Should store node counts, depth, complexity metrics
- ⚠️ **No parent Atom validation** - Assumes ContentType is code, no check

**Performance:**
- Dependent on CLR function performance (Roslyn parsing is CPU-intensive)
- Single-atom processing (no batching)
- GEOMETRY indexing for spatial queries

**Security:**
- ⚠️ **Missing TenantId check** - `SELECT FROM Atom WHERE AtomId = @AtomId` should include `AND TenantId = @TenantId`
- ⚠️ **No code validation** - Could parse malicious code (Roslyn should be sandboxed)

### Improvement Recommendations
1. **Priority 1:** Implement missing CLR functions (`clr_GenerateCodeAstVector`, `clr_ProjectToPoint`)
2. **Priority 2:** Add TenantId filtering to Atom retrieval
3. **Priority 3:** Fix duplicate `CreatedAt` column in INSERT
4. **Priority 4:** Store AST metadata (node count, depth, cyclomatic complexity)
5. **Priority 5:** Add language validation and multi-language support
6. **Priority 6:** Consider different SRID for code space (not Earth coordinates)
7. **Priority 7:** Add ContentType validation (ensure it's code before parsing)

---

## 3. sp_AtomizeModel_Governed

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_AtomizeModel_Governed.sql`  
**Type:** Stored Procedure  
**Lines:** ~200  
**Quality Score:** 88/100 ⭐

### Purpose
**GOLD STANDARD** governed, chunked model weight atomization. Implements T-SQL Governor state machine for resumable, quota-enforced ingestion with deduplication.

### Parameters
- `@IngestionJobId BIGINT` - Job tracking ID
- `@ModelData VARBINARY(MAX)` - Serialized model weights
- `@ModelFormat VARCHAR(50)` - Model format (ONNX, PyTorch, TensorFlow, etc.)

### Architecture

**State Machine Loop:**
1. Load job state from `IngestionJobs` (status, chunk size, offset, quota)
2. Stream ONE chunk from `clr_StreamAtomicWeights_Chunked` CLR function
3. Extract unique weight values from chunk
4. Calculate reference counts per unique weight
5. MERGE unique weights into `Atom` table (Modality='model', Subtype='float32-weight')
6. Update reference counts atomically
7. Insert reconstruction data into `TensorAtomCoefficient` (LayerIdx, PositionX/Y/Z)
8. Update job progress (offset, total atoms processed)
9. Repeat until streaming complete (@@ROWCOUNT = 0)

**Governance Features:**
- Atom quota enforcement
- Resumable processing with offset tracking
- Configurable chunk size
- Transaction-per-chunk (prevents large transactions)
- Error tracking with rollback

### Key Operations

**CLR Streaming:**
```sql
INSERT INTO #ChunkWeights ([LayerIdx], [PositionX], [PositionY], [PositionZ], [Value])
SELECT [LayerIdx], [PositionX], [PositionY], [PositionZ], [Value]
FROM [dbo].[clr_StreamAtomicWeights_Chunked](@ModelData, @ModelFormat, @CurrentAtomOffset, @AtomChunkSize);
```

**Deduplication MERGE:**
```sql
MERGE [dbo].[Atom] AS T
USING #UniqueWeights AS S
ON T.[ContentHash] = S.[ContentHash]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Modality], [Subtype], [ContentHash], [AtomicValue], [ReferenceCount], [TenantId])
    VALUES ('model', 'float32-weight', S.[ContentHash], S.[AtomicValue], 0, @TenantId);
```

**Atomic Reference Count Update:**
```sql
UPDATE a
SET a.[ReferenceCount] = a.[ReferenceCount] + cc.[Count]
FROM [dbo].[Atom] a
JOIN #UniqueWeights uw ON a.[ContentHash] = uw.[ContentHash]
JOIN #ChunkCounts cc ON uw.[Value] = cc.[Value]
WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';
```

**Reconstruction Storage:**
```sql
INSERT INTO [dbo].[TensorAtomCoefficient] (
    [TensorAtomId], [ModelId], [LayerIdx], [PositionX], [PositionY], [PositionZ]
)
SELECT wta.[AtomId], @ModelId, s.[LayerIdx], s.[PositionX], s.[PositionY], s.[PositionZ]
FROM #ChunkWeights s
JOIN #WeightToAtomId wta ON s.[Value] = wta.[Value];
```

### Dependencies
- Tables: `IngestionJobs`, `Atom`, `TensorAtomCoefficient`, `Model`
- CLR Functions: `dbo.clr_StreamAtomicWeights_Chunked` (analyzed in Part 10)
- Indexes: `IX_Atom_ContentHash`, `IX_TensorAtomCoefficient_ModelId_LayerId`

### Quality Assessment

**Strengths:**
- ✅ **Excellent governance** - Full state machine with all safety features
- ✅ **Proper deduplication** - ContentHash-based MERGE with atomic reference counting
- ✅ **Multi-tenancy** - V3 upgrade complete
- ✅ **Transaction safety** - Small transactions per chunk, TRY/CATCH with rollback
- ✅ **Resumable processing** - Offset tracking enables restart after failure
- ✅ **CLR streaming** - Efficient one-chunk-at-a-time processing
- ✅ **Cleanup on error** - Temp tables dropped in CATCH block
- ✅ **Status tracking** - Job status updated throughout lifecycle

**Weaknesses:**
- ⚠️ **Missing CoefficientIndex** - `TensorAtomCoefficient` INSERT doesn't include `CoefficientIndex` (may be nullable)
- ⚠️ **No model validation** - Assumes `@ModelId` is valid, no existence check
- ⚠️ **No layer metadata** - Could store layer names, types from model metadata
- ⚠️ **Float32 only** - Comment says "float32-weight" but no handling for other dtypes (float16, bfloat16, int8)

**Performance:**
- O(1) deduplication via ContentHash
- Small transactions prevent lock escalation
- Batch inserts for reconstruction data
- Streaming prevents memory issues with large models

**Security:**
- ✅ Multi-tenant isolation via TenantId
- ✅ Quota enforcement prevents resource exhaustion
- ⚠️ No validation of ModelData (malformed binary could crash CLR)

### Why This Is a GOLD STANDARD

1. **Complete governance implementation** - All safety features present
2. **Efficient streaming architecture** - CLR + T-SQL hybrid
3. **Proper deduplication** - Atomic reference counting
4. **Production-ready error handling** - Full rollback, cleanup, status tracking
5. **Scalable design** - Handles models of any size via chunking

This procedure demonstrates best practices for governed data ingestion.

### Improvement Recommendations
1. **Priority 1:** Add model validation (check ModelId exists)
2. **Priority 2:** Handle multiple data types (float16, bfloat16, int8 quantization)
3. **Priority 3:** Store layer metadata (names, types, shapes)
4. **Priority 4:** Add ModelData validation (magic bytes, size limits)
5. **Priority 5:** Include CoefficientIndex in INSERT (verify schema requirements)

---

## 4. sp_CalculateBill

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_CalculateBill.sql`  
**Type:** Stored Procedure  
**Lines:** ~80  
**Quality Score:** 74/100

### Purpose
Calculate billing totals for a tenant's usage within a billing period. Supports invoice generation.

### Parameters
- `@TenantId INT` - Tenant to bill
- `@BillingPeriodStart DATETIME2 = NULL` - Period start (defaults to current month start)
- `@BillingPeriodEnd DATETIME2 = NULL` - Period end (defaults to next month start)
- `@GenerateInvoice BIT = 0` - Whether to create invoice record

### Architecture

**Calculation Flow:**
1. Default billing period to current month if not specified
2. Aggregate usage from `BillingUsageLedger` by UsageType
3. Calculate subtotal
4. Apply tiered discounts (5%, 10%, 15% based on volume)
5. Calculate 8% tax on discounted amount
6. Optionally generate invoice record
7. Return summary with usage breakdown JSON

**Discount Tiers:**
- \$1,000 - \$5,000: 5%
- \$5,000 - \$10,000: 10%
- \$10,000+: 15%

### Key Operations

**Usage Aggregation:**
```sql
INSERT INTO @UsageSummary
SELECT UsageType, SUM(Quantity) AS TotalQuantity, SUM(TotalCost) AS TotalCost
FROM dbo.BillingUsageLedger
WHERE TenantId = @TenantId
  AND RecordedUtc >= @BillingPeriodStart
  AND RecordedUtc < @BillingPeriodEnd
GROUP BY UsageType;
```

**Invoice Generation:**
```sql
INSERT INTO dbo.BillingInvoice (TenantId, InvoiceNumber, BillingPeriodStart, BillingPeriodEnd, 
                                 Subtotal, Discount, Tax, Total, Status, GeneratedUtc)
VALUES (@TenantId, 'INV-' + FORMAT(@TenantId, '00000') + '-' + FORMAT(SYSUTCDATETIME(), 'yyyyMMdd'),
        @BillingPeriodStart, @BillingPeriodEnd, @Subtotal, @Discount, @Tax, @Total, 'Pending', SYSUTCDATETIME());
```

### Dependencies
- Tables: `BillingUsageLedger`, `BillingInvoice`
- Functions: None
- Indexes: `IX_BillingUsageLedger_TenantId_RecordedUtc`

### Quality Assessment

**Strengths:**
- ✅ **Clear calculation logic** - Easy to understand discount/tax flow
- ✅ **Flexible period** - Defaults to current month, accepts custom ranges
- ✅ **Optional invoice generation** - Separation of calculation and invoice creation
- ✅ **JSON output** - Usage breakdown in structured format
- ✅ **Error handling** - TRY/CATCH with error return

**Weaknesses:**
- ⚠️ **Hardcoded discount tiers** - Should be in `BillingDiscountTier` table
- ⚠️ **Hardcoded tax rate** - 8% tax should be configurable (varies by jurisdiction)
- ⚠️ **Simple invoice numbering** - Format could collide (no sequence, just date)
- ⚠️ **No rate plan support** - Doesn't check `BillingRatePlan` table (exists in schema)
- ⚠️ **No quota validation** - Doesn't check `BillingTenantQuota` (exists in schema)
- ⚠️ **No multiplier support** - Doesn't apply `BillingMultiplier` (exists in schema)
- ⚠️ **Missing currency** - No currency field (assumes USD?)
- ⚠️ **No rounding** - Decimal precision could cause issues

**Performance:**
- Aggregation query should use columnstore index if large
- Table variable for usage summary (good for small result sets)
- No transactions (could cause partial invoice on error)

**Security:**
- ✅ TenantId filtering (multi-tenant safe)
- ⚠️ No authorization check (any caller can bill any tenant)

### Improvement Recommendations
1. **Priority 1:** Load discount tiers from `BillingDiscountTier` table
2. **Priority 2:** Load tax rate from configuration (per tenant or jurisdiction)
3. **Priority 3:** Use `BillingRatePlan` for per-tenant pricing
4. **Priority 4:** Apply `BillingMultiplier` for promotional pricing
5. **Priority 5:** Add authorization check (ensure caller can access tenant)
6. **Priority 6:** Use SEQUENCE for invoice numbering (prevent collisions)
7. **Priority 7:** Add currency support (multi-currency billing)
8. **Priority 8:** Wrap invoice generation in transaction
9. **Priority 9:** Check `BillingTenantQuota` and flag violations

---

## 5. sp_GenerateText

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_GenerateText.sql`  
**Type:** Stored Procedure  
**Lines:** ~60  
**Quality Score:** 68/100

### Purpose
T-SQL wrapper for CLR text generation function. Provides text generation using multi-modal generation with attention.

### Parameters
- `@prompt NVARCHAR(MAX)` - Input prompt
- `@max_tokens INT = 100` - Maximum output tokens
- `@temperature FLOAT = 0.7` - Sampling temperature (0.0 = deterministic, 1.0 = creative)
- `@model_id INT = NULL` - Model to use (defaults to most recently used text model)
- `@tenant_id INT = 0` - Tenant isolation
- `@GeneratedText NVARCHAR(MAX) OUTPUT` - Generated text (OUTPUT parameter)

### Architecture

**Generation Flow:**
1. Select default model if not specified (most recently used text model)
2. Validate model exists and is active
3. Build context JSON from prompt
4. Call `dbo.fn_GenerateText` CLR function
5. Return generation stream ID (placeholder - actual text retrieval TODO)

### Key Operations

**Model Selection:**
```sql
SELECT TOP 1 @model_id = ModelId 
FROM dbo.Model 
WHERE IsActive = 1 AND ModelType = 'text'
ORDER BY LastUsed DESC;
```

**CLR Generation:**
```sql
SET @generationStreamId = dbo.fn_GenerateText(
    @model_id, @inputAtomIds, @contextJson, @max_tokens, 
    @temperature, 50, 0.9, @tenant_id
);
```

**Placeholder Output:**
```sql
SET @GeneratedText = N'[Generation stream ID: ' + CAST(@generationStreamId AS NVARCHAR(20)) + ']';
```

### Dependencies
- Tables: `Model`, `GenerationStream` (provenance), `AtomProvenance` (provenance)
- CLR Functions: `dbo.fn_GenerateText` - **MISSING** (not found in audit)
- Procedures: Called by `sp_ChainOfThoughtReasoning`, `sp_Converse`, `sp_MultiPathReasoning`, `sp_SelfConsistencyReasoning`, `sp_TransformerStyleInference`

### Quality Assessment

**Strengths:**
- ✅ **Sensible defaults** - Selects active text model automatically
- ✅ **Error handling** - TRY/CATCH with error codes
- ✅ **OUTPUT parameter** - Allows direct result retrieval
- ✅ **Multi-tenancy** - TenantId parameter

**Weaknesses:**
- 🔴 **Incomplete implementation** - Returns stream ID placeholder, not actual generated text
- 🔴 **Missing CLR function** - `dbo.fn_GenerateText` not implemented
- ⚠️ **Empty inputAtomIds** - Hardcoded empty string (should retrieve context atoms)
- ⚠️ **No prompt validation** - Could pass malformed JSON (REPLACE for quotes is naive)
- ⚠️ **No temperature validation** - Negative or >1.0 values not checked
- ⚠️ **No max_tokens validation** - Could request excessive tokens
- ⚠️ **Hardcoded topK/topP** - No parameters exposed for sampling control
- ⚠️ **No model update** - Doesn't update Model.LastUsed timestamp
- ⚠️ **Naive JSON escaping** - `REPLACE(@prompt, '"', '\"')` insufficient for full JSON escaping

**Performance:**
- Model selection query is fast (index on IsActive, ModelType, LastUsed)
- CLR function performance unknown (not implemented)
- No caching (could check `InferenceCache` before generation)

**Security:**
- ⚠️ No TenantId check on model access (could use other tenant's model)
- ⚠️ No prompt sanitization (potential injection if CLR has vulnerabilities)
- ⚠️ No rate limiting

### Improvement Recommendations
1. **Priority 1:** Implement missing `dbo.fn_GenerateText` CLR function
2. **Priority 2:** Retrieve actual generated text from provenance stream
3. **Priority 3:** Add TenantId check for model access authorization
4. **Priority 4:** Implement proper JSON escaping (use JSON_MODIFY or dedicated function)
5. **Priority 5:** Add parameter validation (temperature 0-1, max_tokens > 0, etc.)
6. **Priority 6:** Populate inputAtomIds from conversation context
7. **Priority 7:** Expose topK/topP as parameters
8. **Priority 8:** Update Model.LastUsed after generation
9. **Priority 9:** Check InferenceCache before calling CLR (cache hits)
10. **Priority 10:** Add rate limiting or quota checks

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~680  
**Average Quality:** 77.4/100

**Quality Distribution:**
- Excellent (85-100): 2 files (sp_AtomizeImage_Governed 85, sp_AtomizeModel_Governed 88⭐)
- Good (70-84): 2 files (sp_CalculateBill 74, sp_AtomizeCode 72)
- Fair (65-69): 1 file (sp_GenerateText 68)

**Key Patterns:**
- **Governed atomization** - sp_AtomizeImage_Governed and sp_AtomizeModel_Governed demonstrate production-ready governance
- **Missing CLR functions** - 3 CLR functions not yet implemented (clr_GenerateCodeAstVector, clr_ProjectToPoint, fn_GenerateText)
- **Incomplete implementations** - sp_GenerateText and sp_AtomizeCode have TODO/placeholder sections
- **Billing simplification** - sp_CalculateBill uses hardcoded logic instead of schema tables

**Missing Objects Identified:**
- CLR Functions (3):
  - `dbo.clr_GenerateCodeAstVector(@SourceCode)` - AST vector generation
  - `dbo.clr_ProjectToPoint(@AstVectorJson)` - 512D to 3D projection
  - `dbo.fn_GenerateText(...)` - Text generation with attention
- Tables: All referenced tables exist

**Cross-References:**
- `sp_GenerateText` called by: sp_ChainOfThoughtReasoning, sp_Converse, sp_MultiPathReasoning, sp_SelfConsistencyReasoning, sp_TransformerStyleInference
- `clr_StreamAtomicWeights_Chunked` used by: sp_AtomizeModel_Governed (analyzed in Part 10)

**Critical Issues:**
1. 3 missing CLR functions block functionality
2. sp_GenerateText returns placeholder (not actual text)
3. sp_AtomizeCode requires Roslyn CLR implementation
4. sp_CalculateBill ignores billing schema tables (RatePlan, Multiplier, etc.)

**Recommendations:**
1. Implement missing CLR functions (Priority 1)
2. Complete sp_GenerateText text retrieval from provenance
3. Replace hardcoded billing logic with schema-driven approach
4. Add TenantId authorization checks to sp_AtomizeCode and sp_GenerateText
