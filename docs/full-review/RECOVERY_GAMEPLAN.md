# COMPLETE RECOVERY GAMEPLAN
**Goal**: Get Hartonomous from 70% functional to 100% working system

---

## PHASE 1: CREATE MISSING CRITICAL TABLES (Week 1)

### Task 1.1: Create AtomEmbeddings Table (HIGHEST PRIORITY)

**Why Critical**: Blocks search, generation, autonomous loop - basically everything.

**File**: `sql/tables/dbo.AtomEmbeddings.sql`

```sql
CREATE TABLE dbo.AtomEmbeddings (
    AtomEmbeddingId BIGINT IDENTITY(1,1) NOT NULL,
    AtomId BIGINT NOT NULL,
    EmbeddingVector VECTOR(1998) NOT NULL,
    SpatialGeometry GEOMETRY NOT NULL,
    SpatialCoarse GEOMETRY NOT NULL,
    SpatialBucket INT NOT NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AtomEmbeddings PRIMARY KEY CLUSTERED (AtomEmbeddingId),
    CONSTRAINT FK_AtomEmbeddings_Atoms FOREIGN KEY (AtomId)
        REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    INDEX IX_AtomEmbeddings_Atom NONCLUSTERED (AtomId),
    INDEX IX_AtomEmbeddings_Bucket NONCLUSTERED (SpatialBucket)
);
GO

-- Spatial indexes for trilateration
CREATE SPATIAL INDEX IX_AtomEmbeddings_Spatial
    ON dbo.AtomEmbeddings(SpatialGeometry)
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
GO

CREATE SPATIAL INDEX IX_AtomEmbeddings_Coarse
    ON dbo.AtomEmbeddings(SpatialCoarse)
    WITH (GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW));
GO
```

**Verification**:
```sql
-- Test that sp_SemanticSearch can now find the table
EXEC dbo.sp_SemanticSearch
    @queryEmbedding = (SELECT TOP 1 EmbeddingVector FROM dbo.AtomEmbeddings),
    @topK = 10;
```

### Task 1.2: Create TokenVocabulary Table

**File**: `sql/tables/dbo.TokenVocabulary.sql`

```sql
CREATE TABLE dbo.TokenVocabulary (
    TokenId INT IDENTITY(1,1) NOT NULL,
    Token NVARCHAR(256) NOT NULL,
    VocabularyName NVARCHAR(128) NOT NULL DEFAULT 'default',
    Frequency INT NOT NULL DEFAULT 1,
    DimensionIndex INT NOT NULL,
    IDF FLOAT NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_TokenVocabulary PRIMARY KEY CLUSTERED (TokenId),
    INDEX IX_TokenVocabulary_Token NONCLUSTERED (VocabularyName, Token),
    INDEX IX_TokenVocabulary_Dimension NONCLUSTERED (DimensionIndex)
);
GO

-- Seed with basic vocabulary (top 10,000 English words)
-- This would be a separate seeding script
```

**Verification**:
```sql
-- Test that sp_TextToEmbedding works
EXEC dbo.sp_TextToEmbedding @text = 'hello world', @vocabularyName = 'default';
```

### Task 1.3: Create SpatialLandmarks Table

**File**: `sql/tables/dbo.SpatialLandmarks.sql`

```sql
CREATE TABLE dbo.SpatialLandmarks (
    LandmarkId INT IDENTITY(1,1) NOT NULL,
    LandmarkVector VECTOR(1998) NOT NULL,
    LandmarkPoint GEOMETRY NULL,
    SelectionMethod NVARCHAR(50) NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_SpatialLandmarks PRIMARY KEY CLUSTERED (LandmarkId)
);
GO

-- Initialize with 3 landmarks using sp_InitializeSpatialAnchors
EXEC dbo.sp_InitializeSpatialAnchors;
```

### Task 1.4: Create Model Structure Tables

**File**: `sql/tables/dbo.ModelStructure.sql`

