CREATE TABLE [dbo].[AtomEmbeddings]
(
    [AtomEmbeddingId] BIGINT IDENTITY(1,1) NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [EmbeddingVector] VECTOR(1998) NOT NULL,
    [Dimension] INT NOT NULL DEFAULT (1998),
    [SpatialGeometry] GEOMETRY NOT NULL,
    [SpatialCoarse] GEOMETRY NOT NULL,
    [SpatialProjection3D] GEOMETRY NULL,
    [SpatialBucket] INT NOT NULL,
    [SpatialBucketX] INT NULL,
    [SpatialBucketY] INT NULL,
    [SpatialBucketZ] INT NULL,
    [SpatialProjX] FLOAT NULL,
    [SpatialProjY] FLOAT NULL,
    [SpatialProjZ] FLOAT NULL,
    [ModelId] INT NULL,
    [EmbeddingType] NVARCHAR(50) NOT NULL DEFAULT ('semantic'),
    [Metadata] JSON NULL,
    [TenantId] INT NOT NULL DEFAULT (0),
    [LastUpdated] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastComputedUtc] DATETIME2 NULL,
    [LastAccessedUtc] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY CLUSTERED ([AtomEmbeddingId]),
    CONSTRAINT [FK_AtomEmbeddings_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atoms]([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomEmbeddings_Models] FOREIGN KEY ([ModelId])
        REFERENCES [dbo].[Models]([ModelId]) ON DELETE SET NULL
);
