CREATE TABLE [dbo].[AtomEmbeddings]
(
    [AtomEmbeddingId] BIGINT IDENTITY(1,1) NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [EmbeddingVector] VECTOR(1998) NOT NULL,
    [SpatialGeometry] GEOMETRY NOT NULL,
    [SpatialCoarse] GEOMETRY NOT NULL,
    [SpatialBucket] INT NOT NULL,
    [SpatialBucketX] INT NULL,
    [SpatialBucketY] INT NULL,
    [SpatialBucketZ] INT NULL,
    [ModelId] INT NULL,
    [EmbeddingType] NVARCHAR(50) NOT NULL DEFAULT ('semantic'),
    [LastUpdated] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY CLUSTERED ([AtomEmbeddingId]),
    CONSTRAINT [FK_AtomEmbeddings_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atoms]([AtomId]) ON DELETE CASCADE
);
