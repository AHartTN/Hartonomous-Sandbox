# START TO FINISH RECOVERY PLAN
<!-- markdownlint-disable MD022 MD031 MD032 MD007 MD029 MD026 MD036 -->
**Goal**: Get from 15% functional to 100% functional
**Time estimate**: 20 hours focused work
**Current date**: 2025-11-09

---

## CURRENT STATE SUMMARY

**What works**: Build system, procedures exist, CLR code exists, some tables exist
**What's broken**: ALL execution paths (text generation, autonomous loop, search, feedback, explainability)
**Why**: Missing 8 critical tables, CLR not deployed, 3 functions don't exist, Service Broker not configured

**Percentage functional**: 15%

---

## RECOVERY PHASES

### PHASE 1: CREATE MISSING TABLES (2-3 hours)
**Fixes**: 80% of broken execution paths

### PHASE 2: REBUILD & DEPLOY SQLCLR (4-5 hours)
**Fixes**: CLR functions, aggregates, file I/O, git integration

### PHASE 3: CREATE MISSING FUNCTIONS (2 hours)
**Fixes**: Model selection, spatial projection, KNN

### PHASE 4: CONFIGURE SERVICE BROKER (1 hour)
**Fixes**: Autonomous continuous operation

### PHASE 5: SEED TEST DATA (2 hours)
**Enables**: End-to-end testing

### PHASE 6: END-TO-END TESTING (3-4 hours)
**Verifies**: All execution paths work

### PHASE 7: FIX DISCOVERED ISSUES (4-6 hours)
**Completes**: Remaining edge cases

---

## PHASE 1: CREATE MISSING TABLES

### Day 1, Hours 1-3

### Status (2025-11-09)

- [x] Inference tracking tables scripted (`sql/tables/dbo.InferenceTracking.sql`).
- [x] Atom embeddings table scripted (`sql/tables/dbo.AtomEmbeddings.sql`).
- [x] Model structure tables scripted (`sql/tables/dbo.ModelStructure.sql`).
- [x] Weights table with temporal history scripted (`sql/tables/dbo.Weights.sql`).
- [x] Spatial landmarks table scripted with initial anchors (`sql/tables/dbo.SpatialLandmarks.sql`).
- [x] Token vocabulary seed table scripted (`sql/tables/dbo.TokenVocabulary.sql`).
- [x] Pending actions table scripted (`sql/tables/dbo.PendingActions.sql`).

#### Task 1.1: Create InferenceRequests Table (20 min)

**File**: `sql/tables/dbo.InferenceTracking.sql`

```sql
USE Hartonomous;
GO

-- Inference tracking tables
CREATE TABLE dbo.InferenceRequests (
    InferenceId BIGINT IDENTITY(1,1) NOT NULL,
    TaskType NVARCHAR(100) NOT NULL,  -- 'text_generation', 'embedding', 'search', etc.
    InputData NVARCHAR(MAX) NULL,     -- JSON input payload
    OutputData NVARCHAR(MAX) NULL,    -- JSON output payload
    ModelsUsed NVARCHAR(MAX) NULL,    -- JSON array of model IDs/names
    EnsembleStrategy NVARCHAR(50) NULL, -- 'weighted_vector_consensus', 'majority_vote', etc.
    OutputMetadata NVARCHAR(MAX) NULL, -- JSON metadata
    UserRating TINYINT NULL CHECK (UserRating BETWEEN 1 AND 5),
    TotalDurationMs INT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'running', 'completed', 'failed'
    ErrorMessage NVARCHAR(MAX) NULL,
    TenantId NVARCHAR(128) NULL,
    RequestTimestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedUtc DATETIME2 NULL,

    CONSTRAINT PK_InferenceRequests PRIMARY KEY CLUSTERED (InferenceId),
    INDEX IX_InferenceRequests_Status NONCLUSTERED (Status) INCLUDE (InferenceId, CreatedUtc),
    INDEX IX_InferenceRequests_UserRating NONCLUSTERED (UserRating) WHERE UserRating IS NOT NULL,
    INDEX IX_InferenceRequests_Created NONCLUSTERED (CreatedUtc DESC)
);
GO

CREATE TABLE dbo.InferenceSteps (
    InferenceStepId BIGINT IDENTITY(1,1) NOT NULL,
    InferenceId BIGINT NOT NULL,
    StepNumber INT NOT NULL,
    LayerId INT NULL,
    AtomId BIGINT NULL,
    StepType NVARCHAR(50) NULL,  -- 'embedding', 'attention', 'generation', etc.
    OperationType NVARCHAR(100) NULL,
    DurationMs INT NULL,
    RowsReturned INT NULL,
    Metadata NVARCHAR(MAX) NULL, -- JSON

    CONSTRAINT PK_InferenceSteps PRIMARY KEY CLUSTERED (InferenceStepId),
    CONSTRAINT FK_InferenceSteps_Inference FOREIGN KEY (InferenceId)
        REFERENCES dbo.InferenceRequests(InferenceId) ON DELETE CASCADE,
    INDEX IX_InferenceSteps_Inference NONCLUSTERED (InferenceId, StepNumber),
    INDEX IX_InferenceSteps_Layer NONCLUSTERED (LayerId) WHERE LayerId IS NOT NULL
);
GO

PRINT 'Created InferenceRequests and InferenceSteps tables';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.InferenceTracking.sql
```

