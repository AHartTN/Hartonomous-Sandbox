-- ===============================================
-- Weight Rollback and Restoration Procedures
-- Purpose: Restore weights to previous states
-- ===============================================
-- Created: 2025-11-08
-- Part of: Phase 3 - Temporal Tables Implementation
-- Reference: docs/audit/03-TEMPORAL-TABLES.md
-- ===============================================

USE Hartonomous;
GO

-- ===============================================
-- Procedure: Rollback Weights to Specific Timestamp
-- ===============================================
IF OBJECT_ID('dbo.sp_RollbackWeightsToTimestamp', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RollbackWeightsToTimestamp;
GO

CREATE PROCEDURE dbo.sp_RollbackWeightsToTimestamp
    @TargetDateTime DATETIME2(7),
    @ModelId INT = NULL,
    @DryRun BIT = 1  -- Safety: default to dry run
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RowsToUpdate INT;
    DECLARE @CurrentTime DATETIME2(7) = SYSUTCDATETIME();
    
    -- Validate target date
    IF @TargetDateTime > @CurrentTime
    BEGIN
        RAISERROR('Cannot rollback to future date', 16, 1);
        RETURN;
    END;
    
    -- Create temp table with target weights
    IF OBJECT_ID('tempdb..#RollbackWeights') IS NOT NULL
        DROP TABLE #RollbackWeights;
    
    CREATE TABLE #RollbackWeights
    (
        TensorAtomCoefficientId BIGINT PRIMARY KEY,
        TensorAtomId BIGINT,
        ModelId INT,
        CurrentCoefficient REAL,
        TargetCoefficient REAL,
        Delta REAL
    );
    
    -- Get weights at target time
    INSERT INTO #RollbackWeights
    SELECT 
        tac_current.TensorAtomCoefficientId,
        tac_current.TensorAtomId,
        ta.ModelId,
        tac_current.Coefficient AS CurrentCoefficient,
        tac_target.Coefficient AS TargetCoefficient,
        tac_target.Coefficient - tac_current.Coefficient AS Delta
    FROM 
        dbo.TensorAtomCoefficients tac_current
        INNER JOIN dbo.TensorAtoms ta ON tac_current.TensorAtomId = ta.TensorAtomId
        INNER JOIN dbo.TensorAtomCoefficients FOR SYSTEM_TIME AS OF @TargetDateTime tac_target 
            ON tac_current.TensorAtomCoefficientId = tac_target.TensorAtomCoefficientId
    WHERE 
        (@ModelId IS NULL OR ta.ModelId = @ModelId)
        AND tac_current.Coefficient <> tac_target.Coefficient;
    
    SELECT @RowsToUpdate = COUNT(*) FROM #RollbackWeights;
    
    -- Report what will be changed
    PRINT '============================================';
    PRINT 'WEIGHT ROLLBACK PLAN';
    PRINT '============================================';
    PRINT 'Target DateTime: ' + CONVERT(VARCHAR, @TargetDateTime, 121);
    PRINT 'Current DateTime: ' + CONVERT(VARCHAR, @CurrentTime, 121);
    PRINT 'Model Filter: ' + ISNULL(CAST(@ModelId AS VARCHAR), 'ALL');
    PRINT 'Weights to Update: ' + CAST(@RowsToUpdate AS VARCHAR);
    PRINT '';
    
    IF @RowsToUpdate = 0
    BEGIN
        PRINT 'No weights need to be rolled back.';
        RETURN;
    END;
    
    -- Show summary statistics
    SELECT 
        'Summary' AS Section,
        COUNT(*) AS WeightsToChange,
        AVG(ABS(Delta)) AS AvgAbsDelta,
        MIN(Delta) AS MinDelta,
        MAX(Delta) AS MaxDelta,
        SUM(CASE WHEN Delta > 0 THEN 1 ELSE 0 END) AS WeightsIncreasing,
        SUM(CASE WHEN Delta < 0 THEN 1 ELSE 0 END) AS WeightsDecreasing
    FROM #RollbackWeights;
    
    -- Show top 10 changes by magnitude
    PRINT '';
    PRINT 'Top 10 Largest Changes:';
    SELECT TOP 10
        TensorAtomCoefficientId,
        TensorAtomId,
        ModelId,
        CurrentCoefficient,
        TargetCoefficient,
        Delta,
        ABS(Delta) AS AbsDelta
    FROM #RollbackWeights
    ORDER BY ABS(Delta) DESC;
    
    -- Execute or simulate
    IF @DryRun = 1
    BEGIN
        PRINT '';
        PRINT '*** DRY RUN MODE ***';
        PRINT 'No changes were made.';
        PRINT 'To execute rollback, set @DryRun = 0';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT 'Executing rollback...';
        
        BEGIN TRY
            BEGIN TRANSACTION;
            
            UPDATE tac
            SET tac.Coefficient = rb.TargetCoefficient
            FROM dbo.TensorAtomCoefficients tac
            INNER JOIN #RollbackWeights rb ON tac.TensorAtomCoefficientId = rb.TensorAtomCoefficientId;
            
            COMMIT TRANSACTION;
            
            PRINT '✅ Rollback completed successfully';
            PRINT '   ' + CAST(@RowsToUpdate AS VARCHAR) + ' weights restored to ' + CONVERT(VARCHAR, @TargetDateTime, 121);
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;
            
            DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
            RAISERROR('Rollback failed: %s', 16, 1, @ErrorMessage);
        END CATCH;
    END;
    
    DROP TABLE #RollbackWeights;
END;
GO

PRINT '✓ Created procedure: sp_RollbackWeightsToTimestamp';
GO

-- ===============================================
-- Procedure: Create Weight Snapshot (Backup)
-- ===============================================
IF OBJECT_ID('dbo.sp_CreateWeightSnapshot', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateWeightSnapshot;
GO

CREATE PROCEDURE dbo.sp_CreateWeightSnapshot
    @SnapshotName NVARCHAR(255),
    @ModelId INT = NULL,
    @Description NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SnapshotTime DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @WeightCount INT;
    
    -- Create snapshots table if it doesn't exist
    IF OBJECT_ID('dbo.WeightSnapshots', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.WeightSnapshots
        (
            SnapshotId BIGINT IDENTITY(1,1) PRIMARY KEY,
            SnapshotName NVARCHAR(255) UNIQUE NOT NULL,
            ModelId INT NULL,
            SnapshotTime DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            Description NVARCHAR(MAX) NULL,
            WeightCount INT NOT NULL,
            CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
        );
        
        PRINT 'Created WeightSnapshots table';
    END;
    
    -- Check for duplicate snapshot name
    IF EXISTS (SELECT 1 FROM dbo.WeightSnapshots WHERE SnapshotName = @SnapshotName)
    BEGIN
        RAISERROR('Snapshot name already exists', 16, 1);
        RETURN;
    END;
    
    -- Count weights being snapshotted
    SELECT @WeightCount = COUNT(*)
    FROM dbo.TensorAtomCoefficients tac
    INNER JOIN dbo.TensorAtoms ta ON tac.TensorAtomId = ta.TensorAtomId
    WHERE @ModelId IS NULL OR ta.ModelId = @ModelId;
    
    -- Insert snapshot metadata
    INSERT INTO dbo.WeightSnapshots (SnapshotName, ModelId, SnapshotTime, Description, WeightCount)
    VALUES (@SnapshotName, @ModelId, @SnapshotTime, @Description, @WeightCount);
    
    PRINT '============================================';
    PRINT 'WEIGHT SNAPSHOT CREATED';
    PRINT '============================================';
    PRINT 'Name: ' + @SnapshotName;
    PRINT 'Time: ' + CONVERT(VARCHAR, @SnapshotTime, 121);
    PRINT 'Model: ' + ISNULL(CAST(@ModelId AS VARCHAR), 'ALL');
    PRINT 'Weights: ' + CAST(@WeightCount AS VARCHAR);
    PRINT '';
    PRINT 'To restore this snapshot:';
    PRINT '  EXEC sp_RestoreWeightSnapshot @SnapshotName = ''' + @SnapshotName + '''';
END;
GO

PRINT '✓ Created procedure: sp_CreateWeightSnapshot';
GO

-- ===============================================
-- Procedure: Restore Weight Snapshot
-- ===============================================
IF OBJECT_ID('dbo.sp_RestoreWeightSnapshot', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RestoreWeightSnapshot;
GO

CREATE PROCEDURE dbo.sp_RestoreWeightSnapshot
    @SnapshotName NVARCHAR(255),
    @DryRun BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SnapshotTime DATETIME2(7);
    DECLARE @ModelId INT;
    
    -- Get snapshot details
    SELECT 
        @SnapshotTime = SnapshotTime,
        @ModelId = ModelId
    FROM dbo.WeightSnapshots
    WHERE SnapshotName = @SnapshotName;
    
    IF @SnapshotTime IS NULL
    BEGIN
        RAISERROR('Snapshot not found', 16, 1);
        RETURN;
    END;
    
    PRINT '============================================';
    PRINT 'RESTORING WEIGHT SNAPSHOT';
    PRINT '============================================';
    PRINT 'Snapshot: ' + @SnapshotName;
    PRINT '';
    
    -- Use rollback procedure to restore
    EXEC dbo.sp_RollbackWeightsToTimestamp 
        @TargetDateTime = @SnapshotTime,
        @ModelId = @ModelId,
        @DryRun = @DryRun;
END;
GO

PRINT '✓ Created procedure: sp_RestoreWeightSnapshot';
GO

-- ===============================================
-- Procedure: List Weight Snapshots
-- ===============================================
IF OBJECT_ID('dbo.sp_ListWeightSnapshots', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ListWeightSnapshots;
GO

CREATE PROCEDURE dbo.sp_ListWeightSnapshots
AS
BEGIN
    SET NOCOUNT ON;
    
    IF OBJECT_ID('dbo.WeightSnapshots', 'U') IS NULL
    BEGIN
        PRINT 'No snapshots exist yet.';
        RETURN;
    END;
    
    SELECT 
        SnapshotId,
        SnapshotName,
        ModelId,
        SnapshotTime,
        WeightCount,
        Description,
        CreatedAt
    FROM dbo.WeightSnapshots
    ORDER BY SnapshotTime DESC;
END;
GO

PRINT '✓ Created procedure: sp_ListWeightSnapshots';
GO

PRINT '';
PRINT '============================================';
PRINT 'Weight Rollback Tools Deployed';
PRINT '============================================';
PRINT 'Procedures:';
PRINT '  • sp_RollbackWeightsToTimestamp - Restore weights to any point in time';
PRINT '  • sp_CreateWeightSnapshot - Create named backup of current weights';
PRINT '  • sp_RestoreWeightSnapshot - Restore from named snapshot';
PRINT '  • sp_ListWeightSnapshots - View all available snapshots';
PRINT '';
PRINT 'Usage Examples:';
PRINT '  -- Create snapshot before risky experiment:';
PRINT '  EXEC sp_CreateWeightSnapshot @SnapshotName = ''BeforeExperiment'', @Description = ''Baseline weights'';';
PRINT '';
PRINT '  -- Test rollback (dry run):';
PRINT '  EXEC sp_RollbackWeightsToTimestamp @TargetDateTime = ''2025-11-08 10:00:00'', @DryRun = 1;';
PRINT '';
PRINT '  -- Execute rollback:';
PRINT '  EXEC sp_RollbackWeightsToTimestamp @TargetDateTime = ''2025-11-08 10:00:00'', @DryRun = 0;';
PRINT '';
PRINT '  -- Restore snapshot:';
PRINT '  EXEC sp_RestoreWeightSnapshot @SnapshotName = ''BeforeExperiment'', @DryRun = 0;';
GO
