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