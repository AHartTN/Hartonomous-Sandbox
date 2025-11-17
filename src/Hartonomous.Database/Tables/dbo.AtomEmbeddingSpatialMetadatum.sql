CREATE TABLE [dbo].[AtomEmbeddingSpatialMetadatum] (
    [MetadataId]      BIGINT        NOT NULL IDENTITY,
    [SpatialBucketX]  INT           NOT NULL,
    [SpatialBucketY]  INT           NOT NULL,
    [SpatialBucketZ]  INT           NOT NULL,
    [HasZ]            BIT           NOT NULL,
    [EmbeddingCount]  BIGINT        NOT NULL,
    [MinProjX]        FLOAT (53)    NULL,
    [MaxProjX]        FLOAT (53)    NULL,
    [MinProjY]        FLOAT (53)    NULL,
    [MaxProjY]        FLOAT (53)    NULL,
    [MinProjZ]        FLOAT (53)    NULL,
    [MaxProjZ]        FLOAT (53)    NULL,
    [UpdatedAt]       DATETIME2 (7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AtomEmbeddingSpatialMetadata] PRIMARY KEY CLUSTERED ([MetadataId] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_AtomEmbeddingSpatialMetadata_BucketXYZ] 
    ON [dbo].[AtomEmbeddingSpatialMetadatum] ([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ], [HasZ]);
GO