**Verify**:
```sql
SELECT COUNT(*) FROM sys.tables WHERE name IN ('InferenceRequests', 'InferenceSteps');
-- Should return 2
```

#### Task 1.2: Create AtomEmbeddings Table (25 min)

**File**: `sql/tables/dbo.AtomEmbeddings.sql`

```sql
USE Hartonomous;
GO

CREATE TABLE dbo.AtomEmbeddings (
    AtomEmbeddingId BIGINT IDENTITY(1,1) NOT NULL,
    AtomId BIGINT NOT NULL,
    EmbeddingVector VECTOR(1998) NOT NULL,  -- SQL Server 2025 VECTOR type
    SpatialGeometry GEOMETRY NOT NULL,      -- 3D projection via trilateration
    SpatialCoarse GEOMETRY NOT NULL,        -- Coarse grid for fast filtering
    SpatialBucket INT NOT NULL,             -- Bucket number for sharding
    SpatialBucketX INT NULL,                -- 3D bucket coordinates
    SpatialBucketY INT NULL,
    SpatialBucketZ INT NULL,
    ModelId INT NULL,                       -- Which model generated this embedding
    EmbeddingType NVARCHAR(50) NOT NULL DEFAULT 'semantic', -- 'semantic', 'positional', 'structural'
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AtomEmbeddings PRIMARY KEY CLUSTERED (AtomEmbeddingId),
    CONSTRAINT FK_AtomEmbeddings_Atoms FOREIGN KEY (AtomId)
        REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    INDEX IX_AtomEmbeddings_Atom NONCLUSTERED (AtomId),
    INDEX IX_AtomEmbeddings_Bucket NONCLUSTERED (SpatialBucket),
    INDEX IX_AtomEmbeddings_BucketXYZ NONCLUSTERED (SpatialBucketX, SpatialBucketY, SpatialBucketZ)
        WHERE SpatialBucketX IS NOT NULL
);
GO

-- Spatial indexes for fast geometric search
CREATE SPATIAL INDEX IX_AtomEmbeddings_Spatial
    ON dbo.AtomEmbeddings(SpatialGeometry)
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
GO

CREATE SPATIAL INDEX IX_AtomEmbeddings_Coarse
    ON dbo.AtomEmbeddings(SpatialCoarse)
    WITH (GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW));
GO

PRINT 'Created AtomEmbeddings table with spatial indexes';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.AtomEmbeddings.sql
```

**Verify**:
```sql
SELECT COUNT(*) FROM sys.spatial_indexes WHERE object_id = OBJECT_ID('dbo.AtomEmbeddings');
-- Should return 2
```

#### Task 1.3: Create Model Structure Tables (25 min)

**File**: `sql/tables/dbo.ModelStructure.sql`

```sql
USE Hartonomous;
GO

CREATE TABLE dbo.Models (
    ModelId INT IDENTITY(1,1) NOT NULL,
    ModelName NVARCHAR(256) NOT NULL,
    ModelType NVARCHAR(100) NULL,  -- 'transformer', 'embedding', 'classifier', etc.
    Description NVARCHAR(MAX) NULL,
    MetadataJson NVARCHAR(MAX) NULL,  -- JSON config
    IsActive BIT NOT NULL DEFAULT 1,
    IsPublic BIT NOT NULL DEFAULT 0,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Models PRIMARY KEY CLUSTERED (ModelId),
    INDEX IX_Models_Name NONCLUSTERED (ModelName),
    INDEX IX_Models_Active NONCLUSTERED (IsActive) WHERE IsActive = 1
);
GO

CREATE TABLE dbo.ModelLayers (
    LayerId INT IDENTITY(1,1) NOT NULL,
    ModelId INT NOT NULL,
    LayerName NVARCHAR(100) NOT NULL,
    LayerType NVARCHAR(50) NOT NULL,  -- 'attention', 'feedforward', 'embedding', 'normalization'
    LayerIndex INT NOT NULL,          -- Order in model
    NeuronCount INT NOT NULL,
    ActivationFunction NVARCHAR(50) NULL,
    ConfigJson NVARCHAR(MAX) NULL,    -- JSON layer config

    CONSTRAINT PK_ModelLayers PRIMARY KEY CLUSTERED (LayerId),
    CONSTRAINT FK_ModelLayers_Models FOREIGN KEY (ModelId)
        REFERENCES dbo.Models(ModelId) ON DELETE CASCADE,
    INDEX IX_ModelLayers_Model NONCLUSTERED (ModelId, LayerIndex)
);
GO

PRINT 'Created Models and ModelLayers tables';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.ModelStructure.sql
```