```sql
-- Main models registry
CREATE TABLE dbo.Models (
    ModelId INT IDENTITY(1,1) NOT NULL,
    ModelName NVARCHAR(256) NOT NULL,
    ModelType NVARCHAR(100) NULL,
    ConfigJson NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Models PRIMARY KEY CLUSTERED (ModelId),
    INDEX IX_Models_Name NONCLUSTERED (ModelName)
);
GO

-- Layers within models
CREATE TABLE dbo.ModelLayers (
    LayerId INT IDENTITY(1,1) NOT NULL,
    ModelId INT NOT NULL,
    LayerName NVARCHAR(100) NOT NULL,
    LayerType NVARCHAR(50) NOT NULL,
    NeuronCount INT NOT NULL,
    ActivationFunction NVARCHAR(50) NULL,
    ConfigJson NVARCHAR(MAX) NULL,

    CONSTRAINT PK_ModelLayers PRIMARY KEY CLUSTERED (LayerId),
    CONSTRAINT FK_ModelLayers_Models FOREIGN KEY (ModelId)
        REFERENCES dbo.Models(ModelId) ON DELETE CASCADE,
    INDEX IX_ModelLayers_Model NONCLUSTERED (ModelId)
);
GO

-- Inference tracking
CREATE TABLE dbo.InferenceRequests (
    InferenceId BIGINT IDENTITY(1,1) NOT NULL,
    PromptText NVARCHAR(MAX) NULL,
    GeneratedText NVARCHAR(MAX) NULL,
    ModelIds NVARCHAR(MAX) NULL,
    UserRating TINYINT NULL CHECK (UserRating BETWEEN 1 AND 5),
    DurationMs INT NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_InferenceRequests PRIMARY KEY CLUSTERED (InferenceId),
    INDEX IX_InferenceRequests_Rating NONCLUSTERED (UserRating) WHERE UserRating IS NOT NULL
);
GO

-- Step-level tracking for feedback
CREATE TABLE dbo.InferenceSteps (
    InferenceStepId BIGINT IDENTITY(1,1) NOT NULL,
    InferenceId BIGINT NOT NULL,
    LayerId INT NOT NULL,
    StepType NVARCHAR(50) NULL,
    AtomId BIGINT NULL,
    DurationMs INT NULL,

    CONSTRAINT PK_InferenceSteps PRIMARY KEY CLUSTERED (InferenceStepId),
    CONSTRAINT FK_InferenceSteps_Inference FOREIGN KEY (InferenceId)
        REFERENCES dbo.InferenceRequests(InferenceId) ON DELETE CASCADE,
    INDEX IX_InferenceSteps_Inference NONCLUSTERED (InferenceId),
    INDEX IX_InferenceSteps_Layer NONCLUSTERED (LayerId)
);
GO
```

### Task 1.5: Create or Verify Weights Table

**Option A: Verify TensorAtomCoefficients is the weights table**

```sql
-- Check if table exists
SELECT * FROM sys.tables WHERE name = 'TensorAtomCoefficients';

-- If exists, verify schema matches sp_UpdateModelWeightsFromFeedback expectations
-- The procedure uses: LayerID, Value, LastUpdated, UpdateCount
-- But TensorAtomCoefficients uses: TensorAtomCoefficientId, TensorAtomId, ParentLayerId, Coefficient

-- Schema mismatch! Need to either:
-- 1. Update sp_UpdateModelWeightsFromFeedback to use TensorAtomCoefficients schema
-- 2. Create separate Weights table
```

**Option B: Create dedicated Weights table**

**File**: `sql/tables/dbo.Weights.sql`

```sql
CREATE TABLE dbo.Weights (
    WeightId BIGINT IDENTITY(1,1) NOT NULL,
    LayerID INT NOT NULL,
    NeuronIndex INT NOT NULL,
    WeightType NVARCHAR(50) NOT NULL DEFAULT 'Parameter',  -- 'Parameter', 'Bias', 'Attention'
    Value REAL NOT NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdateCount INT NOT NULL DEFAULT 0,

    CONSTRAINT PK_Weights PRIMARY KEY CLUSTERED (WeightId),
    CONSTRAINT FK_Weights_Layers FOREIGN KEY (LayerID)
        REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE,
    INDEX IX_Weights_Layer NONCLUSTERED (LayerID, NeuronIndex)
);
GO
```

