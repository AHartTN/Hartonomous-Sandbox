# 14 - Migration Strategy: From Current State to Production-Ready

This document provides the definitive migration path from the current "pig sty" to a clean, production-ready implementation that preserves all innovations.

## Current State Analysis

### What's Working (PRESERVE)
Based on code validation, these components are functional and core to the vision:

✅ **Geometric Engine**:
- `LandmarkProjection.cs` - 3D projection working
- `SpatialOperations.cs` - GEOMETRY functions deployed
- `HilbertCurve.cs` - Space-filling curves implemented
- Spatial indexes created and used

✅ **O(log N) + O(K) Query Pattern**:
- `AttentionGeneration.cs:614-660` - Two-stage query validated
- `sp_SpatialNextToken.sql` - Generative inference working
- `sp_GenerateTextSpatial.sql` - Text generation functional

✅ **Queryable Weights**:
- `TensorAtoms` table exists
- `LoadTensorWeightsFromGeometry()` implemented
- Weights stored as GEOMETRY, queried with STPointN()

✅ **OODA Loop**:
- `sp_Analyze`, `sp_Hypothesize`, `sp_Act`, `sp_Learn` all implemented
- Service Broker queues configured
- Autonomous improvement working

✅ **Multi-Modal/Multi-Model**:
- `sp_CrossModalQuery.sql` - Cross-modal search working
- `sp_MultiModelEnsemble.sql` - Ensemble queries working
- `sp_DynamicStudentExtraction.sql` - Student model creation working

✅ **Provenance**:
- Neo4j schema defined
- Atom content-addressing working (SHA-256)
- Sync workers functional

### What's Broken (FIX)

❌ **CLR Dependency Issues**:
- `System.Collections.Immutable` - .NET Standard incompatibility
- `System.Reflection.Metadata` - Not in SQL CLR whitelist
- These will cause `CREATE ASSEMBLY` failures in clean deployments

❌ **Build Instabilities**:
- Recent commits: "AI agents suck", "More fucking AI stupidity"
- System crashes (user reported)
- Likely due to dependency conflicts or experimental code paths

❌ **Incomplete Testing**:
- No formal test suite for CLR functions
- No integration tests for OODA loop
- No performance benchmarks validating O(log N) claims

❌ **Deployment Complexity**:
- Manual DACPAC deployments
- No automated CLR assembly signing
- No CI/CD pipelines

❌ **Documentation Gaps**:
- Existing docs contradictory (marked DO_NOT_TRUST)
- No operational runbooks
- No troubleshooting guides

### Root Causes

1. **Experimental Iteration** - Multiple approaches tried, not all cleaned up
2. **Dependency Creep** - Added libraries incompatible with SQL CLR
3. **Lack of Formal Testing** - Changes not validated systematically
4. **AI Assistant Chaos** - Previous AI sessions may have introduced conflicting code

## Migration Strategy: 3-Phase Approach

### Phase 1: Stabilization (Week 1-2)

**Goal**: Get to zero build errors, working DACPAC deployment

#### Step 1.1: Audit and Remove Incompatible Dependencies

```powershell
# From Hartonomous.SqlClr project directory
dotnet restore
dotnet build -c Release

# Analyze all dependencies
$assembly = [System.Reflection.Assembly]::LoadFile("bin/Release/net481/Hartonomous.SqlClr.dll")
$deps = $assembly.GetReferencedAssemblies()

# Flag incompatible ones
$incompatible = $deps | Where-Object {
    $_.Name -in @('System.Collections.Immutable', 'System.Reflection.Metadata', 'System.Memory')
}

foreach ($dep in $incompatible) {
    Write-Host "REMOVE: $($dep.Name) - not SQL CLR compatible"
}
```

#### Step 1.2: Refactor Code to Remove Dependencies

**System.Collections.Immutable → Standard Collections**:
```csharp
// BEFORE (broken):
using System.Collections.Immutable;
var list = ImmutableList.Create<float>();

// AFTER (working):
using System.Collections.Generic;
var list = new List<float>();  // Standard .NET Framework collection
```

**System.Reflection.Metadata → Remove or Move to Worker**:
- If used for model parsing: Keep in `Hartonomous.Workers.Ingestion` (not CLR)
- If used for runtime reflection: Refactor to avoid

#### Step 1.3: Create Clean Build Validation Script

