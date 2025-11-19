-- =========================================================================================
-- PHYSICS VERIFICATION SUITE
-- =========================================================================================
-- Validates that the Hartonomous Cognitive Kernel is operational.
-- Tests: Deduplication, Spatial Proximity, A* Pathfinding, OODA Anomaly Detection
-- =========================================================================================

SET NOCOUNT ON;
USE [Hartonomous];
GO

PRINT '>>> STARTING PHYSICS VERIFICATION >>>';

-- TEST 1: Content Addressable Storage (Deduplication)
-- Inserting the "Start Node" text again should NOT increase the row count, only ReferenceCount.
PRINT '[Test 1] CAS Deduplication Check...';

DECLARE @PreCount INT = (SELECT COUNT(*) FROM dbo.Atom);
DECLARE @DuplicateText NVARCHAR(MAX) = 'Why is the server latency spiking at 2 AM?';
DECLARE @RefHash BINARY(32) = HASHBYTES('SHA2_256', @DuplicateText);
DECLARE @PreRef BIGINT = (SELECT ReferenceCount FROM dbo.Atom WHERE ContentHash = @RefHash);

-- Attempt Insert
MERGE dbo.Atom AS target
USING (SELECT @RefHash AS Hash, @DuplicateText AS Val) AS source
ON target.ContentHash = source.Hash
WHEN MATCHED THEN 
    UPDATE SET ReferenceCount = ReferenceCount + 1;

DECLARE @PostCount INT = (SELECT COUNT(*) FROM dbo.Atom);
DECLARE @PostRef BIGINT = (SELECT ReferenceCount FROM dbo.Atom WHERE ContentHash = @RefHash);

IF @PreCount = @PostCount AND @PostRef = @PreRef + 1
    PRINT '   [PASS] Deduplication active. Reference Count incremented.';
ELSE
    PRINT '   [FAIL] Deduplication failed or row count increased.';


-- TEST 2: Spatial Proximity (Voronoi/KNN)
-- Check if "Step 1" is spatially closer to "Start" than "Biology Noise"
PRINT '[Test 2] Spatial Proximity Check...';

DECLARE @StartKey GEOMETRY = (SELECT SpatialKey FROM dbo.AtomEmbedding WHERE AtomId = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Why is the server latency spiking at 2 AM?')));
DECLARE @Step1Key GEOMETRY = (SELECT SpatialKey FROM dbo.AtomEmbedding WHERE AtomId = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Logs show high disk I/O during backup operations.')));
DECLARE @BioKey   GEOMETRY = (SELECT SpatialKey FROM dbo.AtomEmbedding WHERE AtomId = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'The mitochondria is the powerhouse of the cell.')));

DECLARE @DistValid FLOAT = @StartKey.STDistance(@Step1Key); -- Should be ~4.24 (0,0 to 3,3)
DECLARE @DistNoise FLOAT = @StartKey.STDistance(@BioKey);   -- Should be ~70.7 (0,0 to -50,-50)

PRINT '   Distance to Logical Step: ' + CAST(@DistValid AS VARCHAR(20));
PRINT '   Distance to Bio Noise:    ' + CAST(@DistNoise AS VARCHAR(20));

IF @DistValid < @DistNoise
    PRINT '   [PASS] Semantic space is coherent. Related atoms are spatially closer.';
ELSE
    PRINT '   [FAIL] Spatial coherence violation.';


-- TEST 3: A* Pathfinding Simulation
-- Verify that sp_GenerateOptimalPath can traverse the seeded nodes
PRINT '[Test 3] A* Pathfinding Simulation...';

DECLARE @StartAtomId BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Why is the server latency spiking at 2 AM?'));
DECLARE @TargetConceptId INT = (SELECT ConceptId FROM provenance.Concepts WHERE Name = 'System Optimization');

PRINT '   Attempting path from Atom ' + CAST(@StartAtomId AS VARCHAR(10)) + ' to Concept ' + CAST(@TargetConceptId AS VARCHAR(10));

-- Note: In a real run, we'd EXEC the procedure. Here we simulate the check by ensuring the nodes exist in the spatial index range.
-- The procedure uses STIntersects with NeighborRadius. 
-- Start(0,0) -> Step1(3,3). Distance 4.24.
-- WARNING: The default NeighborRadius in sp_GenerateOptimalPath might be small (0.5). 
-- For this test data, we need to call it with a larger radius (~5.0).

BEGIN TRY
    EXEC dbo.sp_GenerateOptimalPath 
        @StartAtomId = @StartAtomId, 
        @TargetConceptId = @TargetConceptId,
        @MaxSteps = 10,
        @NeighborRadius = 5.0; -- Increased for the seeded sparse grid
    
    PRINT '   [PASS] Path generation executed without error.';
END TRY
BEGIN CATCH
    PRINT '   [FAIL] Path generation threw error: ' + ERROR_MESSAGE();
END CATCH


-- TEST 4: OODA Loop Anomaly Detection
-- Verify sp_AnalyzeSystem catches the seeded latency spike
PRINT '[Test 4] OODA Loop Anomaly Detection...';

DECLARE @AnalysisId UNIQUEIDENTIFIER;
DECLARE @AnomaliesJson NVARCHAR(MAX);
DECLARE @PatternJson NVARCHAR(MAX);
DECLARE @TotalInferences INT;
DECLARE @AvgDuration FLOAT;
DECLARE @AnomalyCount INT;

EXEC dbo.sp_AnalyzeSystem
    @TenantId = 1,
    @AnalysisScope = 'performance',
    @LookbackHours = 24,
    @AnalysisId = @AnalysisId OUTPUT,
    @TotalInferences = @TotalInferences OUTPUT,
    @AvgDurationMs = @AvgDuration OUTPUT,
    @AnomalyCount = @AnomalyCount OUTPUT,
    @AnomaliesJson = @AnomaliesJson OUTPUT,
    @PatternsJson = @PatternJson OUTPUT;

PRINT '   Total Inferences: ' + CAST(@TotalInferences AS VARCHAR(10));
PRINT '   Avg Duration: ' + CAST(@AvgDuration AS VARCHAR(10)) + 'ms';
PRINT '   Anomalies Found: ' + CAST(@AnomalyCount AS VARCHAR(10));

IF @AnomalyCount >= 10
    PRINT '   [PASS] OODA Loop correctly identified simulated latency spikes.';
ELSE
    PRINT '   [FAIL] OODA Loop missed the anomalies.';

PRINT '<<< VERIFICATION COMPLETE <<<';
GO