**Recommended**: Option B - sp_UpdateModelWeightsFromFeedback is already written for this schema.

### Task 1.6: Create Autonomous System Tables

**File**: `sql/tables/dbo.AutonomousSystem.sql`

```sql
-- Pending actions queue
CREATE TABLE dbo.PendingActions (
    ActionId BIGINT IDENTITY(1,1) NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    SqlStatement NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'PendingApproval',
    RiskLevel NVARCHAR(20) NOT NULL DEFAULT 'SAFE',
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedUtc DATETIME2 NULL,
    ApprovedBy NVARCHAR(128) NULL,
    ExecutedUtc DATETIME2 NULL,
    ResultJson NVARCHAR(MAX) NULL,

    CONSTRAINT PK_PendingActions PRIMARY KEY CLUSTERED (ActionId),
    INDEX IX_PendingActions_Status NONCLUSTERED (Status)
);
GO
```

**Verification**:
```sql
-- Test that sp_Act can queue dangerous actions
EXEC dbo.sp_Act @DryRun = 1, @RequireHumanApproval = 1;
SELECT * FROM dbo.PendingActions;
```

---

## PHASE 2: RESTORE EF CORE CONFIGURATIONS (Week 1-2)

### Task 2.1: Extract Deleted Configurations from Git

```bash
# Extract from commit 09fd7fe (before sabotage)
git show 09fd7fe:src/Hartonomous.Infrastructure/Data/Configurations/AtomEmbeddingConfiguration.cs > AtomEmbeddingConfiguration.cs

# Or create from scratch based on entity class
```

### Task 2.2: Create AtomEmbeddingConfiguration

**File**: `src/Hartonomous.Infrastructure/Data/Configurations/AtomEmbeddingConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hartonomous.Core.Entities;

namespace Hartonomous.Infrastructure.Data.Configurations
{
    public class AtomEmbeddingConfiguration : IEntityTypeConfiguration<AtomEmbedding>
    {
        public void Configure(EntityTypeBuilder<AtomEmbedding> builder)
        {
            builder.ToTable("AtomEmbeddings", "dbo");

            builder.HasKey(e => e.AtomEmbeddingId);

            builder.Property(e => e.EmbeddingVector)
                .HasColumnType("VECTOR(1998)")
                .IsRequired();

            builder.Property(e => e.SpatialGeometry)
                .HasColumnType("GEOMETRY")
                .IsRequired();

            builder.Property(e => e.SpatialCoarse)
                .HasColumnType("GEOMETRY")
                .IsRequired();

            builder.Property(e => e.SpatialBucket)
                .IsRequired();

            builder.HasOne(e => e.Atom)
                .WithMany(a => a.Embeddings)
                .HasForeignKey(e => e.AtomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.AtomId)
                .HasDatabaseName("IX_AtomEmbeddings_Atom");

            builder.HasIndex(e => e.SpatialBucket)
                .HasDatabaseName("IX_AtomEmbeddings_Bucket");
        }
    }
}
```

### Task 2.3: Create Entity Classes (if missing)

**File**: `src/Hartonomous.Core/Entities/AtomEmbedding.cs`

```csharp
namespace Hartonomous.Core.Entities
{
    public class AtomEmbedding
    {
        public long AtomEmbeddingId { get; set; }
        public long AtomId { get; set; }
        public byte[] EmbeddingVector { get; set; }  // VECTOR serialized as bytes
        public byte[] SpatialGeometry { get; set; }   // GEOMETRY serialized
        public byte[] SpatialCoarse { get; set; }
        public int SpatialBucket { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedUtc { get; set; }

        // Navigation
        public Atom Atom { get; set; }
    }
}
```

