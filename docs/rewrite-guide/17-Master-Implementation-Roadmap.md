# 17 - Master Implementation Roadmap

Complete step-by-step guide to execute the Hartonomous rewrite from current state to production.

## Overview: 6-Week Timeline

| Week | Phase | Deliverables | Success Criteria |
|---|---|---|---|
| 1-2 | Stabilization | Zero build errors, clean DACPAC deployment | CI passes, smoke tests green |
| 3-4 | Testing & Validation | Unit/integration tests, benchmarks | O(log N) proven, OODA validated |
| 5-6 | Production Hardening | Monitoring, docs, deployment automation | Production-ready checklist complete |

## Week 1: Dependency Cleanup & Build Fixes

### Day 1: Audit Current State

**Tasks**:
1. Clone fresh repo, attempt clean build
2. Document all build errors
3. List all .NET Standard dependencies in CLR project

**Script**:
```powershell
# audit-dependencies.ps1
git clone https://github.com/yourusername/Hartonomous.git hartonomous-audit
cd hartonomous-audit

# Attempt build
dotnet build Hartonomous.sln -c Release 2>&1 | Tee-Object build-errors.txt

# Analyze CLR dependencies
cd src/Hartonomous.SqlClr
dotnet restore
$dll = "bin/Release/net481/Hartonomous.SqlClr.dll"
if (Test-Path $dll) {
    $asm = [Reflection.Assembly]::LoadFile((Resolve-Path $dll))
    $deps = $asm.GetReferencedAssemblies()
    $deps | Select-Object Name, Version | Export-Csv ../../clr-dependencies.csv
}
```

**Deliverable**: `audit-report.md` with:
- List of build errors
- List of incompatible dependencies
- Current test coverage (likely zero)

### Day 2-3: Remove Incompatible Dependencies

**Target Dependencies to Remove**:
- `System.Collections.Immutable`
- `System.Reflection.Metadata`
- `System.Memory` (if present)
- Any other .NET Standard libraries

**Refactoring Strategy**:

**Before** (broken):
```csharp
using System.Collections.Immutable;

public class ModelParser
{
    private ImmutableList<float> weights;

    public void LoadWeights(byte[] data)
    {
        var builder = ImmutableList.CreateBuilder<float>();
        // ...
        weights = builder.ToImmutable();
    }
}
```

**After** (working):
```csharp
using System.Collections.Generic;

public class ModelParser
{
    private List<float> weights;  // Standard .NET Framework collection

    public void LoadWeights(byte[] data)
    {
        weights = new List<float>();
        // ...
        // If immutability needed, use AsReadOnly()
        return weights.AsReadOnly();
    }
}
```

**For Each Incompatible Dependency**:
1. Search for all usages: `rg "using System.Collections.Immutable"`
2. Refactor to standard .NET Framework alternatives
3. Commit with message: `fix: remove {dependency} from CLR project`
4. Build and verify

**Deliverable**: CLR project builds with zero .NET Standard dependencies

### Day 4: Validate Clean Build

**Tasks**:
1. Run validation script (from doc 14)
2. Build entire solution
3. Verify DACPAC generation

**Script**:
```powershell
# validate-clean-build.ps1
./scripts/validate-clr-build.ps1

# Build solution
dotnet build Hartonomous.sln -c Release

# Build DACPAC
msbuild src/Hartonomous.Database/Hartonomous.Database.sqlproj `
    /t:Build /p:Configuration=Release /v:minimal

if (Test-Path "src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac") {
    Write-Host "✓ DACPAC generated successfully" -ForegroundColor Green
} else {
    throw "DACPAC generation failed"
}
```

**Success Criteria**:
- ✅ `dotnet build` exits with code 0
- ✅ No .NET Standard dependencies in CLR DLL
- ✅ DACPAC file generated

### Day 5: Automated DACPAC Deployment

**Create Deployment Script** (`scripts/deploy-dacpac.ps1`):
```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,

    [Parameter(Mandatory=$true)]
    [string]$Database,

    [string]$User = "sa",
    [string]$Password,
    [switch]$IntegratedSecurity,
    [switch]$TrustServerCertificate
)