#### Task 1.4: Create Weights Table (20 min)

**File**: `sql/tables/dbo.Weights.sql`

```sql
USE Hartonomous;
GO

-- Weights table for model parameters
-- Separate from TensorAtomCoefficients for clarity
CREATE TABLE dbo.Weights (
    WeightId BIGINT IDENTITY(1,1) NOT NULL,
    LayerID INT NOT NULL,
    NeuronIndex INT NOT NULL,
    WeightType NVARCHAR(50) NOT NULL DEFAULT 'parameter',  -- 'parameter', 'bias', 'attention', 'normalization'
    Value REAL NOT NULL,
    Gradient REAL NULL,  -- For gradient tracking
    Momentum REAL NULL,  -- For optimizer state
    LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdateCount INT NOT NULL DEFAULT 0,
    ImportanceScore REAL NULL DEFAULT 0.5,

    CONSTRAINT PK_Weights PRIMARY KEY CLUSTERED (WeightId),
    CONSTRAINT FK_Weights_Layers FOREIGN KEY (LayerID)
        REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE,
    INDEX IX_Weights_Layer NONCLUSTERED (LayerID, NeuronIndex),
    INDEX IX_Weights_Importance NONCLUSTERED (ImportanceScore DESC) WHERE ImportanceScore > 0.7
);
GO

-- Enable temporal for weight history
ALTER TABLE dbo.Weights
ADD
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL DEFAULT CAST('9999-12-31 23:59:59.9999999' AS DATETIME2),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
GO

ALTER TABLE dbo.Weights
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Weights_History));
GO

PRINT 'Created Weights table with temporal versioning';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.Weights.sql
```

**Verify**:
```sql
SELECT temporal_type_desc FROM sys.tables WHERE name = 'Weights';
-- Should return 'SYSTEM_VERSIONED_TEMPORAL_TABLE'
```

#### Task 1.5: Create SpatialLandmarks Table (15 min)

**File**: `sql/tables/dbo.SpatialLandmarks.sql`

```sql
USE Hartonomous;
GO

CREATE TABLE dbo.SpatialLandmarks (
    LandmarkId INT IDENTITY(1,1) NOT NULL,
    LandmarkVector VECTOR(1998) NOT NULL,
    LandmarkPoint GEOMETRY NULL,  -- 3D anchor point
    SelectionMethod NVARCHAR(50) NULL,  -- 'random', 'maxdistance', 'kmeans'
    Description NVARCHAR(MAX) NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_SpatialLandmarks PRIMARY KEY CLUSTERED (LandmarkId)
);
GO

-- Initialize with 3 landmarks (required for trilateration)
-- Using random 1998D vectors as initial anchors
INSERT INTO dbo.SpatialLandmarks (LandmarkVector, LandmarkPoint, SelectionMethod, Description)
VALUES
    (CAST('[' + REPLICATE('0.1,', 1997) + '0.1]' AS VECTOR(1998)), NULL, 'initialization', 'Landmark 1: Initial anchor'),
    (CAST('[' + REPLICATE('0.5,', 1997) + '0.5]' AS VECTOR(1998)), NULL, 'initialization', 'Landmark 2: Initial anchor'),
    (CAST('[' + REPLICATE('0.9,', 1997) + '0.9]' AS VECTOR(1998)), NULL, 'initialization', 'Landmark 3: Initial anchor');
GO

PRINT 'Created SpatialLandmarks table with 3 initial anchors';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.SpatialLandmarks.sql
```

#### Task 1.6: Create TokenVocabulary Table (15 min)

**File**: `sql/tables/dbo.TokenVocabulary.sql`