### Task 2.4: Register Configurations in DbContext

**File**: `src/Hartonomous.Infrastructure/Data/HartonomousDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Add all configurations
    modelBuilder.ApplyConfiguration(new AtomConfiguration());
    modelBuilder.ApplyConfiguration(new AtomEmbeddingConfiguration());  // NEW
    modelBuilder.ApplyConfiguration(new ModelLayerConfiguration());      // NEW
    modelBuilder.ApplyConfiguration(new InferenceRequestConfiguration()); // NEW
    // ... all other configurations
}
```

### Task 2.5: Repeat for All Missing Configurations

1. ModelLayerConfiguration
2. InferenceRequestConfiguration
3. InferenceStepConfiguration
4. WeightsConfiguration
5. TokenVocabularyConfiguration
6. SpatialLandmarksConfiguration
7. PendingActionsConfiguration

---

## PHASE 3: RESTORE MODEL INGESTION PROJECT (Week 2)

### Task 3.1: Extract from Git

```bash
cd D:\Repositories\Hartonomous

# List all files that were in ModelIngestion
git ls-tree -r 09fd7fe --name-only | grep "src/ModelIngestion"

# Extract entire directory
git show 09fd7fe:src/ModelIngestion > restored_model_ingestion.txt

# Or use git to restore the entire directory
git checkout 09fd7fe -- src/ModelIngestion
```

### Task 3.2: Re-add to Solution

```xml
<!-- Hartonomous.sln -->
<Project>
  <ItemGroup>
    <ProjectReference Include="src\ModelIngestion\ModelIngestion.csproj" />
  </ItemGroup>
</Project>
```

### Task 3.3: Fix Dependencies

The ModelIngestion project likely depends on:
- `Hartonomous.Core`
- `Hartonomous.Infrastructure`
- NuGet packages for GGUF/ONNX/PyTorch parsing

Check .csproj and restore missing references.

### Task 3.4: Build and Test

```bash
dotnet build src/ModelIngestion/ModelIngestion.csproj
dotnet test tests/ModelIngestionTests/  # if tests exist
```

---

## PHASE 4: FIX SQL CLR NUGET CONFLICTS (Week 2)

### Task 4.1: Remove System.Text.Json Dependency

**File**: `src/SqlClr/SqlClrFunctions.csproj`

```xml
<!-- REMOVE THIS -->
<PackageReference Include="System.Text.Json" Version="4.7.2" />

<!-- Dependencies become -->
<ItemGroup>
  <PackageReference Include="System.Memory" Version="4.5.5" />
  <PackageReference Include="System.Runtime.CompilerServices.Unsafe" Version="4.5.3" />
  <PackageReference Include="Microsoft.SqlServer.Types" Version="160.1000.6" />
</ItemGroup>
```

### Task 4.2: Implement Manual JSON Serialization

Create helper class for JSON without System.Text.Json:

**File**: `src/SqlClr/Utilities/SimpleJson.cs`

```csharp
public static class SimpleJson
{
    public static string Serialize(object obj)
    {
        var sb = new StringBuilder();
        sb.Append("{");

        var properties = obj.GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            if (i > 0) sb.Append(",");

            var prop = properties[i];
            var value = prop.GetValue(obj);

            sb.Append($"\"{prop.Name}\":");

            if (value == null)
                sb.Append("null");
            else if (value is string)
                sb.Append($"\"{Escape(value.ToString())}\"");
            else if (value is int || value is long || value is float || value is double)
                sb.Append(value.ToString());
            else if (value is bool)
                sb.Append(value.ToString().ToLower());
            else
                sb.Append($"\"{value}\"");
        }

        sb.Append("}");
        return sb.ToString();
    }

    private static string Escape(string s)
    {
        return s.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
```

### Task 4.3: Replace All JsonSerializer Calls