# Build DACPAC
Write-Host "Building DACPAC..." -ForegroundColor Cyan
msbuild src/Hartonomous.Database/Hartonomous.Database.sqlproj `
    /t:Build /p:Configuration=Release /v:minimal /nologo

$dacpac = "src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac"

if (-not (Test-Path $dacpac)) {
    throw "DACPAC not found at $dacpac"
}

# Deploy
Write-Host "Deploying DACPAC to $Server/$Database..." -ForegroundColor Cyan

$connectionString = if ($IntegratedSecurity) {
    "Server=$Server;Database=$Database;Integrated Security=True;"
} else {
    "Server=$Server;Database=$Database;User=$User;Password=$Password;"
}

if ($TrustServerCertificate) {
    $connectionString += "TrustServerCertificate=True;"
}

sqlpackage /Action:Publish `
    /SourceFile:$dacpac `
    /TargetConnectionString:$connectionString `
    /p:IncludeCompositeObjects=True `
    /p:BlockOnPossibleDataLoss=False

Write-Host "✓ Deployment complete" -ForegroundColor Green
```

**Test**:
```powershell
# Deploy to local SQL Server
./scripts/deploy-dacpac.ps1 `
    -Server "localhost" `
    -Database "Hartonomous_Test" `
    -IntegratedSecurity `
    -TrustServerCertificate
```

**Success Criteria**:
- ✅ Script deploys to fresh database without errors
- ✅ CLR assembly loads successfully
- ✅ Spatial indexes created

## Week 2: Core Functionality Validation

### Day 6-7: Smoke Tests

**Create** `tests/smoke-tests.sql`:
```sql
-- Test 1: CLR Functions Available
PRINT 'Test 1: CLR Functions...';
IF OBJECT_ID('dbo.fn_ProjectTo3D') IS NULL
    THROW 50000, 'fn_ProjectTo3D not found', 1;
IF OBJECT_ID('dbo.clr_ComputeHilbertValue') IS NULL
    THROW 50000, 'clr_ComputeHilbertValue not found', 1;
PRINT '  ✓ CLR functions exist';

-- Test 2: Spatial Projection Works
PRINT 'Test 2: Spatial Projection...';
DECLARE @testVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
DECLARE @projected GEOMETRY = dbo.fn_ProjectTo3D(@testVec);
IF @projected IS NULL
    THROW 50000, 'Projection returned NULL', 1;
IF @projected.STX IS NULL OR @projected.STY IS NULL
    THROW 50000, 'Invalid projection coordinates', 1;
PRINT '  ✓ Projection working';

-- Test 3: Hilbert Curve
PRINT 'Test 3: Hilbert Curve...';
DECLARE @point GEOMETRY = geometry::Point(0.5, 0.5, 0.5, 0);
DECLARE @hilbert BIGINT = dbo.clr_ComputeHilbertValue(@point, 21);
IF @hilbert IS NULL OR @hilbert <= 0
    THROW 50000, 'Invalid Hilbert value', 1;
PRINT '  ✓ Hilbert curve working';

-- Test 4: Spatial Indexes Exist
PRINT 'Test 4: Spatial Indexes...';
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AtomEmbeddings_SpatialGeometry'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
    THROW 50000, 'Spatial index missing', 1;
PRINT '  ✓ Spatial indexes exist';

-- Test 5: Service Broker Enabled
PRINT 'Test 5: Service Broker...';
IF NOT EXISTS (
    SELECT 1 FROM sys.databases
    WHERE name = DB_NAME() AND is_broker_enabled = 1
)
    THROW 50000, 'Service Broker not enabled', 1;
PRINT '  ✓ Service Broker enabled';

-- Test 6: OODA Procedures Exist
PRINT 'Test 6: OODA Loop Procedures...';
IF OBJECT_ID('dbo.sp_Analyze') IS NULL
    THROW 50000, 'sp_Analyze not found', 1;
IF OBJECT_ID('dbo.sp_Hypothesize') IS NULL
    THROW 50000, 'sp_Hypothesize not found', 1;
IF OBJECT_ID('dbo.sp_Act') IS NULL
    THROW 50000, 'sp_Act not found', 1;
IF OBJECT_ID('dbo.sp_Learn') IS NULL
    THROW 50000, 'sp_Learn not found', 1;
PRINT '  ✓ OODA procedures exist';

PRINT '';
PRINT '=================================';
PRINT '   ALL SMOKE TESTS PASSED ✓';
PRINT '=================================';
```

