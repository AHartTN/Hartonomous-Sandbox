-- =========================================================================================
-- GENESIS SCRIPT: HARTONOMOUS COGNITIVE KERNEL
-- =========================================================================================
-- This script bootstraps a semantic universe with defined physics, matter, and time.
-- It creates specific geometric arrangements to test Spatial Indexing, A*, and OODA loops.
-- =========================================================================================

SET NOCOUNT ON;
USE [Hartonomous];
GO

PRINT 'Initializing the Cognitive Kernel...';
PRINT '--------------------------------------------------';

-- =========================================================================================
-- EPOCH 1: THE AXIOMS (Tenants, Models, and The Coordinate System)
-- =========================================================================================
PRINT 'EPOCH 1: Defining Physics (Tenants, Models, Landmarks)';

-- 1.1 Tenants (The Observers)
MERGE dbo.TenantGuidMapping AS target
USING (VALUES 
    (0, '00000000-0000-0000-0000-000000000000', 'System Root'),
    (1, '11111111-1111-1111-1111-111111111111', 'Dev Operations'),
    (2, '22222222-2222-2222-2222-222222222222', 'Research Lab')
) AS source (Id, Guid, Name)
ON target.TenantId = source.Id
WHEN MATCHED THEN UPDATE SET TenantGuid = source.Guid
WHEN NOT MATCHED THEN INSERT (TenantId, TenantGuid, CreatedAt) VALUES (source.Id, source.Guid, SYSDATETIME());

-- 1.2 Models (The Encoders)
-- We define models with specific dimensions to test capability parsing
MERGE dbo.Models AS target
USING (VALUES 
    ('godel-v1-reasoning', 'Reasoning', 'Hartonomous', '{"embeddingDimension": 1536, "contextWindow": 128000, "capabilities": ["chain-of-thought", "spatial-reasoning"]}'),
    ('clip-spatial-v2', 'Multimodal', 'OpenAI', '{"embeddingDimension": 1024, "supportedModalities": ["image", "text"], "spatialAwareness": true}'),
    ('codellama-70b', 'LLM', 'Meta', '{"embeddingDimension": 4096, "supportedModalities": ["code"]}')
) AS source (Name, Type, Prov, Meta)
ON target.ModelName = source.Name
WHEN MATCHED THEN UPDATE SET MetadataJson = source.Meta
WHEN NOT MATCHED THEN INSERT (ModelName, ModelType, Provider, IsActive, MetadataJson) VALUES (source.Name, source.Type, source.Prov, 1, source.Meta);

DECLARE @ModelReasoning INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = 'godel-v1-reasoning');

-- 1.3 Spatial Landmarks (The Reference Frame for Trilateration)
-- We seed orthogonal vectors to act as the Basis for X, Y, Z dimensions.
-- Simulating 1536-dim vectors with a simplified binary pattern for the seed.
PRINT '   -> Seeding Orthogonal Basis Vectors...';

DELETE FROM dbo.SpatialLandmarks; -- Reset physics

INSERT INTO dbo.SpatialLandmarks (ModelId, LandmarkType, Vector, AxisAssignment, CreatedAt)
VALUES 
    -- X-Axis: Represents "Abstract <-> Concrete"
    (@ModelReasoning, 'Basis', CAST(REPLICATE(CAST(0x3F800000 AS VARBINARY(MAX)), 100) AS VARBINARY(MAX)), 'X', SYSDATETIME()), 
    -- Y-Axis: Represents "Technical <-> Creative"
    (@ModelReasoning, 'Basis', CAST(REPLICATE(CAST(0x40000000 AS VARBINARY(MAX)), 100) AS VARBINARY(MAX)), 'Y', SYSDATETIME()),
    -- Z-Axis: Represents "Static <-> Dynamic"
    (@ModelReasoning, 'Basis', CAST(REPLICATE(CAST(0xC0000000 AS VARBINARY(MAX)), 100) AS VARBINARY(MAX)), 'Z', SYSDATETIME());

-- =========================================================================================
-- EPOCH 2: THE PRIMORDIAL SOUP (Atoms and CAS)
-- =========================================================================================
PRINT 'EPOCH 2: Creating Matter (Atoms & CAS)';

-- We will create atoms representing a logical reasoning chain to test A* pathing later.
-- Path:  [Problem] -> [Observation] -> [Hypothesis] -> [Solution]

DECLARE @AtomData TABLE (
    Alias NVARCHAR(50),
    Content NVARCHAR(MAX),
    Modality VARCHAR(50),
    ContentType NVARCHAR(100)
);