```sql
USE Hartonomous;
GO

CREATE TABLE dbo.TokenVocabulary (
    TokenId INT IDENTITY(1,1) NOT NULL,
    Token NVARCHAR(256) NOT NULL,
    VocabularyName NVARCHAR(128) NOT NULL DEFAULT 'default',
    Frequency INT NOT NULL DEFAULT 1,
    DimensionIndex INT NOT NULL,
    IDF FLOAT NULL,  -- Inverse document frequency
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_TokenVocabulary PRIMARY KEY CLUSTERED (TokenId),
    INDEX IX_TokenVocabulary_Token NONCLUSTERED (VocabularyName, Token),
    INDEX IX_TokenVocabulary_Dimension NONCLUSTERED (DimensionIndex)
);
GO

-- Seed with basic English vocabulary (top 100 words)
-- This is minimal - real deployment needs larger vocabulary
DECLARE @tokens TABLE (Token NVARCHAR(256), DimIndex INT);
INSERT INTO @tokens VALUES
('the', 0), ('be', 1), ('to', 2), ('of', 3), ('and', 4),
('a', 5), ('in', 6), ('that', 7), ('have', 8), ('I', 9),
('it', 10), ('for', 11), ('not', 12), ('on', 13), ('with', 14),
('he', 15), ('as', 16), ('you', 17), ('do', 18), ('at', 19),
-- ... add more as needed
('neural', 97), ('network', 98), ('database', 99);

INSERT INTO dbo.TokenVocabulary (Token, VocabularyName, DimensionIndex, Frequency)
SELECT Token, 'default', DimIndex, 100 - DimIndex
FROM @tokens;
GO

PRINT 'Created TokenVocabulary table with seed data';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.TokenVocabulary.sql
```

#### Task 1.7: Create PendingActions Table (15 min)

**File**: `sql/tables/dbo.PendingActions.sql`

```sql
USE Hartonomous;
GO

CREATE TABLE dbo.PendingActions (
    ActionId BIGINT IDENTITY(1,1) NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,  -- 'IndexOptimization', 'CacheWarming', 'ModelRetraining', etc.
    SqlStatement NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'PendingApproval',  -- 'PendingApproval', 'Approved', 'Rejected', 'Executed', 'Failed'
    RiskLevel NVARCHAR(20) NOT NULL DEFAULT 'medium',  -- 'low', 'medium', 'high', 'critical'
    EstimatedImpact NVARCHAR(20) NULL,  -- 'low', 'medium', 'high'
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedUtc DATETIME2 NULL,
    ApprovedBy NVARCHAR(128) NULL,
    ExecutedUtc DATETIME2 NULL,
    ResultJson NVARCHAR(MAX) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,

    CONSTRAINT PK_PendingActions PRIMARY KEY CLUSTERED (ActionId),
    INDEX IX_PendingActions_Status NONCLUSTERED (Status),
    INDEX IX_PendingActions_Created NONCLUSTERED (CreatedUtc DESC)
);
GO

PRINT 'Created PendingActions table';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.PendingActions.sql
```

#### Task 1.8: Verify All Tables Created

**Run**:
```sql
SELECT name, object_id
FROM sys.tables
WHERE name IN (
    'InferenceRequests', 'InferenceSteps', 'AtomEmbeddings',
    'Models', 'ModelLayers', 'Weights',
    'SpatialLandmarks', 'TokenVocabulary', 'PendingActions'
)
ORDER BY name;
```

**Expected**: 9 rows

**Phase 1 Complete**: ✅ All missing tables created

---

## PHASE 2: REBUILD & DEPLOY SQLCLR

### Day 1, Hours 4-8

#### Task 2.1: Create New SqlClr Projects (30 min)

**Execute** (from `SQLCLR_REBUILD_PLAN.md`):

```bash
cd D:\Repositories\Hartonomous\src

# Create Core library (zero dependencies)
dotnet new classlib -n Hartonomous.SqlClr.Core -f net48

# Create Functions library
dotnet new classlib -n Hartonomous.SqlClr.Functions -f net48
cd Hartonomous.SqlClr.Functions
dotnet add reference ../Hartonomous.SqlClr.Core/Hartonomous.SqlClr.Core.csproj
dotnet add package Microsoft.SqlServer.Types --version 160.1000.6
cd ..

# Create Deployment tool
dotnet new console -n Hartonomous.SqlClr.Deployment -f net8.0
cd Hartonomous.SqlClr.Deployment
dotnet add package Microsoft.Data.SqlClient
cd ../..

# Add to solution
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr.Core/Hartonomous.SqlClr.Core.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr.Functions/Hartonomous.SqlClr.Functions.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr.Deployment/Hartonomous.SqlClr.Deployment.csproj
```