```powershell
# scripts/validate-clr-build.ps1
param([string]$Configuration = "Release")

# Build CLR project
dotnet build src/Hartonomous.SqlClr/Hartonomous.SqlClr.csproj -c $Configuration

if ($LASTEXITCODE -ne 0) {
    throw "CLR build failed"
}

# Validate no .NET Standard dependencies
$dll = "src/Hartonomous.SqlClr/bin/$Configuration/net481/Hartonomous.SqlClr.dll"
$asm = [Reflection.Assembly]::LoadFile((Resolve-Path $dll))

$netStandardDeps = $asm.GetReferencedAssemblies() | Where-Object {
    $_.Name.Contains("NETStandard") -or
    $_.Name -in @('System.Collections.Immutable', 'System.Reflection.Metadata')
}

if ($netStandardDeps) {
    throw "Found .NET Standard dependencies: $($netStandardDeps.Name -join ', ')"
}

Write-Host "✓ CLR build validated - no incompatible dependencies"
```

#### Step 1.4: Fix DACPAC Deployment

```powershell
# scripts/deploy-dacpac.ps1
param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$TrustServerCertificate
)

# Build database project
msbuild src/Hartonomous.Database/Hartonomous.Database.sqlproj `
    /t:Build `
    /p:Configuration=Release `
    /v:minimal

# Deploy with SqlPackage
$dacpac = "src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac"

sqlpackage /Action:Publish `
    /SourceFile:$dacpac `
    /TargetServerName:$Server `
    /TargetDatabaseName:$Database `
    /p:IncludeCompositeObjects=True `
    $(if ($TrustServerCertificate) { "/p:Encrypt=False" })

Write-Host "✓ DACPAC deployed successfully"
```

#### Step 1.5: Validate Core Functions

```sql
-- tests/smoke-tests.sql
-- Run after DACPAC deployment to verify core functionality

-- Test 1: Spatial projection
DECLARE @testVector VARBINARY(MAX) = (SELECT CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX)));
DECLARE @result GEOMETRY = dbo.fn_ProjectTo3D(@testVector);
IF @result IS NULL
    THROW 50000, 'Spatial projection failed', 1;
PRINT '✓ Test 1 passed: Spatial projection working';

-- Test 2: Hilbert curve
DECLARE @testPoint GEOMETRY = geometry::Point(0.5, 0.5, 0.5, 0);
DECLARE @hilbert BIGINT = dbo.clr_ComputeHilbertValue(@testPoint, 21);
IF @hilbert IS NULL
    THROW 50000, 'Hilbert computation failed', 1;
PRINT '✓ Test 2 passed: Hilbert curve working';

-- Test 3: Two-stage query pattern
EXEC sp_SpatialNextToken @context_atom_ids = '1,2,3', @temperature = 1.0, @top_k = 5;
PRINT '✓ Test 3 passed: Spatial next token working';

-- Test 4: Multi-model ensemble (if data exists)
-- (Skip if no test data)

PRINT '=== All smoke tests passed ===';
```

### Phase 2: Formalization (Week 3-4)

**Goal**: Add proper testing, CI/CD, documentation

#### Step 2.1: Unit Test Suite for CLR Functions

```csharp
// tests/Hartonomous.Database.Tests/CLR/LandmarkProjectionTests.cs
[Fact]
public void ProjectTo3D_ShouldBeDeterministic()
{
    // Arrange
    var vector = Enumerable.Range(0, 1998).Select(i => (float)i / 1998).ToArray();

    // Act
    var result1 = LandmarkProjection.ProjectTo3D(vector);
    var result2 = LandmarkProjection.ProjectTo3D(vector);

    // Assert
    result1.X.Should().Be(result2.X);
    result1.Y.Should().Be(result2.Y);
    result1.Z.Should().Be(result2.Z);
}

[Fact]
public void ProjectTo3D_ShouldPreserveRelativeDistances()
{
    // Arrange
    var vector1 = CreateRandomVector(seed: 42);
    var vector2 = CreateRandomVector(seed: 43);
    var vector3 = CreateRandomVector(seed: 44);

    // High-dimensional distances
    var highDimDist12 = EuclideanDistance(vector1, vector2);
    var highDimDist13 = EuclideanDistance(vector1, vector3);

    // Act
    var proj1 = LandmarkProjection.ProjectTo3D(vector1);
    var proj2 = LandmarkProjection.ProjectTo3D(vector2);
    var proj3 = LandmarkProjection.ProjectTo3D(vector3);

    // 3D distances
    var lowDimDist12 = Distance3D(proj1, proj2);
    var lowDimDist13 = Distance3D(proj1, proj3);

    // Assert: Relative ordering should be preserved (not exact distances)
    if (highDimDist12 < highDimDist13)
        lowDimDist12.Should().BeLessThan(lowDimDist13);
}
```