INSERT INTO @AtomData VALUES 
('START_NODE', 'Why is the server latency spiking at 2 AM?', 'text', 'text/question'),
('STEP_1',     'Logs show high disk I/O during backup operations.', 'text', 'text/log-analysis'),
('STEP_2',     'The backup schedule overlaps with the ETL batch job.', 'text', 'text/reasoning'),
('GOAL_NODE',  'Reschedule ETL job to 4 AM to avoid contention.', 'text', 'text/solution'),
('NOISE_1',    'The mitochondria is the powerhouse of the cell.', 'text', 'text/fact'), -- Biology Cluster (Noise)
('NOISE_2',    'def main(): print("Hello World")', 'code', 'text/python');              -- Code Cluster (Noise)

-- Insert Atoms using CAS logic
MERGE dbo.Atom AS target
USING (
    SELECT 
        Alias, 
        Content, 
        Modality, 
        ContentType, 
        HASHBYTES('SHA2_256', Content) as Hash,
        CAST(Content AS VARBINARY(MAX)) as BinValue
    FROM @AtomData
) AS source
ON target.ContentHash = source.Hash
WHEN MATCHED THEN 
    UPDATE SET ReferenceCount = ReferenceCount + 1
WHEN NOT MATCHED THEN
    INSERT (TenantId, Modality, ContentHash, ContentType, AtomicValue, ReferenceCount, CanonicalText)
    VALUES (1, source.Modality, source.Hash, source.ContentType, source.BinValue, 1, source.Content);

-- =========================================================================================
-- EPOCH 3: MAPPING THE SPACE (Embeddings, Voronoi Regions, & A* Setup)
-- =========================================================================================
PRINT 'EPOCH 3: Mapping Space (Embeddings & Geometric Topology)';

-- 3.1 Concepts (Voronoi Regions / A* Targets)
-- We define the "Solution Space" as a geometric region (Polygon).
-- The A* algorithm will try to find a path from START_NODE to inside this POLYGON.

DECLARE @SolutionConceptID INT;

MERGE provenance.Concepts AS target
USING (VALUES (1, 'System Optimization', 'A region containing valid system fixes')) AS source(T, N, D)
ON target.Name = source.N
WHEN NOT MATCHED THEN
    INSERT (TenantId, Name, Description, CreatedAt)
    VALUES (source.T, source.N, source.D, SYSDATETIME());

SELECT @SolutionConceptID = ConceptId FROM provenance.Concepts WHERE Name = 'System Optimization';

-- Define the Concept Domain (Target Region for A*)
-- Let's define a box in 3D space: X(8-12), Y(8-12), Z(8-12)
DECLARE @TargetRegion GEOMETRY = geometry::STPolyFromText('POLYGON ((8 8 0, 12 8 0, 12 12 0, 8 12 0, 8 8 0))', 0); 
-- Note: SQL Geometry is 2D projected, but we use Z pseudo-columns or assume projection to plane for simple A*

UPDATE provenance.Concepts
SET ConceptDomain = @TargetRegion,
    CentroidSpatialKey = geometry::STPointFromText('POINT(10 10 0)', 0)
WHERE ConceptId = @SolutionConceptID;

-- 3.2 Embeddings & Spatial Projection (Simulating the CLR result)
-- We manually assign coordinates to our atoms to force a navigable path.
-- Start (0,0) -> Step1 (3,3) -> Step2 (6,6) -> Goal (10,10) [Inside Target Region]

DECLARE @AtomStart BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Why is the server latency spiking at 2 AM?'));
DECLARE @AtomStep1 BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Logs show high disk I/O during backup operations.'));
DECLARE @AtomStep2 BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'The backup schedule overlaps with the ETL batch job.'));
DECLARE @AtomGoal  BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Reschedule ETL job to 4 AM to avoid contention.'));
DECLARE @AtomBio   BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'The mitochondria is the powerhouse of the cell.'));

-- Insert/Update Embeddings with explicit Geometry
-- We use a dummy binary for the vector, as we are hardcoding the SpatialKey for the test.
DECLARE @DummyVec VARBINARY(MAX) = 0x00; 