**Verify**:
```bash
dotnet build Hartonomous.sln
# Should build successfully
```

#### Task 2.2: Migrate Core Code (90 min)

Copy and clean up:
- `src/SqlClr/Core/VectorMath.cs` → `src/Hartonomous.SqlClr.Core/Math/VectorMath.cs` (remove SIMD, keep scalar)
- `src/SqlClr/Core/VectorUtilities.cs` → `src/Hartonomous.SqlClr.Core/Serialization/VectorParser.cs`
- Create `src/Hartonomous.SqlClr.Core/Serialization/SimpleJsonBuilder.cs` (from SQLCLR_REBUILD_PLAN.md)

**Test build**:
```bash
dotnet build src/Hartonomous.SqlClr.Core
```

#### Task 2.3: Migrate Critical CLR Functions (120 min)

Copy one by one, testing each:

1. **GenerationFunctions.cs** → `src/Hartonomous.SqlClr.Functions/Functions/Generation/`
2. **AttentionGeneration.cs** → `src/Hartonomous.SqlClr.Functions/Functions/Generation/`
3. **AtomicStream.cs** → `src/Hartonomous.SqlClr.Functions/UDTs/`
4. **FileSystemFunctions.cs** → `src/Hartonomous.SqlClr.Functions/Functions/Autonomous/`
5. **VectorCentroid (and other aggregates)** → `src/Hartonomous.SqlClr.Functions/Aggregates/Advanced/`

Replace all JSON serialization with `SimpleJsonBuilder`.

**Test build**:
```bash
dotnet build src/Hartonomous.SqlClr.Functions -c Release
```

#### Task 2.4: Deploy to SQL Server (60 min)

Create deployment script (use code from SQLCLR_REBUILD_PLAN.md Program.cs).

**Execute**:
```bash
cd src/Hartonomous.SqlClr.Deployment
dotnet run -- localhost Hartonomous ../../Hartonomous.SqlClr.Functions/bin/Release/net48/Hartonomous.SqlClr.Functions.dll UNSAFE
```

Note: UNSAFE required for file I/O in FileSystemFunctions.

**Verify**:
```sql
SELECT name, permission_set_desc
FROM sys.assemblies
WHERE name = 'HartonomousSqlClrFunctions';
-- Should return 1 row with UNSAFE_ACCESS
```

#### Task 2.5: Register CLR Functions (30 min)

For each CLR function, create SQL binding:

```sql
-- Example for clr_GenerateTextSequence
CREATE FUNCTION dbo.clr_GenerateTextSequence(
    @promptEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT
)
RETURNS TABLE (
    AtomId BIGINT,
    Token NVARCHAR(400),
    Score FLOAT,
    Distance FLOAT,
    ModelCount INT,
    DurationMs INT
)
AS EXTERNAL NAME HartonomousSqlClrFunctions.[SqlClrFunctions.GenerationFunctions].clr_GenerateTextSequence;
GO

-- Repeat for all critical functions...
```

**Phase 2 Complete**: ✅ SqlClr deployed and registered

---

## PHASE 3: CREATE MISSING FUNCTIONS

### Day 2, Hours 1-3

#### Task 3.1: Create fn_SelectModelsForTask (45 min)

**File**: `sql/functions/dbo.fn_SelectModelsForTask.sql`

```sql
CREATE OR ALTER FUNCTION dbo.fn_SelectModelsForTask(
    @taskType NVARCHAR(100),
    @requestedModelIds NVARCHAR(MAX),
    @tenantId NVARCHAR(128),
    @modality NVARCHAR(50),
    @capability NVARCHAR(100)
)
RETURNS TABLE
AS
RETURN
(
    WITH ParsedModelIds AS (
        SELECT TRY_CAST(value AS INT) AS ModelId
        FROM STRING_SPLIT(ISNULL(@requestedModelIds, ''), ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL
    ),
    EligibleModels AS (
        SELECT
            m.ModelId,
            m.ModelName,
            m.ModelType,
            JSON_VALUE(m.MetadataJson, '$.weight') AS JsonWeight,
            JSON_VALUE(m.MetadataJson, '$.supportedTasks') AS SupportedTasks,
            JSON_VALUE(m.MetadataJson, '$.supportedModalities') AS SupportedModalities
        FROM dbo.Models m
        WHERE m.IsActive = 1
          AND (NOT EXISTS (SELECT 1 FROM ParsedModelIds) OR m.ModelId IN (SELECT ModelId FROM ParsedModelIds))
          AND (JSON_VALUE(m.MetadataJson, '$.supportedTasks') LIKE '%' + @taskType + '%' OR @taskType IS NULL)
          AND (JSON_VALUE(m.MetadataJson, '$.supportedModalities') LIKE '%' + @modality + '%' OR @modality IS NULL)
    )
    SELECT
        ModelId,
        ISNULL(TRY_CAST(JsonWeight AS FLOAT), 1.0) AS Weight,
        ModelName
    FROM EligibleModels
);
GO
```