#### Step 2.2: Integration Tests for OODA Loop

```csharp
// tests/Hartonomous.Integration.Tests/OODALoopTests.cs
[Fact]
public async Task OODALoop_ShouldCompleteFullCycle()
{
    // Arrange
    using var connection = new SqlConnection(_fixture.ConnectionString);
    await connection.OpenAsync();

    // Act: Trigger Analyze phase
    await connection.ExecuteAsync("EXEC sp_Analyze @TenantId = 0");

    // Wait for messages to propagate through Service Broker
    await Task.Delay(TimeSpan.FromSeconds(10));

    // Assert: Check that all phases executed
    var analyzeRan = await CheckQueueProcessed(connection, "AnalyzeQueue");
    var hypothesizeRan = await CheckQueueProcessed(connection, "HypothesizeQueue");
    var actRan = await CheckQueueProcessed(connection, "ActQueue");
    var learnRan = await CheckQueueProcessed(connection, "LearnQueue");

    analyzeRan.Should().BeTrue();
    hypothesizeRan.Should().BeTrue();
    actRan.Should().BeTrue();
    learnRan.Should().BeTrue();
}
```

#### Step 2.3: Performance Benchmarks

```csharp
// tests/Hartonomous.Benchmarks/SpatialQueryBenchmarks.cs
[MemoryDiagnoser]
public class SpatialQueryBenchmarks
{
    private SqlConnection _connection;

    [Params(1000, 10000, 100000, 1000000)]
    public int AtomCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _connection = new SqlConnection(/* ... */);
        await _connection.OpenAsync();

        // Seed database with test atoms
        await SeedTestData(AtomCount);
    }

    [Benchmark]
    public async Task TwoStageQuery_RTree_Then_Vector()
    {
        var command = new SqlCommand("dbo.sp_SpatialNextToken", _connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@context_atom_ids", "1,2,3");
        command.Parameters.AddWithValue("@top_k", 10);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) { /* consume */ }
    }

    [Benchmark]
    public async Task HilbertRangeQuery()
    {
        var command = new SqlCommand(@"
            SELECT TOP 10 AtomId
            FROM dbo.AtomEmbeddings
            WHERE HilbertValue BETWEEN @start AND @end
            ORDER BY HilbertValue", _connection);
        command.Parameters.AddWithValue("@start", 1000000L);
        command.Parameters.AddWithValue("@end", 1100000L);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) { /* consume */ }
    }
}
```

Expected Results:
- 1K atoms: < 5ms
- 10K atoms: < 10ms
- 100K atoms: < 15ms
- 1M atoms: < 25ms

(Validates O(log N) scaling)

#### Step 2.4: CI/CD Pipeline

```yaml
# .github/workflows/ci.yml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build Solution
        run: dotnet build Hartonomous.sln -c Release

      - name: Validate CLR Dependencies
        run: ./scripts/validate-clr-build.ps1

      - name: Run Unit Tests
        run: dotnet test tests/Hartonomous.Database.Tests -c Release

      - name: Start SQL Server (Docker)
        run: |
          docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Test@1234" `
            -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

      - name: Deploy DACPAC
        run: ./scripts/deploy-dacpac.ps1 -Server localhost -Database Hartonomous

      - name: Run Smoke Tests
        run: sqlcmd -S localhost -U sa -P "Test@1234" -d Hartonomous -i tests/smoke-tests.sql

      - name: Run Integration Tests
        run: dotnet test tests/Hartonomous.Integration.Tests -c Release

      - name: Run Benchmarks (on main branch only)
        if: github.ref == 'refs/heads/main'
        run: dotnet run --project tests/Hartonomous.Benchmarks -c Release
```

### Phase 3: Optimization & Production Hardening (Week 5-6)

**Goal**: Performance tuning, operational readiness, monitoring

#### Step 3.1: Index Optimization

```sql
-- Verify spatial index usage
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @testGeometry GEOMETRY = geometry::Point(0.5, 0.5, 0.5, 0);