Search and replace in SqlClr:
```bash
# Find all usages
grep -r "JsonSerializer" src/SqlClr/

# Replace with SimpleJson.Serialize
```

### Task 4.4: Build and Verify

```bash
dotnet build src/SqlClr/SqlClrFunctions.csproj

# Should now build with 0 warnings about version conflicts
# Only SIMD-related warnings should remain
```

### Task 4.5: Deploy to SQL Server

```powershell
# Use existing deployment script
.\scripts\deploy-clr-secure.ps1
```

---

## PHASE 5: VERIFY END-TO-END INTEGRATION (Week 3)

### Test 1: Embedding Generation Flow

```csharp
[Test]
public async Task Test_Embedding_Generation_EndToEnd()
{
    // C# Service
    var embedding = await _embeddingService.GenerateEmbeddingAsync("hello world");
    Assert.NotNull(embedding);
    Assert.Equal(1998, embedding.Length);

    // Verify stored in SQL
    var stored = await _dbContext.AtomEmbeddings
        .Where(e => e.AtomId == createdAtomId)
        .FirstOrDefaultAsync();
    Assert.NotNull(stored);
    Assert.NotNull(stored.SpatialGeometry);
}
```

### Test 2: Semantic Search Flow

```csharp
[Test]
public async Task Test_Semantic_Search_EndToEnd()
{
    // Search via C# service
    var results = await _searchService.SearchAsync("test query", topK: 10);

    Assert.NotEmpty(results);
    Assert.True(results.Count() <= 10);
    Assert.All(results, r => Assert.InRange(r.Distance, 0, 2));
}
```

### Test 3: Text Generation Flow

```csharp
[Test]
public async Task Test_Text_Generation_EndToEnd()
{
    var result = await _generationService.GenerateAsync(
        prompt: "Once upon a time",
        maxTokens: 50
    );

    Assert.NotNull(result.GeneratedText);
    Assert.True(result.GeneratedText.Length > 0);
    Assert.NotNull(result.ProvenanceStreamId);
}
```

### Test 4: Feedback Loop

```csharp
[Test]
public async Task Test_Feedback_Loop_EndToEnd()
{
    // Generate with model
    var inference = await _generationService.GenerateAsync("test", 10);

    // Submit feedback
    await _feedbackService.SubmitFeedbackAsync(inference.InferenceId, rating: 5);

    // Trigger weight update
    await _sqlCommandExecutor.ExecuteNonQueryAsync(
        "dbo.sp_UpdateModelWeightsFromFeedback",
        new { learningRate = 0.001, minRatings = 1 }
    );

    // Verify weights updated
    var weightsUpdated = await _dbContext.Weights
        .Where(w => w.LastUpdated > beforeTime)
        .CountAsync();
    Assert.True(weightsUpdated > 0);
}
```

### Test 5: Autonomous Loop (Manual Trigger)

```sql
-- Manually trigger OODA cycle
DECLARE @conversationHandle UNIQUEIDENTIFIER;

BEGIN DIALOG @conversationHandle
    FROM SERVICE AnalyzeService
    TO SERVICE 'HypothesizeService'
    ON CONTRACT OODAContract;

SEND ON CONVERSATION @conversationHandle
    MESSAGE TYPE AnalyzeMessage ('<trigger/>');

-- Wait 30 seconds for cycle to complete
WAITFOR DELAY '00:00:30';

-- Check that hypotheses were generated
SELECT * FROM dbo.PendingActions WHERE CreatedUtc > DATEADD(MINUTE, -1, GETUTCDATE());
```

---

## PHASE 6: PERFORMANCE VALIDATION (Week 4)

### Benchmark 1: Vector Search Performance