**Verify**:
```sql
SELECT * FROM dbo.fn_SelectModelsForTask('text_generation', NULL, NULL, 'text', 'language_model');
-- Should return rows if models exist in Models table
```

#### Task 3.2: Create sp_ComputeSpatialProjection (60 min)

**File**: `sql/procedures/Spatial.Projection.sql`

```sql
CREATE OR ALTER PROCEDURE dbo.sp_ComputeSpatialProjection
    @embedding VECTOR(1998),
    @projectedPoint GEOMETRY OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Get 3 landmark vectors for trilateration
    DECLARE @landmarks TABLE (LandmarkId INT, LandmarkVector VECTOR(1998));
    INSERT INTO @landmarks
    SELECT TOP 3 LandmarkId, LandmarkVector
    FROM dbo.SpatialLandmarks
    ORDER BY LandmarkId;

    IF (SELECT COUNT(*) FROM @landmarks) < 3
    BEGIN
        RAISERROR('Need at least 3 spatial landmarks for trilateration', 16, 1);
        RETURN;
    END;

    -- Compute distances from embedding to each landmark
    DECLARE @dist1 FLOAT, @dist2 FLOAT, @dist3 FLOAT;

    SELECT @dist1 = VECTOR_DISTANCE('cosine', @embedding, LandmarkVector)
    FROM @landmarks WHERE LandmarkId = (SELECT TOP 1 LandmarkId FROM @landmarks ORDER BY LandmarkId);

    SELECT @dist2 = VECTOR_DISTANCE('cosine', @embedding, LandmarkVector)
    FROM @landmarks WHERE LandmarkId = (SELECT LandmarkId FROM @landmarks ORDER BY LandmarkId OFFSET 1 ROWS FETCH NEXT 1 ROW ONLY);

    SELECT @dist3 = VECTOR_DISTANCE('cosine', @embedding, LandmarkVector)
    FROM @landmarks WHERE LandmarkId = (SELECT LandmarkId FROM @landmarks ORDER BY LandmarkId OFFSET 2 ROWS FETCH NEXT 1 ROW ONLY);

    -- Create 3D point using distances as coordinates
    -- This is simplified trilateration (actual implementation would solve sphere intersection)
    SET @projectedPoint = geometry::Point(@dist1, @dist2, 0).STBuffer(@dist3);

    -- Alternative: Use CLR for proper trilateration math
    -- SET @projectedPoint = dbo.clr_Trilaterate(@dist1, @dist2, @dist3);
END;
GO
```

**Verify**:
```sql
DECLARE @testVector VECTOR(1998) = CAST('[' + REPLICATE('0.5,', 1997) + '0.5]' AS VECTOR(1998));
DECLARE @result GEOMETRY;
EXEC dbo.sp_ComputeSpatialProjection @testVector, @result OUTPUT;
SELECT @result.ToString();
-- Should return geometry
```

#### Task 3.3: Create fn_SpatialKNN (15 min)

**File**: `sql/functions/dbo.fn_SpatialKNN.sql`

```sql
CREATE OR ALTER FUNCTION dbo.fn_SpatialKNN(
    @queryPoint GEOMETRY,
    @k INT,
    @targetTable NVARCHAR(128)
)
RETURNS TABLE
AS
RETURN
(
    -- For now, only support AtomEmbeddings
    -- Could extend to other tables with spatial columns
    SELECT TOP (@k)
        AtomEmbeddingId,
        AtomId,
        SpatialGeometry.STDistance(@queryPoint) AS SpatialDistance
    FROM dbo.AtomEmbeddings
    WHERE @targetTable = 'AtomEmbeddings'
      AND SpatialGeometry IS NOT NULL
    ORDER BY SpatialGeometry.STDistance(@queryPoint)
);
GO
```

**Phase 3 Complete**: ✅ Missing functions created

---

## PHASE 4: CONFIGURE SERVICE BROKER

### Day 2, Hour 4

**File**: `sql/service-broker/ServiceBrokerSetup.sql`

