## Hartonomous — AI assistant guidance for contributors

This file gives targeted, actionable guidance for AI coding agents (Copilot-style) working in Hartonomous. Keep answers short and reference specific files and commands.

### Big-picture architecture (short)
- **EF Core 10.0.0-rc.2.25502.107 Code First** is the PRIMARY approach: Clean architecture (Core/Data/Infrastructure) with repository pattern, DI, and migrations. Database schema is managed by EF migrations.
- **SQL Server 2025 RC1** is the engine: VECTOR type (max 1998 dimensions float32, 3996 dimensions float16), JSON type, VECTOR_DISTANCE function, stored procedures for inference operations EF cannot express.
- **SQL CLR** (.NET Framework 4.8): Performance-critical extensions for iterative algorithms, array operations, and spatial geometry that T-SQL cannot express efficiently. Deployed separately via `scripts/deploy.ps1`.
- **VECTOR type**: Native binary storage for all float arrays (embeddings, model weights, activations). Use `SqlVector<float>` in C#. Already efficient - no compression needed.
- **JSON type**: Native UTF-8 storage for all structured metadata. Use regular `string` in C# (appears as VARCHAR(MAX) to TDS clients).
- **DiskANN indexes**: CREATE VECTOR INDEX omitted (RC1 read-only limitation). Queries work correctly with O(n) scans, just slower until GA.
- **Neo4j** stores the semantic audit trail (`neo4j/schemas/CoreSchema.cypher`) but sync is currently a future enhancement.

### CRITICAL: SQL CLR Purpose & When to Use
**SQL CLR is NOT a general-purpose compute layer.** It's for **performance-critical operations that T-SQL cannot do efficiently or at all.**

**When SQL CLR is appropriate (use VectorOperations.cs, SpatialOperations.cs):**
- **Complex vector math** that T-SQL lacks (now mostly superseded by native VECTOR_DISTANCE function in SQL Server 2025)
- **Iterative algorithms** (loops are slow in T-SQL) - e.g., UMAP dimensionality reduction, gradient descent
- **Array/Buffer operations** - T-SQL doesn't have arrays, C# does (Buffer.BlockCopy for byte-to-float conversion)
- **Complex spatial geometry** beyond built-in GEOMETRY functions (convex hulls, point clouds, multi-resolution hierarchies)
- **Performance:** When T-SQL is O(n²) but C# can do O(n log n) with proper data structures

