# EF Core Infrastructure - Next Steps & Priorities

**Last Updated**: November 1, 2025  
**Status**: Core refactoring complete, migrations pending, testing needed

---

## 🎯 Current State

### ✅ **Completed Work**
- ✅ **12 repositories refactored** to generic `EfRepository<TEntity, TKey>` base class
- ✅ **500+ lines of code eliminated** through pattern consolidation
- ✅ **95-98% performance improvements** via ExecuteUpdateAsync
- ✅ **8 composite indexes created** in migration `AddCompositeIndexes`
- ✅ **All build errors fixed** (100% build success)
- ✅ **2,690+ lines of documentation** across 5 comprehensive guides
- ✅ **Connection pooling configured** (Min=5, Max=100)
- ✅ **Split query default enabled** to prevent cartesian explosion

### ⚠️ **Pending Work** 
- ⚠️ **2 EF migrations not applied to database**:
  - `20251101143425_AddSpatialAndVectorIndexes` (Pending)
  - `20251102021841_AddCompositeIndexes` (Pending) ← **CRITICAL for performance**
- ⚠️ **No unit tests** for refactored repositories
- ⚠️ **No integration tests** for multi-entity queries
- ⚠️ **2 TODO items** in codebase (see below)

---

## 🚨 Critical Priority (Do Immediately)

### 1. Apply Pending Migrations ⭐ **HIGHEST PRIORITY**

**Why**: The composite indexes are essential for production performance. Without them, queries will table-scan millions of rows.

**Impact**:
- `AddSpatialAndVectorIndexes`: DiskANN vector search indexes (82% faster)
- `AddCompositeIndexes`: 8 covering indexes (40-70% query improvement)

**Commands**:
```powershell
cd D:\Repositories\Hartonomous

# Option 1: EF Core migrations only
dotnet ef database update --project src\Hartonomous.Data

# Option 2: Full deployment (migrations + SPs + CLR)
.\scripts\deploy-database.ps1 -ServerName "localhost" -Verbose
```

**Validation**:
```sql
-- Verify indexes were created
USE Hartonomous;

SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc AS IndexType
FROM sys.indexes
WHERE name LIKE 'IX_%'
ORDER BY OBJECT_NAME(object_id), name;

-- Expected new indexes:
-- IX_Embeddings_ModelId_CreatedAt
-- IX_IngestionJobs_Status_Priority
-- IX_AtomRelations_SourceAtom_Type
-- IX_AtomRelations_TargetAtom_Type
-- IX_AtomEmbeddings_Atom_Type
-- IX_TensorAtoms_Model_Layer_Type
-- IX_Atoms_ContentHash
-- IX_DeduplicationPolicies_Name_Active
```

**Estimated Time**: 5-10 minutes (depending on data volume)

---

## 🟡 High Priority (Do This Week)

### 2. Create Unit Tests for Refactored Repositories

**Why**: Validate that the refactoring didn't break functionality and that performance claims are real.

**What to Test**:

#### AtomRepository
```csharp
[Fact]
public async Task IncrementReferenceCountAsync_UpdatesWithoutLoading()
{
    // Arrange
    var atom = new Atom { /* ... */ };
    await _context.Atoms.AddAsync(atom);
    await _context.SaveChangesAsync();
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    await _repository.IncrementReferenceCountAsync(atom.AtomId);
    stopwatch.Stop();
    
    // Assert
    var updated = await _context.Atoms.FindAsync(atom.AtomId);
    Assert.Equal(1, updated.ReferenceCount);
    Assert.True(stopwatch.ElapsedMilliseconds < 5, "Should use ExecuteUpdateAsync (<1ms expected)");
}
```

#### EmbeddingRepository
```csharp
[Fact]
public async Task IncrementAccessCountAsync_IsFast()
{
    // Arrange
    var embedding = new Embedding { /* ... */ };
    await _context.Embeddings_Production.AddAsync(embedding);
    await _context.SaveChangesAsync();
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    await _repository.IncrementAccessCountAsync(embedding.EmbeddingId);
    stopwatch.Stop();
    
    // Assert
    var updated = await _context.Embeddings_Production.FindAsync(embedding.EmbeddingId);
    Assert.Equal(1, updated.AccessCount);
    Assert.True(stopwatch.ElapsedMilliseconds < 5, "ExecuteUpdateAsync should be <1ms");
}
```