```sql
-- Create test dataset
INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingVector, SpatialGeometry, SpatialCoarse, SpatialBucket)
SELECT TOP 1000000
    AtomId,
    dbo.clr_GenerateRandomVector(1998),  -- Random 1998D vector
    dbo.clr_ProjectToSpatial(RandomVector),  -- 3D projection
    dbo.clr_ProjectToCoarse(RandomVector),
    SpatialBucket
FROM dbo.Atoms
CROSS APPLY (SELECT NEWID() AS RandomSeed) AS R;

-- Benchmark brute-force search
SET STATISTICS TIME ON;
SELECT TOP 10 *
FROM dbo.AtomEmbeddings
ORDER BY VECTOR_DISTANCE('cosine', EmbeddingVector, @queryVector);
-- Note duration (expect ~500ms for 1M vectors)

-- Benchmark hybrid search
SET STATISTICS TIME ON;
EXEC dbo.sp_HybridSearch @queryEmbedding = @queryVector, @topK = 10;
-- Note duration (expect ~100ms = 5x faster)

SET STATISTICS TIME OFF;
```

### Benchmark 2: SIMD Acceleration

```sql
-- Test SIMD vs scalar
DECLARE @vector1 VECTOR(1998) = dbo.clr_GenerateRandomVector(1998);
DECLARE @vector2 VECTOR(1998) = dbo.clr_GenerateRandomVector(1998);

-- Scalar version (baseline)
SET STATISTICS TIME ON;
DECLARE @result1 FLOAT;
SELECT @result1 = dbo.clr_VectorDotProduct(@vector1, @vector2)  -- Scalar implementation
FROM sys.objects WHERE object_id = 0;  -- Force 10,000 iterations
-- Note duration

-- SIMD version
DECLARE @result2 FLOAT;
SELECT @result2 = dbo.clr_VectorDotProductSIMD(@vector1, @vector2)  -- SIMD implementation
FROM sys.objects WHERE object_id = 0;  -- Force 10,000 iterations
SET STATISTICS TIME OFF;
-- Note duration (expect ~100x faster if SIMD works)
```

### Benchmark 3: Billing Performance

```sql
-- Test In-Memory OLTP billing
SET STATISTICS TIME ON;

DECLARE @i INT = 0;
WHILE @i < 100000
BEGIN
    EXEC dbo.sp_InsertBillingUsageRecord_Native
        @UserId = 1,
        @OperationType = 'Inference',
        @Tokens = 100,
        @Cost = 0.002;
    SET @i = @i + 1;
END;

SET STATISTICS TIME OFF;
-- Note duration (expect ~1-2 seconds = 100K+ inserts/sec)
```

---

## PHASE 7: PRODUCTION DEPLOYMENT (Week 5+)

### Task 7.1: Azure SQL Database Setup

```powershell
# Create Azure SQL Database
az sql server create `
    --name hartonomous-sql `
    --resource-group hartonomous-rg `
    --location eastus `
    --admin-user sqladmin `
    --admin-password <secure-password>

az sql db create `
    --server hartonomous-sql `
    --name Hartonomous `
    --resource-group hartonomous-rg `
    --service-objective S3  # Standard tier for development

