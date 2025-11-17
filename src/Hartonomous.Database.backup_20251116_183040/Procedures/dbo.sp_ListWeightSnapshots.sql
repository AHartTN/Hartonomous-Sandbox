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