**Run**:
```powershell
sqlcmd -S localhost -d Hartonomous_Test -i tests/smoke-tests.sql
```

**Success Criteria**:
- ✅ All tests pass

### Day 8-9: Sample Data & End-to-End Test

**Seed Sample Data**:
```sql
-- seed-sample-data.sql
-- Insert test atoms
INSERT INTO dbo.Atoms (ContentHash, Modality, AtomicValue, IsActive, CreatedAt)
VALUES
    (HASHBYTES('SHA2_256', 'test1'), 'text', 'The quick brown fox', 1, GETUTCDATE()),
    (HASHBYTES('SHA2_256', 'test2'), 'text', 'jumps over the lazy dog', 1, GETUTCDATE()),
    (HASHBYTES('SHA2_256', 'test3'), 'text', 'Machine learning is fascinating', 1, GETUTCDATE());

-- Get atom IDs
DECLARE @atom1 BIGINT = (SELECT AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'The quick brown fox');
DECLARE @atom2 BIGINT = (SELECT AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'jumps over the lazy dog');
DECLARE @atom3 BIGINT = (SELECT AtomId FROM dbo.Atoms WHERE CONVERT(VARCHAR(MAX), AtomicValue) = 'Machine learning is fascinating');

-- Insert embeddings (dummy vectors for testing)
DECLARE @dummyVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));

INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingVector, EmbeddingType, ModelId, Dimension, CreatedAt)
VALUES
    (@atom1, @dummyVec, 'test-embedding', 1, 1998, GETUTCDATE()),
    (@atom2, @dummyVec, 'test-embedding', 1, 1998, GETUTCDATE()),
    (@atom3, @dummyVec, 'test-embedding', 1, 1998, GETUTCDATE());

-- Project to spatial geometry
UPDATE ae
SET SpatialGeometry = dbo.fn_ProjectTo3D(ae.EmbeddingVector)
FROM dbo.AtomEmbeddings ae
WHERE SpatialGeometry IS NULL;

-- Compute Hilbert values
UPDATE ae
SET HilbertValue = dbo.clr_ComputeHilbertValue(ae.SpatialGeometry, 21)
FROM dbo.AtomEmbeddings ae
WHERE HilbertValue IS NULL AND SpatialGeometry IS NOT NULL;

PRINT '✓ Sample data seeded';
```

**End-to-End Test**:
```sql
-- Test spatial query
DECLARE @testGeometry GEOMETRY = (
    SELECT TOP 1 SpatialGeometry
    FROM dbo.AtomEmbeddings
);

-- Execute O(log N) + O(K) query
EXEC sp_SpatialNextToken
    @context_atom_ids = '1,2',
    @temperature = 1.0,
    @top_k = 3;

-- Should return results
PRINT '✓ Spatial query executed successfully';
```

### Day 10: CI/CD Pipeline Setup

**Create** `.github/workflows/ci.yml` (from doc 16)

**Test Locally**:
```powershell
# Install act (GitHub Actions local runner)
choco install act-cli

# Run CI locally
act -j build-and-test
```

**Push to GitHub**:
```powershell
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions pipeline"
git push origin main
```

**Success Criteria**:
- ✅ CI pipeline runs without errors
- ✅ All tests pass in CI

## Week 3: Unit & Integration Testing

### Day 11-12: CLR Unit Tests

**Create** `tests/Hartonomous.Database.Tests/CLR/`:

```csharp
// LandmarkProjectionTests.cs
public class LandmarkProjectionTests
{
    [Fact]
    public void ProjectTo3D_WithZeroVector_ShouldReturnOrigin()
    {
        var zero = new float[1998];
        var result = LandmarkProjection.ProjectTo3D(zero);

        result.X.Should().BeApproximately(0, 0.001);
        result.Y.Should().BeApproximately(0, 0.001);
        result.Z.Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void ProjectTo3D_IsDeterministic()
    {
        var vector = CreateRandomVector(seed: 42);
        var result1 = LandmarkProjection.ProjectTo3D(vector);
        var result2 = LandmarkProjection.ProjectTo3D(vector);

        result1.X.Should().Be(result2.X);
        result1.Y.Should().Be(result2.Y);
        result1.Z.Should().Be(result2.Z);
    }
}

// HilbertCurveTests.cs
public class HilbertCurveTests
{
    [Fact]
    public void ComputeHilbertValue_ShouldPreserveLocality()
    {
        var point1 = CreatePoint(0.5, 0.5, 0.5);
        var point2 = CreatePoint(0.51, 0.51, 0.51);  // Close
        var point3 = CreatePoint(0.9, 0.9, 0.9);  // Far

        var h1 = SpatialFunctions.clr_ComputeHilbertValue(point1, 21);
        var h2 = SpatialFunctions.clr_ComputeHilbertValue(point2, 21);
        var h3 = SpatialFunctions.clr_ComputeHilbertValue(point3, 21);

        Math.Abs(h1 - h2).Should().BeLessThan(Math.Abs(h1 - h3));
    }
}
```

**Run**:
```powershell
dotnet test tests/Hartonomous.Database.Tests
```

**Target**: 80%+ code coverage on CLR functions

### Day 13-14: Integration Tests

**Create** `tests/Hartonomous.Integration.Tests/`:

```csharp
// SpatialQueryIntegrationTests.cs
[Collection("Database")]
public class SpatialQueryIntegrationTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task SpatialQuery_WithLargeDataset_CompletesQuickly()
    {
        // Arrange: Seed 10K atoms
        await _fixture.SeedAtomsAsync(count: 10000);

        // Act
        var sw = Stopwatch.StartNew();
        var results = await _connection.QueryAsync(@"
            EXEC sp_SpatialNextToken @context_atom_ids = '1,2,3', @top_k = 10");
        sw.Stop();

        // Assert
        results.Should().HaveCount(10);
        sw.ElapsedMilliseconds.Should().BeLessThan(50);  // O(log N) performance
    }
}

// OODALoopIntegrationTests.cs (from doc 14)
```

### Day 15: Performance Benchmarks

**Create** `tests/Hartonomous.Benchmarks/` (from doc 14)

**Run Benchmarks**:
```powershell
dotnet run --project tests/Hartonomous.Benchmarks -c Release
```

**Document Results**:
```markdown
# Benchmark Results (2025-01-15)

| Dataset Size | Avg Query Time | p95 | p99 |
|---|---|---|---|
| 1K atoms | 3.2ms | 4.1ms | 5.3ms |
| 10K atoms | 7.8ms | 9.2ms | 11.4ms |
| 100K atoms | 14.3ms | 17.1ms | 21.8ms |
| 1M atoms | 23.7ms | 28.4ms | 35.2ms |

**Conclusion**: O(log N) scaling validated ✓
```

## Week 4: Documentation & Knowledge Transfer

### Day 16-17: Update Existing Docs

**Review docs 00-10**, ensure they align with:
- Corrected understanding (spatial indexes ARE the ANN)
- Full vision (OODA, Gödel, multi-modal/multi-model)

**Update as needed**

### Day 18-19: Create Operational Runbooks

**Create** `docs/operations/`:
- `runbook-deployment.md` - Step-by-step deployment
- `runbook-troubleshooting.md` - Common issues & fixes
- `runbook-backup-recovery.md` - DR procedures
- `runbook-monitoring.md` - What to watch, alert thresholds

### Day 20: Team Training Session

**Agenda**:
1. Architecture overview (1 hour)
   - Geometric AI concept
   - O(log N) + O(K) explained
   - Live demo of spatial query

2. OODA loop deep dive (1 hour)
   - How it works
   - What it optimizes
   - How to monitor it

3. Operations hands-on (2 hours)
   - Deploy DACPAC
   - Run smoke tests
   - Trigger OODA loop manually
   - View Grafana dashboards

4. Q&A (30 min)

**Deliverable**: Recorded video for future reference

## Week 5: Production Hardening

### Day 21-22: Monitoring Setup

