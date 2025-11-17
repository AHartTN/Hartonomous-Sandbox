-- =============================================
-- AtomEmbeddings: Semantic Representation for All Modalities
-- Enterprise-grade multi-dimensional embedding storage with spatial indexing
-- =============================================
CREATE TABLE [dbo].[AtomEmbedding] (
    [AtomEmbeddingId]   BIGINT         IDENTITY (1, 1) NOT NULL,
    [AtomId]            BIGINT         NOT NULL,
    [TenantId]          INT            NOT NULL DEFAULT 0,  -- Multi-tenancy support
    [ModelId]           INT            NOT NULL,
    [EmbeddingType]     NVARCHAR(50)   NOT NULL DEFAULT 'semantic',  -- 'semantic', 'syntactic', 'visual', etc.
    [Dimension]         INT            NOT NULL,  -- Vector dimensionality: 768, 1536, 1998, etc.

    -- 3D/4D spatial projection (semantic space) - for GEOMETRY-based spatial indexing
    [SpatialKey]        GEOMETRY       NOT NULL,
    
    -- Full-dimensional vector for similarity search - for VECTOR_DISTANCE operations
    [EmbeddingVector]   VECTOR(1998)   NULL,  -- Full embedding vector for cosine/euclidean distance

    -- Grid-based spatial bucketing for coarse-grained queries
    [SpatialBucketX]    INT            NULL,
    [SpatialBucketY]    INT            NULL,
    [SpatialBucketZ]    INT            NULL,

    -- Hilbert curve value for 1D indexing
    [HilbertValue]      BIGINT         NULL,

    [CreatedAt]         DATETIME2(7)   DEFAULT (SYSUTCDATETIME()) NOT NULL,

    CONSTRAINT [PK_AtomEmbedding] PRIMARY KEY CLUSTERED ([AtomEmbeddingId] ASC),
    CONSTRAINT [FK_AtomEmbeddings_Atom] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atom] ([AtomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AtomEmbeddings_Model] FOREIGN KEY ([ModelId])
        REFERENCES [dbo].[Model] ([ModelId])
);
GO

-- Spatial index for semantic similarity search
-- BOUNDING_BOX covers normalized embedding space (-1 to 1 for first 3 dimensions)
CREATE SPATIAL INDEX [SIX_AtomEmbedding_SpatialKey]
    ON [dbo].[AtomEmbedding]([SpatialKey])
    WITH (
        BOUNDING_BOX = (-1, -1, 1, 1),
        GRIDS = (LOW, LOW, MEDIUM, HIGH)
    );
GO

-- Performance indexes
CREATE NONCLUSTERED INDEX [IX_AtomEmbedding_AtomId]
    ON [dbo].[AtomEmbedding]([AtomId])
    INCLUDE ([ModelId], [EmbeddingType], [Dimension]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomEmbedding_TenantId_ModelId]
    ON [dbo].[AtomEmbedding]([TenantId], [ModelId], [EmbeddingType])
    INCLUDE ([AtomId], [Dimension]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomEmbedding_Dimension]
    ON [dbo].[AtomEmbedding]([Dimension], [EmbeddingType])
    INCLUDE ([AtomId], [ModelId]);
GO

CREATE NONCLUSTERED INDEX [IX_AtomEmbedding_SpatialBuckets]
    ON [dbo].[AtomEmbedding]([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ])
    INCLUDE ([AtomId], [ModelId])
    WHERE [SpatialBucketX] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_AtomEmbedding_Hilbert]
    ON [dbo].[AtomEmbedding]([HilbertValue] ASC)
    INCLUDE ([AtomId], [ModelId])
    WHERE [HilbertValue] IS NOT NULL;
GO