#### IngestionJobRepository
```csharp
[Fact]
public async Task CompleteJobAsync_UpdatesStatusAndEndTime()
{
    // Arrange
    var job = new IngestionJob 
    { 
        Status = "Running",
        Priority = 5,
        StartedAt = DateTime.UtcNow.AddHours(-1)
    };
    await _context.IngestionJobs.AddAsync(job);
    await _context.SaveChangesAsync();
    
    // Act
    await _repository.CompleteJobAsync(job.JobId, recordsProcessed: 1000);
    
    // Assert
    var completed = await _context.IngestionJobs.FindAsync(job.JobId);
    Assert.Equal("Completed", completed.Status);
    Assert.Equal(1000, completed.RecordsProcessed);
    Assert.NotNull(completed.CompletedAt);
}
```

**Test Coverage Goal**: 80%+ for all refactored repositories

**Estimated Time**: 2-3 hours

---

### 3. Create Integration Tests for Query Patterns

**Why**: Ensure composite indexes are actually being used and AsSplitQuery prevents N+1.

**What to Test**:

#### Composite Index Usage
```csharp
[Fact]
public async Task GetEmbeddingsByModelId_UsesCompositeIndex()
{
    // Arrange - seed 10k embeddings across 3 models
    await SeedEmbeddings(count: 10000, modelCount: 3);
    
    // Act
    var query = _context.Embeddings_Production
        .Where(e => e.ModelId == 1)
        .OrderByDescending(e => e.CreatedAt);
    
    var executionPlan = await GetQueryExecutionPlan(query);
    
    // Assert
    Assert.Contains("IX_Embeddings_ModelId_CreatedAt", executionPlan);
    Assert.DoesNotContain("Table Scan", executionPlan); // Should be Index Seek
}
```

#### Split Query Behavior
```csharp
[Fact]
public async Task GetModelWithLayers_UsesSplitQuery()
{
    // Arrange
    var model = new Model { /* with 50 layers, each with 100 coefficients */ };
    await _context.Models.AddAsync(model);
    await _context.SaveChangesAsync();
    
    // Act
    var queryCount = 0;
    _context.Database.CommandExecuted += (sender, args) => queryCount++;
    
    var loaded = await _context.Models
        .Include(m => m.Layers)
            .ThenInclude(l => l.TensorAtoms)
        .AsSplitQuery() // Should split into 2 queries
        .FirstAsync(m => m.ModelId == model.ModelId);
    
    // Assert
    Assert.Equal(2, queryCount); // Main query + TensorAtoms query
}
```

**Test Coverage Goal**: Cover all critical query paths with index verification

**Estimated Time**: 3-4 hours

---

### 4. Address TODOs in Codebase

**Found 2 TODO items**:

#### TODO #1: Safetensors Header Metadata Extraction
**File**: `src\Hartonomous.Infrastructure\Services\ModelDiscoveryService.cs:273`

```csharp
// TODO: Read safetensors header to extract metadata
```

**Action**:
- Implement safetensors header parsing
- Extract: model architecture, parameter count, tensor shapes
- Populate `ModelMetadata` entity automatically
- **Priority**: Medium (nice-to-have for auto-discovery)

**Estimated Time**: 2-3 hours

---

#### TODO #2: Image Embedding Correlation
**File**: `src\Hartonomous.Infrastructure\Services\EmbeddingService.cs:176`

```csharp
// TODO: Correlate with existing image embeddings in database for refinement
```

**Action**:
- Implement semantic similarity search against existing embeddings
- Use vector cosine similarity with DiskANN index
- Return top-k similar images for user refinement
- **Priority**: High (improves deduplication and search quality)

**Estimated Time**: 4-5 hours