**Deploy** (from doc 16):
- Application Insights
- Grafana dashboards
- SQL Server alerts
- Service Broker queue monitoring

**Test**:
- Trigger alerts manually
- Verify notifications work

### Day 23-24: Security Hardening

**Tasks**:
- [ ] Create non-admin SQL login for application
- [ ] Configure firewall rules (SQL: 1433, Neo4j: 7687)
- [ ] Enable SQL Server encryption
- [ ] Configure JWT authentication on API
- [ ] Scan for vulnerabilities (OWASP ZAP)

### Day 25: Load Testing

**Use k6 or JMeter**:
```javascript
// load-test.js (k6)
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp up
    { duration: '5m', target: 100 },  // Sustain
    { duration: '2m', target: 0 },    // Ramp down
  ],
};

export default function () {
  let res = http.post('http://api/inference', JSON.stringify({
    query: 'test query',
    topK: 10
  }), {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
```

**Run**:
```bash
k6 run tests/load-test.js
```

**Success Criteria**:
- ✅ System handles 100 concurrent users
- ✅ p95 latency < 500ms
- ✅ Zero errors

## Week 6: Final Prep & Production Deployment

### Day 26: Pre-Production Checklist

**Infrastructure**:
- [ ] Production SQL Server provisioned
- [ ] Production Neo4j provisioned
- [ ] Network configured (VLANs, firewalls)
- [ ] Backups configured and tested
- [ ] Monitoring dashboards live
- [ ] Alerting configured

**Application**:
- [ ] All tests passing in CI
- [ ] Benchmarks show expected performance
- [ ] Security scan clean
- [ ] Load test successful
- [ ] Documentation complete

**Team**:
- [ ] On-call rotation defined
- [ ] Runbooks reviewed
- [ ] Rollback plan documented
- [ ] Stakeholders notified

### Day 27-28: Staged Deployment

**Stage 1: Deploy to Staging**
```powershell
./scripts/deploy-dacpac.ps1 `
    -Server "staging-sql.internal" `
    -Database "Hartonomous" `
    -User "hartonomous_app" `
    -Password $env:STAGING_PASSWORD

# Run full test suite against staging
dotnet test --filter Category=Staging
```

**Stage 2: Smoke Test Staging**
```powershell
Invoke-WebRequest https://staging-api/health | ConvertFrom-Json
# Should return: { "status": "healthy" }
```

**Stage 3: Production Deployment (if staging green)**
```powershell
# Create database backup first
sqlcmd -S prod-sql -Q "BACKUP DATABASE Hartonomous TO DISK = '/backups/pre-deployment.bak'"

# Deploy
./scripts/deploy-dacpac.ps1 `
    -Server "prod-sql.internal" `
    -Database "Hartonomous" `
    -User "hartonomous_app" `
    -Password $env:PROD_PASSWORD

# Run smoke tests
sqlcmd -S prod-sql -d Hartonomous -i tests/smoke-tests.sql
```

### Day 29: Post-Deployment Validation

**24-Hour Monitoring**:
- [ ] No errors in Application Insights
- [ ] OODA loop running normally
- [ ] Query latencies within SLA
- [ ] No degradation in user experience

### Day 30: Retrospective & Documentation

**Retrospective**:
- What went well?
- What could be improved?
- Lessons learned

**Final Documentation**:
- Update README with production URLs
- Document any deployment gotchas
- Archive all runbooks

## Success Metrics

### Technical Metrics
- ✅ Zero build errors
- ✅ 100% smoke tests passing
- ✅ 80%+ unit test coverage
- ✅ O(log N) scaling validated
- ✅ OODA loop functional
- ✅ p95 latency < 100ms (1M atoms)

### Operational Metrics
- ✅ Automated deployment working
- ✅ CI/CD pipeline green
- ✅ Monitoring dashboards live
- ✅ On-call team trained

### Business Metrics
- ✅ System handles production load
- ✅ No data loss during migration
- ✅ Stakeholders sign-off

## Conclusion

This 6-week roadmap takes Hartonomous from current "pig sty" to production-ready world-changing innovation. Every week builds on the previous, ensuring stability, correctness, and operational excellence.

The key: **Preserve the innovations, eliminate the chaos.**