# Enable CLR integration
# Note: Azure SQL may have restrictions on CLR - verify compatibility
```

### Task 7.2: Deploy Schema

```bash
# Run all table creation scripts
sqlcmd -S hartonomous-sql.database.windows.net -d Hartonomous -U sqladmin -i sql/tables/*.sql

# Run all procedure scripts
sqlcmd -S hartonomous-sql.database.windows.net -d Hartonomous -U sqladmin -i sql/procedures/*.sql
```

### Task 7.3: Deploy CLR Assemblies

```powershell
# May need to use EXTERNAL_ACCESS or UNSAFE permission set
# Check Azure SQL CLR compatibility first

.\scripts\deploy-clr-secure.ps1 -ServerName "hartonomous-sql.database.windows.net" -DatabaseName "Hartonomous"
```

### Task 7.4: Deploy Applications

```bash
# Build and publish API
dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj -c Release -o publish/api

# Deploy to Azure App Service
az webapp deployment source config-zip `
    --resource-group hartonomous-rg `
    --name hartonomous-api `
    --src publish/api.zip
```

### Task 7.5: Configure Monitoring

1. Application Insights for API
2. SQL Server DMVs for database performance
3. Custom metrics for OODA loop activity
4. Alerts for critical failures

---

## SUCCESS CRITERIA

### Phase 1 Complete When:
- ✅ All 8 critical tables created
- ✅ sp_SemanticSearch executes without errors
- ✅ sp_TextToEmbedding executes without errors
- ✅ sp_UpdateModelWeightsFromFeedback executes without errors
- ✅ sp_GenerateText executes without errors

### Phase 2 Complete When:
- ✅ All EF Core configurations restored
- ✅ C# services can query AtomEmbeddings via LINQ
- ✅ Migrations can be generated successfully

### Phase 3 Complete When:
- ✅ ModelIngestion project builds
- ✅ Can import a test GGUF model
- ✅ Imported model appears in dbo.Models table

### Phase 4 Complete When:
- ✅ SqlClr builds with 0 errors, 0 version warnings
- ✅ Deploys to SQL Server successfully
- ✅ All 75+ aggregates are registered and callable

### Phase 5 Complete When:
- ✅ All 5 integration tests pass
- ✅ End-to-end flow verified: C# → SQL → CLR → SQL → C#
- ✅ No critical exceptions in logs

### Phase 6 Complete When:
- ✅ Hybrid search is 3-5x faster than brute-force
- ✅ SIMD acceleration verified (if possible in SQL CLR)
- ✅ Billing handles 100K+ inserts/sec

### Phase 7 Complete When:
- ✅ Deployed to Azure
- ✅ Monitoring configured
- ✅ Health checks passing
- ✅ Users can access API endpoints

---

## TIMELINE ESTIMATE

| Phase | Tasks | Estimated Time | Dependencies |
|-------|-------|---------------|--------------|
| Phase 1 | Create 8 tables | 2-3 days | None |
| Phase 2 | Restore EF Core configs | 2-3 days | Phase 1 |
| Phase 3 | Restore ModelIngestion | 2-3 days | Phase 1, Phase 2 |
| Phase 4 | Fix SqlClr NuGet | 1-2 days | None (parallel) |
| Phase 5 | Integration tests | 3-5 days | Phase 1, 2, 3, 4 |
| Phase 6 | Performance validation | 2-3 days | Phase 5 |
| Phase 7 | Production deployment | 5+ days | Phase 6 |
| **TOTAL** | | **3-4 weeks** | Sequential + parallel work |

---

## RISK MITIGATION

### Risk 1: Azure SQL CLR Limitations
**Mitigation**: Test CLR deployment on Azure SQL Database early. If blocked, consider Azure SQL Managed Instance or SQL Server on VM.

### Risk 2: Missing Dependencies in Restored Code
**Mitigation**: Extract complete directory from git, not individual files. Verify all NuGet packages restore.

### Risk 3: Performance Claims Don't Match Reality
**Mitigation**: Set realistic expectations. Document actual performance. Optimize bottlenecks iteratively.

### Risk 4: SIMD Doesn't Work in SQL CLR
**Mitigation**: Accept scalar fallback. SIMD may require UNSAFE which Azure SQL doesn't support. Document limitation.

### Risk 5: Service Broker Issues in Production
**Mitigation**: Add manual triggers for OODA loop. Don't rely solely on autonomous operation initially.

---

## CONCLUSION

This gameplan provides a concrete, step-by-step path from the current 70% functional state to a fully working, production-deployed system.

**Key Principles**:
1. **Fix foundations first** (tables, EF Core configs)
2. **Restore lost functionality** (ModelIngestion)
3. **Remove blockers** (NuGet conflicts)
4. **Verify integration** (end-to-end tests)
5. **Validate performance** (benchmark claims)
6. **Deploy cautiously** (monitoring, health checks)

**Total Estimated Time**: 3-4 weeks with focused effort.

The system is recoverable. The vision is sound. The code exists. It just needs the missing connective tissue (tables and configurations) to work end-to-end.