-- 1. START NODE at (0,0)
MERGE dbo.AtomEmbedding AS target
USING (SELECT @AtomStart AS AId) AS source
ON target.AtomId = source.AId AND target.ModelId = @ModelReasoning
WHEN NOT MATCHED THEN
    INSERT (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
    VALUES (@AtomStart, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(0 0 0)', 0), 0, 0, 0);

-- 2. STEP 1 at (3,3) - Distance ~4.2 from start
MERGE dbo.AtomEmbedding AS target
USING (SELECT @AtomStep1 AS AId) AS source
ON target.AtomId = source.AId AND target.ModelId = @ModelReasoning
WHEN NOT MATCHED THEN
    INSERT (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
    VALUES (@AtomStep1, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(3 3 0)', 0), 3, 3, 0);

-- 3. STEP 2 at (6,6) - Distance ~4.2 from step 1
MERGE dbo.AtomEmbedding AS target
USING (SELECT @AtomStep2 AS AId) AS source
ON target.AtomId = source.AId AND target.ModelId = @ModelReasoning
WHEN NOT MATCHED THEN
    INSERT (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
    VALUES (@AtomStep2, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(6 6 0)', 0), 6, 6, 0);

-- 4. GOAL NODE at (10,10) - Distance ~5.6 from step 2, INSIDE TARGET REGION
MERGE dbo.AtomEmbedding AS target
USING (SELECT @AtomGoal AS AId) AS source
ON target.AtomId = source.AId AND target.ModelId = @ModelReasoning
WHEN NOT MATCHED THEN
    INSERT (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
    VALUES (@AtomGoal, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(10 10 0)', 0), 10, 10, 0);

-- 5. NOISE (Biology) at (-50, -50) - Far away, should be ignored by A*
MERGE dbo.AtomEmbedding AS target
USING (SELECT @AtomBio AS AId) AS source
ON target.AtomId = source.AId AND target.ModelId = @ModelReasoning
WHEN NOT MATCHED THEN
    INSERT (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
    VALUES (@AtomBio, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(-50 -50 0)', 0), -50, -50, 0);

-- =========================================================================================
-- EPOCH 4: WAKING THE MIND (OODA Loop History & Anomalies)
-- =========================================================================================
PRINT 'EPOCH 4: Waking the Mind (Seeding Operational History)';

-- 4.1 Seed Billing/Usage Data (Base Load)
-- Simulate 30 days of steady usage
DECLARE @i INT = 0;
WHILE @i < 100
BEGIN
    INSERT INTO dbo.BillingUsageLedger (TenantId, MetricType, Quantity, Unit, UsageDate, CreatedAt)
    VALUES (1, 'TokenCount', 100 + (@i * 2), 'Tokens', DATEADD(DAY, -30 + (@i/4), SYSDATETIME()), SYSDATETIME());
    SET @i = @i + 1;
END

-- 4.2 Seed Inference Requests (The "Observe" signal)
-- Normal Operation (Last 24 hours)
INSERT INTO dbo.InferenceRequests (TenantId, ModelId, InputHash, Status, RequestTimestamp, CompletedTimestamp, TotalDurationMs)
SELECT TOP 50 
    1, 
    @ModelReasoning, 
    HASHBYTES('SHA2_256', CAST(NEWID() AS NVARCHAR(36))), 
    'Completed', 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) * 10, SYSDATETIME()), 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) * 10, DATEADD(MILLISECOND, 200, SYSDATETIME())), 
    200 -- 200ms (Fast)
FROM sys.objects;

-- Anomaly (Last 1 hour) - High Latency Spike
PRINT '   -> Seeding Latency Anomaly (2500ms spike)...';
INSERT INTO dbo.InferenceRequests (TenantId, ModelId, InputHash, Status, RequestTimestamp, CompletedTimestamp, TotalDurationMs)
SELECT TOP 10 
    1, 
    @ModelReasoning, 
    HASHBYTES('SHA2_256', CAST(NEWID() AS NVARCHAR(36))), 
    'Completed', 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), SYSDATETIME()), 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), DATEADD(MILLISECOND, 2500, SYSDATETIME())), 
    2500 -- 2.5s (Slow!)
FROM sys.objects;

-- 4.3 Seed Previous Improvements
INSERT INTO dbo.AutonomousImprovementHistory (ImprovementId, TargetFile, ChangeType, RiskLevel, SuccessScore, WasDeployed, StartedAt, CompletedAt)
VALUES (NEWID(), 'Index_Optimization', 'IndexCreate', 'Low', 0.95, 1, DATEADD(DAY, -5, SYSDATETIME()), DATEADD(DAY, -5, SYSDATETIME()));

PRINT '--------------------------------------------------';
PRINT 'Cognitive Kernel Seeded Successfully.';
PRINT '   - Physics: Defined';
PRINT '   - Matter:  Created (Path: Start -> Step1 -> Step2 -> Goal)';
PRINT '   - Space:   Mapped (Embeddings at 0,0 to 10,10)';
PRINT '   - Time:    Simulated (Latency anomalies injected)';
GO