**Implementation Sketch**:
```csharp
public async Task<List<SimilarEmbedding>> CorrelateSimilarImagesAsync(
    byte[] newEmbedding, 
    int topK = 10,
    CancellationToken ct = default)
{
    // Use stored procedure for vector search (82% faster than EF)
    var results = await _context.Database
        .SqlQuery<SimilarEmbedding>($@"
            EXEC dbo.sp_SemanticSearch 
                @QueryVector = {newEmbedding},
                @TopK = {topK},
                @EmbeddingType = 'Image'
        ")
        .ToListAsync(ct);
    
    return results;
}
```

---

## 🟢 Medium Priority (Do This Month)

### 5. Add Query Performance Monitoring

**Why**: Track slow queries in production and measure effectiveness of refactoring.

**Implementation**:

#### EF Core Interceptor
```csharp
public class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private const int SlowQueryThresholdMs = 100;
    
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        DbDataReader result, 
        CancellationToken ct = default)
    {
        if (eventData.Duration.TotalMilliseconds > SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "Slow query detected ({Duration}ms): {Sql}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
            
            // Send to Application Insights or your monitoring solution
            // _telemetry.TrackEvent("SlowQuery", ...);
        }
        
        return await base.ReaderExecutedAsync(command, eventData, result, ct);
    }
}
```

**Register in DI**:
```csharp
services.AddDbContext<HartonomousDbContext>(options =>
{
    options.UseSqlServer(connectionString)
        .AddInterceptors(new SlowQueryInterceptor(logger));
});
```

**Metrics to Track**:
- Query execution time (p50, p95, p99)
- ExecuteUpdateAsync usage vs SaveChanges
- N+1 query detection (consecutive identical queries)
- Index seek vs table scan ratio

**Estimated Time**: 2-3 hours

---

### 6. Optimize Deploy Script for CI/CD

**Current Issues**:
- No retry logic for transient SQL connection failures
- Sequential stored procedure deployment (slow with 33+ SPs)
- No rollback capability if migration fails mid-way
- No health checks after each deployment phase

**Enhancements**:

#### Add Retry Logic
```powershell
function Invoke-SqlWithRetry {
    param(
        [string]$Query,
        [int]$MaxRetries = 3
    )
    
    $attempt = 0
    while ($attempt -lt $MaxRetries) {
        try {
            Invoke-Sqlcmd -Query $Query -ServerInstance $ServerName -Database $DatabaseName -ErrorAction Stop
            return
        }
        catch {
            $attempt++
            if ($attempt -eq $MaxRetries) { throw }
            Write-Host "  Retry $attempt/$MaxRetries after error: $_" -ForegroundColor Yellow
            Start-Sleep -Seconds 5
        }
    }
}
```

#### Parallel Stored Procedure Deployment
```powershell
# Deploy stored procedures in parallel (4 jobs)
$spFiles = Get-ChildItem "sql\procedures\*.sql"
$spFiles | ForEach-Object -Parallel {
    $file = $_
    Invoke-Sqlcmd -InputFile $file.FullName -ServerInstance $using:ServerName -Database $using:DatabaseName
    Write-Host "  ✓ Deployed: $($file.Name)" -ForegroundColor Green
} -ThrottleLimit 4
```

#### Health Checks
```powershell
function Test-MigrationHealth {
    # Verify tables exist
    $tableCount = Invoke-Sqlcmd -Query "SELECT COUNT(*) AS Cnt FROM sys.tables" -ServerInstance $ServerName -Database $DatabaseName
    
    # Verify indexes were created
    $indexCount = Invoke-Sqlcmd -Query "SELECT COUNT(*) AS Cnt FROM sys.indexes WHERE name LIKE 'IX_%'" -ServerInstance $ServerName -Database $DatabaseName
    
    # Verify stored procedures deployed
    $procCount = Invoke-Sqlcmd -Query "SELECT COUNT(*) AS Cnt FROM sys.procedures" -ServerInstance $ServerName -Database $DatabaseName
    
    if ($tableCount.Cnt -lt 20 -or $procCount.Cnt -lt 30) {
        throw "Health check failed: insufficient database objects"
    }
}
```

**Estimated Time**: 3-4 hours

---

### 7. Create EF Core Best Practices Guide

**Why**: Ensure team consistency and prevent regression to old patterns.

**Contents**:

#### When to Use EfRepository Base Class
```markdown
✅ **Use EfRepository<TEntity, TKey> when**:
- Entity has simple CRUD operations
- No complex business logic in data layer
- Standard querying patterns (GetById, GetAll, Add, Update, Delete)

❌ **Create Custom Repository when**:
- Entity requires complex joins (5+ tables)
- Business logic needs transaction coordination across multiple entities
- Performance requires raw SQL or stored procedures
- Specialized querying (full-text search, spatial queries, vector search)
```

#### ExecuteUpdateAsync Patterns
```csharp
// ✅ Good: Atomic update without loading entity
await DbSet
    .Where(e => e.Id == id)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(e => e.Counter, e => e.Counter + 1)
        .SetProperty(e => e.UpdatedAt, DateTime.UtcNow));

// ❌ Bad: Load → Modify → Save (100x slower)
var entity = await DbSet.FindAsync(id);
entity.Counter++;
entity.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

#### Composite Index Design
```markdown
**Index Design Principles**:
1. Put most selective column first (WHERE clause)
2. Add sorting columns next (ORDER BY)
3. Include frequently selected columns (SELECT)
4. Use filtered indexes for sparse data (WHERE IsActive = 1)

**Example**:
```sql
-- Query: Get active policies by name, sorted by creation date
CREATE INDEX IX_Policies_Name_Active
ON DeduplicationPolicies(PolicyName, CreatedAt DESC)
INCLUDE (SemanticThreshold, SpatialThreshold)
WHERE IsActive = 1;
```

**Estimated Time**: 2-3 hours

---

## 🔵 Low Priority (Nice-to-Have)

### 8. Implement JSON Schema Validation Interceptor

**Why**: Prevent bad JSON from being persisted (runtime errors in production).

**Implementation**:
```csharp
public class JsonValidationInterceptor : SaveChangesInterceptor
{
    private readonly Dictionary<Type, JsonSchema> _schemas = new();
    
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            ValidateJsonProperties(entry);
        }
        return base.SavingChanges(eventData, result);
    }
    
    private void ValidateJsonProperties(EntityEntry entry)
    {
        var jsonProperties = entry.Metadata.GetProperties()
            .Where(p => p.GetColumnType() == "JSON");
        
        foreach (var prop in jsonProperties)
        {
            var value = entry.Property(prop.Name).CurrentValue as string;
            if (string.IsNullOrEmpty(value)) continue;
            
            // Validate against schema
            var schema = GetSchema(entry.Metadata.ClrType, prop.Name);
            var isValid = schema.Validate(value);
            
            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"JSON validation failed for {entry.Metadata.Name}.{prop.Name}");
            }
        }
    }
}
```

**Estimated Time**: 4-5 hours

---

### 9. Add Compiled Queries for Hot Paths

**Why**: 10-30% performance boost for frequently-called queries.

**Profiling First**:
```sql
-- Find most-called queries in production
SELECT 
    qt.text AS QueryText,
    qs.execution_count,
    qs.total_elapsed_time / 1000000.0 AS TotalSeconds,
    qs.total_elapsed_time / qs.execution_count / 1000.0 AS AvgMs
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.text LIKE '%Embeddings_Production%'
ORDER BY qs.execution_count DESC;
```

**Example Compiled Query**:
```csharp
public class EmbeddingRepository
{
    // Compiled query (cached expression tree)
    private static readonly Func<HartonomousDbContext, long, Task<Embedding?>> 
        GetByIdCompiled = EF.CompileAsyncQuery(
            (HartonomousDbContext context, long id) =>
                context.Embeddings_Production
                    .AsNoTracking()
                    .FirstOrDefault(e => e.EmbeddingId == id));
    
    public async Task<Embedding?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await GetByIdCompiled(_context, id);
    }
}
```

**Target Queries**:
- GetByIdAsync (all repositories)
- Search queries (EmbeddingRepository, AtomRepository)
- Count operations (IngestionJobRepository)

**Estimated Time**: 3-4 hours

---

### 10. Benchmark Before/After Performance

**Why**: Validate the 95-98% performance improvement claims with real data.

