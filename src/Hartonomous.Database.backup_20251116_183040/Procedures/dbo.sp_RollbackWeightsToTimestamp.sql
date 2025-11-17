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