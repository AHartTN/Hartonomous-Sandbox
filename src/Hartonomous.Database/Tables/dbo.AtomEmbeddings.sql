USE Hartonomous;
GO

IF OBJECT_ID(N'dbo.AtomEmbeddings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AtomEmbeddings (
        AtomEmbeddingId BIGINT IDENTITY(1,1) NOT NULL,
        AtomId BIGINT NOT NULL,
        EmbeddingVector VECTOR(1998) NOT NULL,
        SpatialGeometry GEOMETRY NOT NULL,
        SpatialCoarse GEOMETRY NOT NULL,
        SpatialBucket INT NOT NULL,
        SpatialBucketX INT NULL,
        SpatialBucketY INT NULL,
        SpatialBucketZ INT NULL,
        ModelId INT NULL,
        EmbeddingType NVARCHAR(50) NOT NULL DEFAULT 'semantic',
        LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AtomEmbeddings PRIMARY KEY CLUSTERED (AtomEmbeddingId),
        CONSTRAINT FK_AtomEmbeddings_Atoms FOREIGN KEY (AtomId)
            REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomEmbeddings_Atom' AND object_id = OBJECT_ID(N'dbo.AtomEmbeddings'))
BEGIN
    CREATE INDEX IX_AtomEmbeddings_Atom ON dbo.AtomEmbeddings (AtomId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomEmbeddings_Bucket' AND object_id = OBJECT_ID(N'dbo.AtomEmbeddings'))
BEGIN
    CREATE INDEX IX_AtomEmbeddings_Bucket ON dbo.AtomEmbeddings (SpatialBucket);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomEmbeddings_BucketXYZ' AND object_id = OBJECT_ID(N'dbo.AtomEmbeddings'))
BEGIN
    CREATE INDEX IX_AtomEmbeddings_BucketXYZ ON dbo.AtomEmbeddings (SpatialBucketX, SpatialBucketY, SpatialBucketZ)
        WHERE SpatialBucketX IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomEmbeddings_Spatial' AND object_id = OBJECT_ID(N'dbo.AtomEmbeddings'))
BEGIN
    CREATE SPATIAL INDEX IX_AtomEmbeddings_Spatial
        ON dbo.AtomEmbeddings(SpatialGeometry)
        WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AtomEmbeddings_Coarse' AND object_id = OBJECT_ID(N'dbo.AtomEmbeddings'))
BEGIN
    CREATE SPATIAL INDEX IX_AtomEmbeddings_Coarse
        ON dbo.AtomEmbeddings(SpatialCoarse)
        WITH (GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW));
END
GO

PRINT 'Created AtomEmbeddings table with spatial indexes';
