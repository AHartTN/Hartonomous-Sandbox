CREATE OR ALTER PROCEDURE dbo.sp_RestoreWeightSnapshot
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
    FROM dbo.WeightSnapshot
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