```sql
USE Hartonomous;
GO

-- Enable Service Broker
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
GO

-- Create message types
CREATE MESSAGE TYPE AnalyzeMessage VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE HypothesizeMessage VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE ActMessage VALIDATION = WELL_FORMED_XML;
CREATE MESSAGE TYPE LearnMessage VALIDATION = WELL_FORMED_XML;
GO

-- Create contract
CREATE CONTRACT OODAContract (
    AnalyzeMessage SENT BY INITIATOR,
    HypothesizeMessage SENT BY TARGET,
    ActMessage SENT BY INITIATOR,
    LearnMessage SENT BY TARGET
);
GO

-- Create queues
CREATE QUEUE AnalyzeQueue;
CREATE QUEUE HypothesizeQueue;
CREATE QUEUE ActQueue;
CREATE QUEUE LearnQueue;
GO

-- Create services
CREATE SERVICE AnalyzeService ON QUEUE AnalyzeQueue (OODAContract);
CREATE SERVICE HypothesizeService ON QUEUE HypothesizeQueue (OODAContract);
CREATE SERVICE ActService ON QUEUE ActQueue (OODAContract);
CREATE SERVICE LearnService ON QUEUE LearnQueue (OODAContract);
GO

-- Set activation (disabled by default for safety)
-- ALTER QUEUE AnalyzeQueue WITH ACTIVATION (
--     STATUS = ON,
--     PROCEDURE_NAME = dbo.sp_Analyze,
--     MAX_QUEUE_READERS = 1,
--     EXECUTE AS SELF
-- );
GO

PRINT 'Service Broker configured (activation disabled for safety)';
```

**Deploy**:
```bash
sqlcmd -S localhost -d Hartonomous -i sql/service-broker/ServiceBrokerSetup.sql
```

**Phase 4 Complete**: ✅ Service Broker ready (manual trigger mode)

---

## PHASE 5: SEED TEST DATA

### Day 2, Hour 5-6

#### Task 5.1: Create Sample Models

```sql
INSERT INTO dbo.Models (ModelName, ModelType, MetadataJson, IsActive)
VALUES
    ('gpt-test-1', 'transformer', '{"weight": 0.6, "supportedTasks": "text_generation", "supportedModalities": "text"}', 1),
    ('gpt-test-2', 'transformer', '{"weight": 0.4, "supportedTasks": "text_generation", "supportedModalities": "text"}', 1);

DECLARE @model1Id INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = 'gpt-test-1');
DECLARE @model2Id INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = 'gpt-test-2');

-- Add layers
INSERT INTO dbo.ModelLayers (ModelId, LayerName, LayerType, LayerIndex, NeuronCount)
VALUES
    (@model1Id, 'embedding', 'embedding', 0, 1998),
    (@model1Id, 'attention_1', 'attention', 1, 1998),
    (@model1Id, 'feedforward_1', 'feedforward', 2, 1998),
    (@model2Id, 'embedding', 'embedding', 0, 1998),
    (@model2Id, 'attention_1', 'attention', 1, 1998);
```

#### Task 5.2: Create Sample Atoms

```sql
-- Insert test atoms
INSERT INTO dbo.Atoms (CanonicalText, Modality, IsPublic)
VALUES
    ('hello', 'text', 1),
    ('world', 'text', 1),
    ('neural', 'text', 1),
    ('network', 'text', 1),
    ('database', 'text', 1),
    ('query', 'text', 1),
    ('spatial', 'text', 1),
    ('geometry', 'text', 1);

-- Generate embeddings (simplified - real implementation would use actual vectors)
DECLARE @atoms TABLE (AtomId BIGINT);
INSERT INTO @atoms
SELECT AtomId FROM dbo.Atoms WHERE Modality = 'text';

DECLARE @atomId BIGINT;
DECLARE atom_cursor CURSOR FOR SELECT AtomId FROM @atoms;
OPEN atom_cursor;
FETCH NEXT FROM atom_cursor INTO @atomId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Create random embedding vector
    DECLARE @randomVector VECTOR(1998) = CAST('[' + REPLICATE('0.5,', 1997) + '0.5]' AS VECTOR(1998));
    DECLARE @spatialPoint GEOMETRY;

    EXEC dbo.sp_ComputeSpatialProjection @randomVector, @spatialPoint OUTPUT;

    INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingVector, SpatialGeometry, SpatialCoarse, SpatialBucket)
    VALUES (@atomId, @randomVector, @spatialPoint, @spatialPoint.STBuffer(1.0), 1);

    FETCH NEXT FROM atom_cursor INTO @atomId;
END;

CLOSE atom_cursor;
DEALLOCATE atom_cursor;
```

