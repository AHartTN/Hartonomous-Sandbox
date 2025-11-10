CREATE TABLE [dbo].[SpatialLandmarks]
(
    [LandmarkId] INT IDENTITY(1,1) NOT NULL,
    [LandmarkVector] VECTOR(1998) NOT NULL,
    [LandmarkPoint] GEOMETRY NULL,
    [SelectionMethod] NVARCHAR(50) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_SpatialLandmarks] PRIMARY KEY CLUSTERED ([LandmarkId])
);
GO