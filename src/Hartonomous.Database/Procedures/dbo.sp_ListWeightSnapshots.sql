CREATE OR ALTER PROCEDURE dbo.sp_ListWeightSnapshots
AS
BEGIN
    SET NOCOUNT ON;
    
    IF OBJECT_ID('dbo.WeightSnapshot', 'U') IS NULL
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
    FROM dbo.WeightSnapshot
    ORDER BY SnapshotTime DESC;
END;