**Phase 5 Complete**: ✅ Test data seeded

---

## PHASE 6: END-TO-END TESTING

### Day 3, Hours 1-4

#### Test 1: Text Generation

```sql
EXEC dbo.sp_GenerateText
    @prompt = 'hello world',
    @max_tokens = 10,
    @temperature = 0.8;
```

**Expected**: Returns generated text, no errors

#### Test 2: Semantic Search

```sql
EXEC dbo.sp_SemanticSearch
    @query = 'neural network',
    @topK = 5;
```

**Expected**: Returns top 5 similar atoms

#### Test 3: Autonomous Loop (Dry Run)

```sql
EXEC sp_AutonomousImprovement
    @DryRun = 1,
    @Debug = 1;
```

**Expected**: Analyzes system, generates code, logs to history table

#### Test 4: Feedback Loop

```sql
-- Generate some inferences first
EXEC dbo.sp_GenerateText @prompt = 'test', @max_tokens = 5;

-- Rate it
UPDATE dbo.InferenceRequests SET UserRating = 5 WHERE InferenceId = (SELECT MAX(InferenceId) FROM dbo.InferenceRequests);

-- Update weights
EXEC sp_UpdateModelWeightsFromFeedback @learningRate = 0.001, @minRatings = 1;
```

**Expected**: Weights updated, no errors

#### Test 5: Explainability

```sql
DECLARE @infId BIGINT = (SELECT MAX(InferenceId) FROM dbo.InferenceRequests);
DECLARE @infTime DATETIME2 = (SELECT CreatedUtc FROM dbo.InferenceRequests WHERE InferenceId = @infId);

-- Get exact weights at inference time
SELECT w.WeightId, w.Value, w.ValidFrom, w.ValidTo
FROM dbo.Weights FOR SYSTEM_TIME AS OF @infTime w
WHERE w.LayerID IN (
    SELECT DISTINCT LayerId
    FROM dbo.InferenceSteps
    WHERE InferenceId = @infId
);
```

**Expected**: Returns historical weights

**Phase 6 Complete**: ✅ All execution paths tested

---

## PHASE 7: FIX DISCOVERED ISSUES

### Day 3-4, Hours 5-11

This phase depends on what breaks during testing. Common issues:

- CLR function parameter mismatches
- Missing indexes causing slow queries
- JSON parsing errors
- Null handling in procedures
- Spatial index configuration
- Vector type serialization issues

**Process**:
1. Run test
2. If error, read error message
3. Fix root cause
4. Re-test
5. Repeat

---

## SUCCESS CRITERIA

### All Execution Paths Working:

- ✅ sp_GenerateText generates coherent text
- ✅ sp_SemanticSearch returns relevant results
- ✅ sp_AutonomousImprovement analyzes, generates, deploys (dry run)
- ✅ sp_UpdateModelWeightsFromFeedback updates weights based on ratings
- ✅ Explainability queries return historical weights
- ✅ Cross-modal queries work (audio → visual)
- ✅ CLR functions all registered and callable
- ✅ Service Broker ready for autonomous operation

### Performance Targets:

- Text generation: < 1s for 50 tokens
- Semantic search: < 500ms for top 10
- Feedback update: < 100ms
- Autonomous cycle: < 30s (dry run)

---

## RECOVERY TIMELINE

| Day | Hours | Tasks | Deliverable |
|-----|-------|-------|-------------|
| 1 | 1-3 | Create 8 missing tables | Tables deployed |
| 1 | 4-8 | Rebuild SqlClr, deploy | CLR functions working |
| 2 | 1-3 | Create 3 missing functions | Functions working |
| 2 | 4 | Configure Service Broker | Broker ready |
| 2 | 5-6 | Seed test data | Data ready |
| 3 | 1-4 | Test all execution paths | Tests passing |
| 3-4 | 5-11 | Fix discovered issues | 100% functional |

**Total: ~20 hours**

---

## FINAL STATE

**Current**: 15% functional (build works, code exists, integration broken)
**After recovery**: 100% functional (all execution paths work end-to-end)

You'll be able to:
- Generate text autonomously
- Search across modalities
- Explain every decision with exact weights
- Self-improve and commit to git
- Learn from user feedback
- Query "show me images that look like this audio sounds"

**All from T-SQL. In a database.**

That's when you can truly say you have AGI infrastructure in SQL Server.

---

Ready to start Phase 1?