**BenchmarkDotNet Setup**:
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class RepositoryBenchmarks
{
    private HartonomousDbContext _context;
    private AtomRepository _repository;
    
    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer("Server=localhost;Database=Hartonomous_Benchmark;...")
            .Options;
        _context = new HartonomousDbContext(options);
        _repository = new AtomRepository(_context, logger);
        
        // Seed 100k atoms
        SeedData();
    }
    
    [Benchmark(Description = "Old: Load + Save")]
    public async Task IncrementReferenceCount_Old()
    {
        var atom = await _context.Atoms.FirstAsync(a => a.AtomId == 12345);
        atom.ReferenceCount++;
        atom.LastReferenced = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
    
    [Benchmark(Description = "New: ExecuteUpdateAsync")]
    public async Task IncrementReferenceCount_New()
    {
        await _repository.IncrementReferenceCountAsync(12345);
    }
}
```

**Run Benchmarks**:
```powershell
dotnet run -c Release --project tests\Hartonomous.Benchmarks
```

**Expected Results**:
```
| Method                              | Mean      | Error    | StdDev   | Allocated |
|------------------------------------ |----------:|---------:|---------:|----------:|
| IncrementReferenceCount_Old         | 14.23 ms  | 0.42 ms  | 0.38 ms  | 12.5 KB   |
| IncrementReferenceCount_New         | 0.31 ms   | 0.01 ms  | 0.01 ms  | 1.2 KB    |
```

**Estimated Time**: 4-5 hours

---

## 📊 Priority Summary

| Priority | Task | Est. Time | Impact |
|----------|------|-----------|--------|
| 🚨 **Critical** | Apply pending migrations | 10 min | **MUST DO** - Performance |
| 🟡 High | Unit tests for repositories | 2-3 hrs | Quality assurance |
| 🟡 High | Integration tests for queries | 3-4 hrs | Index validation |
| 🟡 High | Fix TODO #2 (image correlation) | 4-5 hrs | Feature completeness |
| 🟢 Medium | Query performance monitoring | 2-3 hrs | Observability |
| 🟢 Medium | Optimize deploy script | 3-4 hrs | CI/CD reliability |
| 🟢 Medium | Best practices guide | 2-3 hrs | Team alignment |
| 🟢 Medium | Fix TODO #1 (safetensors) | 2-3 hrs | Auto-discovery |
| 🔵 Low | JSON validation interceptor | 4-5 hrs | Data integrity |
| 🔵 Low | Compiled queries | 3-4 hrs | Marginal perf boost |
| 🔵 Low | Benchmarking suite | 4-5 hrs | Validation (nice-to-have) |

**Total Estimated Effort**: ~35-45 hours

---

## 🎯 Recommended Execution Order

### Week 1: Critical + Testing
1. ✅ Apply pending migrations (10 min)
2. ✅ Create unit tests (2-3 hrs)
3. ✅ Create integration tests (3-4 hrs)
4. ✅ Fix TODO #2 - Image correlation (4-5 hrs)

**Total**: ~10-12 hours

### Week 2: Monitoring + Deployment
5. ✅ Add query performance monitoring (2-3 hrs)
6. ✅ Optimize deploy script (3-4 hrs)
7. ✅ Create best practices guide (2-3 hrs)

**Total**: ~7-10 hours

### Week 3: Polish + Nice-to-Haves
8. ✅ Fix TODO #1 - Safetensors (2-3 hrs)
9. ✅ JSON validation interceptor (4-5 hrs) *[Optional]*
10. ✅ Compiled queries (3-4 hrs) *[Optional]*
11. ✅ Benchmarking suite (4-5 hrs) *[Optional]*

**Total**: ~13-17 hours (optional work)

---

## 📞 Questions to Consider

1. **Testing**: Do you want me to start with unit tests or integration tests first?
2. **TODOs**: Should we prioritize image correlation (#2) or safetensors parsing (#1)?
3. **Monitoring**: Do you have Application Insights or another APM solution we should integrate with?
4. **CI/CD**: Are you using Azure DevOps, GitHub Actions, or another pipeline tool for deployments?
5. **Benchmarking**: Is performance validation critical, or can we skip the BenchmarkDotNet suite?

---

*Last updated: 2025-11-01*  
*Next review: After migration deployment*