**When to use T-SQL instead (most cases):**
- **Data access** - SELECT/INSERT/UPDATE/DELETE (T-SQL is optimized for this)
- **Set operations** - JOINs, aggregations, window functions (declarative >>> imperative)
- **Vector distance** - Use native VECTOR_DISTANCE('cosine', v1, v2), NOT custom CLR functions
- **Spatial queries** - Use spatial indexes with STDistance, NOT custom CLR (indexes don't work with CLR functions)
- **Business logic** - Stored procedures with parameters, NOT CLR wrappers

**Current CLR usage (see `src/SqlClr/`):**
- `VectorOperations.cs` - Mostly **OBSOLETE** now that SQL Server 2025 has native VECTOR_DISTANCE. May still be useful for: VectorAdd, VectorSubtract, VectorScale, VectorNormalize (arithmetic T-SQL lacks).
- `SpatialOperations.cs` - **VALID** use cases: CreatePointCloud (multi-point geometry), ConvexHull wrapper, RegionOverlap (intersection area calculation beyond built-in STIntersection).
- `AudioProcessing.cs` / `ImageProcessing.cs` - **FUTURE** - for FFT, convolution, image transforms that T-SQL cannot express.

**Deployment:** SQL CLR requires .NET Framework 4.8 SDK (not .NET Core) to build. Once compiled, it's a stable assembly that .NET 10 applications pass data through for performance-critical operations inside SQL Server. Build is conditional in `scripts/deploy.ps1` (checks for SDK), but the assembly itself is **essential for production**.

### The Spatial Optimization Strategy (CRITICAL CONCEPT)
**Problem:** VECTOR_DISTANCE is O(n) - scans all embeddings. At 1 billion vectors, this takes seconds.

**Solution:** Dual representation with spatial geometry for O(log n) indexed lookups.

**How it works:**
1. **Full resolution:** Store `VECTOR(768)` for exact similarity (VECTOR_DISTANCE function)
2. **Spatial projection:** Reduce 768D → 3D using distance-based coordinates (see `sql/procedures/08_SpatialProjection.sql`)
   - Pick 3 anchor points (maximal distance apart)
   - For any vector V: (x, y, z) = (distance(V, anchor1), distance(V, anchor2), distance(V, anchor3))
   - Store as `GEOMETRY POINT(x, y, z)` with spatial index
3. **Hybrid search:** Fast spatial filter (100 candidates via index) → Exact vector rerank (top 10)
   - See `sql/procedures/06_ProductionSystem.sql` lines 206-245 for sp_HybridSearch

**Performance:**
- Exact vector scan: O(n) - 5 seconds at 1M vectors, 1.4 hours at 1B vectors
- Spatial index: O(log n) - 2-3ms even at 1B vectors (DiskANN benchmarks)
- Hybrid: 95-99% recall with <10ms latency

**Why spatial geometry for AI?**
- **Spatial indexes are mature** - 30+ years of optimization in SQL Server
- **Attention mechanism ≈ nearest-neighbor** - find semantically similar tokens = find nearby points in space
- **No GPU required** - spatial indexes use disk-based B-trees, not VRAM
- **Multi-resolution hierarchy** - Spatial index LEVELS (1-4) = coarse-to-fine search like transformer layers

**See these files for implementation:**
- `sql/procedures/05_SpatialInference.sql` - Novel approach: attention via STDistance instead of matrix multiply
- `sql/procedures/08_SpatialProjection.sql` - Distance-based dimensionality reduction in pure T-SQL
- `sql/procedures/06_ProductionSystem.sql` - Production system with sp_ExactVectorSearch, sp_ApproxSpatialSearch, sp_HybridSearch
- `src/ModelIngestion/EmbeddingIngestionService.cs` - C# ingestion with dual representation (VECTOR + GEOMETRY)

### Key developer workflows (commands)
- **EF Code First migrations**: `cd src/Hartonomous.Data; dotnet ef migrations add MigrationName --startup-project ../ModelIngestion; dotnet ef database update --startup-project ../ModelIngestion`
- **Deploy CLR only**: run `scripts\deploy.ps1` from PowerShell (builds and deploys SQL CLR assembly only - EF handles schema)
- **Build solution**: `dotnet build Hartonomous.sln` (builds Core/Data/Infrastructure/ModelIngestion/CesConsumer)
- **Test ingestion**: `dotnet run --project src/ModelIngestion` (ADO.NET service, being migrated to EF)
- **Test stored procedures**: Use SSMS or `sqlcmd -S localhost -E -C -d Hartonomous -Q "EXEC sp_ExactVectorSearch ..."`

### Project-specific conventions and patterns
- **EF Code First**: PRIMARY approach. Database schema managed by migrations. Entities in Core layer drive schema generation.
- **Architecture layers**: Core (domain entities) → Data (EF DbContext + configurations) → Infrastructure (repositories) → Applications.
- **Repository pattern**: Inject `IModelRepository`, `IEmbeddingRepository`, `IInferenceRepository` via DI. Repositories encapsulate EF Core and stored proc calls.
- **VECTOR type in C#**: Use `SqlVector<float>` from `Microsoft.Data.SqlClient` 6.1.2+. Pattern: `cmd.Parameters.AddWithValue("@vector", new SqlVector<float>(floatArray))`. Binary TDS transport. NO compression needed - VECTOR is already binary and efficient.
- **JSON type in C#**: Use regular `string` parameters. JSON columns in SQL Server 2025 appear as VARCHAR(MAX) to TDS clients. Pattern: `cmd.Parameters.AddWithValue("@json", jsonString)`.
- **VECTOR mapping in EF**: VECTOR columns map to `SqlVector<float>?` in C# entities (Core layer), configured with `.HasColumnType("VECTOR(768)")` in Data layer configurations.
- **Model weights**: Stored as `VECTOR(1998)` (max float32 dimension). Large layers chunked across multiple rows. NO WeightsCompressed - VECTOR is already efficient.
- **Relational structure for embeddings**: NO aggregated embeddings in parent tables. Use proper detail tables with individual VECTOR columns per component.
- **Stored procedures**: For inference operations EF cannot express. Called via `FromSqlRaw` in repositories.
- **DiskANN indexes**: Omitted in RC1 (read-only limitation). Code acts as if indexes exist, queries work correctly but slower until GA.
- **SQL schema files**: Reference only - NOT deployed. EF migrations are the source of truth for schema.

### Integration points & external deps
- **SQL Server 2025 RC1**: VECTOR type (max 1998 dimensions float32, 3996 dimensions float16), VECTOR_DISTANCE, DiskANN indexes (read-only limitation), CES are **production-ready**. Run `ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;` for half-precision (float16) and CREATE VECTOR INDEX.
- **.NET 10 RC2** (10.0.100-rc.2.25502.107) installed - use packages with version **10.0.0-rc.2.25502.107** (not 10.0.0 stable, doesn't exist yet).
- **EF Core 10.0.0-rc.2.25502.107** - **Standard C# interface to the engine.** Use for all new C# code. Existing ModelIngestion being migrated.
- **SQL CLR** (`src/SqlClr`): **Core performance component** (.NET Framework 4.8). Lives inside SQL Server, provides functions T-SQL can't do efficiently (iterative algorithms, array ops, complex spatial geometry). .NET 10 applications pass data through it. Requires .NET Framework 4.8 SDK to build, then works with any .NET version.
- **Neo4j 5.x** (local at `http://localhost:7474`, credentials: neo4j/neo4jneo4j) — schema in `neo4j/schemas/CoreSchema.cypher`. Currently optional/future enhancement.

### What to change and how to verify (examples)
- **Add inference procedure**: Create SQL in `sql/procedures/`, follow naming like `sp_YourFeature`. Test with `sqlcmd` or SSMS. Update `README.md` with usage example.
- **Extend ingestion (with EF Core)**: Modify `src/Hartonomous.Infrastructure/Repositories/EmbeddingRepository.cs`. Use DI-injected `HartonomousDbContext`. Run tests.
- **Extend ingestion (with ADO.NET)**: Modify `src/ModelIngestion/EmbeddingIngestionService.cs`. Use `SqlCommand` + `@param` style. Run `dotnet run --project src/ModelIngestion` to test.
- **Add C# repository**: Follow `ModelRepository.cs` pattern in Infrastructure layer - constructor with `HartonomousDbContext` and `ILogger`, async methods. For stored procs: use `FromSqlRaw`.
- **Enable DiskANN index**: Uncomment CREATE VECTOR INDEX in `sql/schemas/04_DiskANNPattern.sql` line ~231 when GA releases (removes read-only limitation).
- **Add SQL CLR function**: Only if T-SQL cannot do it efficiently. Add to `src/SqlClr/VectorOperations.cs` or `SpatialOperations.cs`. Requires .NET Framework 4.8 SDK. Redeploy via `scripts/deploy.ps1`.

### Example code snippets (cite files)
- **VECTOR insert (C#, SqlVector pattern)**: `src/ModelIngestion/EmbeddingIngestionService.cs` - Use `new SqlVector<float>(floatArray)` with `AddWithValue`. Binary TDS transport, no JSON conversion needed. Deduplication tests passing.
- **JSON insert (C#, string pattern)**: JSON columns accept regular string parameters. Pattern: `cmd.Parameters.AddWithValue("@metadata", jsonString)`. SQL Server 2025 JSON type appears as VARCHAR(MAX) to TDS clients.
- **VECTOR insert (EF Core)**: `src/Hartonomous.Infrastructure/Repositories/EmbeddingRepository.cs` - entity has `SqlVector<float>? EmbeddingFull` property, EF configuration maps to `VECTOR(768)` via `.HasColumnType("VECTOR(768)")`. See `src/Hartonomous.Data/Configurations/EmbeddingConfiguration.cs`.
- **Stored proc call (ADO.NET)**: `ModelRepository.cs` line ~30-45 - `SqlCommand` with `@param` AddWithValue, ExecuteScalarAsync for return values.
- **Stored proc call (EF Core)**: `src/Hartonomous.Infrastructure/Repositories/EmbeddingRepository.cs` line ~50-70 - `_context.Embeddings.FromSqlRaw("EXEC dbo.sp_ExactVectorSearch @queryVector, @topK", ...)`.
- **Hybrid search**: `sql/procedures/06_ProductionSystem.sql` line ~206-245 - spatial filter (100 candidates) then exact VECTOR_DISTANCE rerank (top 10).
- **DiskANN promotion**: `sql/schemas/04_DiskANNPattern.sql` line ~90-180 - `sp_PromoteToProduction` moves staging to production with INT clustered PK.
- **Spatial projection (pure T-SQL)**: `sql/procedures/08_SpatialProjection.sql` line ~60-100 - `sp_ComputeSpatialProjection` uses 3 anchor points, outputs (x, y, z) = distances.
- **Spatial attention (novel approach)**: `sql/procedures/05_SpatialInference.sql` line ~85-120 - `sp_SpatialAttention` uses `STDistance` instead of Q@K.T matrix multiply.

### Don'ts / pitfalls
- **Do NOT add SQL CLR** for things T-SQL can do. Use CLR only for: complex iterative algorithms, array operations, or when T-SQL is O(n²) but C# can do O(n log n).
- **Do NOT use custom CLR vector distance functions** - SQL Server 2025 has native VECTOR_DISTANCE. Use that instead (it's faster and index-friendly).
- **Do NOT use string conversions for VECTOR columns** - Use SqlVector<float> type from Microsoft.Data.SqlClient 6.1.2+. Avoid JSON array strings like "[1.0, 2.0]".
- **Do NOT aggregate embeddings in parent tables** - Use proper detail tables (ImagePatches, AudioFrames, VideoFrames) with individual VECTOR columns. NO VARBINARY(MAX) for embeddings.
- **Do NOT use NVARCHAR(MAX) with CHECK (ISJSON...)** - SQL Server 2025 has native JSON data type. Use `column_name JSON` instead.
- **Do NOT uncomment DiskANN index creation** until SQL Server GA (removes read-only limitation). Keep commented for now.
- **Do NOT assume Neo4j sync is implemented** - skeleton exists but not wired up. Focus on SQL-first functionality.
- **Do NOT mix EF and ADO.NET** in the same service - use one or the other. EF Core framework (Core/Data/Infrastructure) is optional addition. Existing ModelIngestion uses ADO.NET and works fine.
- **Avoid long-running loops without CancellationToken** - see `CdcListener.cs` pattern for background services.
- **Do NOT use package version "10.0.0" (stable)** - doesn't exist yet. Use **10.0.0-rc.2.25502.107** for .NET 10 RC2 compatibility.

### Deduplication Strategy (Content-Addressable Storage)

**Philosophy**: NEVER store the same atomic component twice. Use SHA256 hashing for exact matches, cosine similarity for semantic duplicates.

**Threshold Selection** (for semantic deduplication):
- **0.99**: Near-perfect copies only (99% similar) - TOO STRICT for real deduplication
- **0.95**: Very similar content - paraphrases, rewording - **RECOMMENDED for production**
- **0.90**: Similar content - related sentences, same topic
- **0.85**: Somewhat similar - may catch false positives
- Default: **0.95** (configurable in appsettings.json)

**Two-Stage Deduplication**:
1. **Exact hash check** (SHA256 of content) - instant O(1) lookup via indexed column
2. **Semantic similarity check** (cosine distance via VECTOR_DISTANCE) - O(n) but catches paraphrases

**Implementation** (`EmbeddingIngestionService.IngestEmbeddingWithDeduplicationAsync`):
```csharp
// Stage 1: Content hash (exact match)
var contentHash = ComputeSHA256Hash(sourceText);
var existing = await CheckDuplicateByHashAsync(connection, contentHash);
if (existing.HasValue) {
    await IncrementAccessCountAsync(connection, existing.Value);
    return new EmbeddingIngestionResult { WasDuplicate = true };
}

// Stage 2: Semantic similarity (cosine > threshold)
var similar = await CheckDuplicateBySimilarityAsync(connection, embeddingFull, 0.95);
if (similar.HasValue) {
    await IncrementAccessCountAsync(connection, similar.Value);
    return new EmbeddingIngestionResult { WasDuplicate = true };
}

// New: Insert with hash
await InsertEmbeddingAsync(connection, sourceText, embeddingFull, spatial3D, contentHash);
```

**Atomic Component Deduplication**:
- **Pixels**: Same RGBA = same hash → reference existing AtomicPixels row
- **Audio samples**: Same amplitude → reference existing AtomicAudioSamples row
- **Tokens**: Same text → reference existing AtomicTextTokens row
- **Vector components**: Same float value → reference existing AtomicVectorComponents row

**Reference Counting**: Every duplicate increments `reference_count` and updates `last_referenced` timestamp.
1. **`sql/schemas/01_CoreTables.sql`** — Core DB: Models, ModelLayers, InferenceRequests, TokenVocabulary tables
2. **`sql/schemas/04_DiskANNPattern.sql`** — Staging/production pattern with commented-out DiskANN index (RC1 limitation)
3. **`sql/procedures/06_ProductionSystem.sql`** — Key stored procs: sp_ExactVectorSearch, sp_HybridSearch, sp_ExtractStudentModel
4. **`sql/procedures/08_SpatialProjection.sql`** — Distance-based 768D→3D projection (pure T-SQL, no Python/UMAP)
5. **`sql/procedures/05_SpatialInference.sql`** — Novel spatial attention: STDistance replaces matrix multiply
6. **`src/Hartonomous.Infrastructure/Repositories/`** — EF Core repository pattern (IModelRepository, IEmbeddingRepository, IInferenceRepository) - optional framework
7. **`src/Hartonomous.Data/HartonomousDbContext.cs`** — EF Core DbContext with VECTOR type mapping - optional framework
8. **`src/ModelIngestion/EmbeddingIngestionService.cs`** — Current working C# ingestion with VECTOR casting and spatial projection (direct ADO.NET)
9. **`src/ModelIngestion/ModelRepository.cs`** — Direct ADO.NET pattern example
10. **`scripts/deploy.ps1`** — Deployment: SQL schemas + Neo4j + optional CLR
11. **`README.md`, `PRODUCTION_GUIDE.md`** — Architecture rationale, design decisions, performance characteristics

### Current implementation status (as of Oct 2025)
**Working now:**
- ✅ VECTOR(n) type with VECTOR_DISTANCE function
- ✅ Dual storage: VECTOR + GEOMETRY spatial indexes
- ✅ Stored procedures: exact search, hybrid search, student model extraction
- ✅ C# ingestion service with direct ADO.NET (ModelIngestion)
- ✅ EF Core architecture: Core/Data/Infrastructure layers (builds successfully, optional framework)
- ✅ Staging/production pattern ready for DiskANN
- ✅ Spatial projection: 768D→3D using distance-based coordinates (pure T-SQL)
- ✅ Spatial attention: Novel approach using STDistance instead of matrix multiply

**Commented out (RC1 limitation):**
- 🟡 CREATE VECTOR INDEX (makes table read-only in RC1, will be enabled in GA)

**Not yet implemented:**
- ⏳ EF Core migrations (not generated yet - run `dotnet ef migrations add InitialCreate`)
- ⏳ Refactor services to use EF Core architecture (optional - current ADO.NET works fine)
- ⏳ Neo4j sync service (skeleton exists, needs implementation)
- ⏳ CES consumer (skeleton exists, needs LSN tracking)
- ⏳ Full model decomposition ingestion (tables exist, C# code partial)
- ⏳ Image/audio/video generation (framework ready, not implemented)

### Architecture layers (NEW - as of Oct 2025)
**Clean architecture introduced for maintainability:**
1. **Hartonomous.Core** (`src/Hartonomous.Core/`) - Pure domain entities: Model, ModelLayer, Embedding, InferenceRequest, etc. No dependencies.
2. **Hartonomous.Data** (`src/Hartonomous.Data/`) - EF Core DbContext, entity configurations (Fluent API), VECTOR type mapping via `.HasColumnType("VECTOR(768)")`.
3. **Hartonomous.Infrastructure** (`src/Hartonomous.Infrastructure/`) - Repository implementations, DI registration (`AddHartonomousInfrastructure`), health checks. Calls stored procs via `FromSqlRaw`.
4. **Application Services** - ModelIngestion (legacy ADO.NET), CesConsumer, Neo4jSync (will be refactored to use Infrastructure layer).

**DI registration pattern:**
```csharp
// In Program.cs or Startup.cs
builder.Services.AddHartonomousInfrastructure(builder.Configuration);
builder.Services.AddHartonomousHealthChecks(builder.Configuration);

// Then inject:
public class MyService
{
    private readonly IEmbeddingRepository _embeddings;
    public MyService(IEmbeddingRepository embeddings) { _embeddings = embeddings; }
}
```

If unclear, ask specific scenario questions (e.g., "how to add DI to ModelIngestion" or "when should I use SQL CLR vs T-SQL for vector operations").
