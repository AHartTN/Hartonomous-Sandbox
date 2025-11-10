USE Hartonomous;
GO

IF OBJECT_ID(N'dbo.SpatialLandmarks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SpatialLandmarks (
        LandmarkId INT IDENTITY(1,1) NOT NULL,
        LandmarkVector VECTOR(1998) NOT NULL,
        LandmarkPoint GEOMETRY NULL,
        SelectionMethod NVARCHAR(50) NULL,
        Description NVARCHAR(MAX) NULL,
        CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_SpatialLandmarks PRIMARY KEY CLUSTERED (LandmarkId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SpatialLandmarks)
BEGIN
    INSERT INTO dbo.SpatialLandmarks (LandmarkVector, LandmarkPoint, SelectionMethod, Description)
    VALUES
        (CAST('[' + REPLICATE('0.1,', 1997) + '0.1]' AS VECTOR(1998)), NULL, 'initialization', 'Landmark 1: Initial anchor'),
        (CAST('[' + REPLICATE('0.5,', 1997) + '0.5]' AS VECTOR(1998)), NULL, 'initialization', 'Landmark 2: Initial anchor'),
        (CAST('[' + REPLICATE('0.9,', 1997) + '0.9]' AS VECTOR(1998)), NULL, 'initialization', 'Landmark 3: Initial anchor');
END
GO

PRINT 'Created SpatialLandmarks table with initial anchors';