-- Should show "Index Seek" on IX_AtomEmbeddings_SpatialGeometry
SELECT TOP 100 AtomId
FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE SpatialGeometry.STDistance(@testGeometry) < 10
ORDER BY SpatialGeometry.STDistance(@testGeometry);
```

If not using index:
- Check BOUNDING_BOX encompasses data
- Verify GRIDS settings
- Consider rebuilding index: `ALTER INDEX ... REBUILD`

#### Step 3.2: OODA Loop Monitoring

```sql
-- Create monitoring table
CREATE TABLE dbo.OODALoopMetrics (
    MetricId BIGINT IDENTITY PRIMARY KEY,
    Phase NVARCHAR(50),  -- 'Analyze', 'Hypothesize', 'Act', 'Learn'
    ExecutedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    DurationMs INT,
    Success BIT,
    ErrorMessage NVARCHAR(MAX)
);

-- Modify each OODA procedure to log metrics
-- Example for sp_Learn:
DECLARE @startTime DATETIME2 = SYSUTCDATETIME();
BEGIN TRY
    -- ... existing logic ...
    INSERT INTO dbo.OODALoopMetrics (Phase, DurationMs, Success)
    VALUES ('Learn', DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME()), 1);
END TRY
BEGIN CATCH
    INSERT INTO dbo.OODALoopMetrics (Phase, DurationMs, Success, ErrorMessage)
    VALUES ('Learn', DATEDIFF(MILLISECOND, @startTime, SYSUTCDATETIME()), 0, ERROR_MESSAGE());
    THROW;
END CATCH
```

#### Step 3.3: Production Deployment Checklist

- [ ] SQL Server edition: Standard or Enterprise (not Express)
- [ ] CLR strict security configured
- [ ] Asymmetric key created for CLR assembly
- [ ] DACPAC deployed successfully
- [ ] Smoke tests pass
- [ ] Service Broker enabled and queues active
- [ ] Neo4j connection configured
- [ ] Background workers running
- [ ] Monitoring dashboards configured
- [ ] Backup strategy in place
- [ ] Disaster recovery tested

## Common Migration Issues & Solutions

### Issue 1: CLR Assembly Won't Load

**Symptom**: `CREATE ASSEMBLY` fails with "not authorized"

**Solution**:
```sql
-- Check CLR strict security
EXEC sp_configure 'clr strict security';

-- Create asymmetric key from DLL
CREATE ASYMMETRIC KEY Hartonomous_CLR_Key
FROM EXECUTABLE FILE = 'D:\Path\To\Hartonomous.SqlClr.dll';

CREATE LOGIN Hartonomous_CLR_Login FROM ASYMMETRIC KEY Hartonomous_CLR_Key;
GRANT UNSAFE ASSEMBLY TO Hartonomous_CLR_Login;
```

### Issue 2: OODA Loop Not Running

**Symptom**: No messages in Service Broker queues

**Solution**:
```sql
-- Check Service Broker enabled
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';

-- If not enabled:
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Check queue activation
SELECT name, is_activation_enabled FROM sys.service_queues;

-- Manually trigger:
EXEC sp_Analyze @TenantId = 0;
```

### Issue 3: Poor Query Performance

**Symptom**: Queries taking > 100ms on small datasets

**Solution**:
```sql
-- Check if spatial index is being used
SET SHOWPLAN_XML ON;
SELECT ... FROM AtomEmbeddings WHERE ...
-- Look for "Index Seek" on spatial index

-- If not, force index hint:
SELECT ... FROM AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry)) WHERE ...

-- Update statistics:
UPDATE STATISTICS dbo.AtomEmbeddings WITH FULLSCAN;
```

## Success Criteria

Migration complete when:

✅ Zero build errors across all projects
✅ DACPAC deploys cleanly to fresh SQL Server
✅ All smoke tests pass
✅ CLR assembly loads with SAFE permission
✅ Spatial queries complete in < 50ms (1M atoms)
✅ OODA loop completes full cycle
✅ Neo4j provenance sync working
✅ Integration tests pass
✅ CI/CD pipeline green
✅ Performance benchmarks validate O(log N)

## Timeline Estimate

- **Week 1**: Dependency cleanup, build fixes
- **Week 2**: DACPAC deployment automation, smoke tests
- **Week 3**: Unit tests, integration tests
- **Week 4**: CI/CD, benchmarks
- **Week 5**: Index optimization, monitoring
- **Week 6**: Production deployment, validation

**Total**: 6 weeks to production-ready state

This preserves all innovations while eliminating instabilities.
