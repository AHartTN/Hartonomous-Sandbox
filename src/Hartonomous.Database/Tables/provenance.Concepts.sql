CREATE TABLE [provenance].[Concepts] (
    [ConceptId]            BIGINT         NOT NULL IDENTITY,
    [ConceptName]          NVARCHAR (200) NOT NULL,
    [Description]          NVARCHAR (MAX) NULL,
    [CentroidVector]       VARBINARY (MAX)NOT NULL,
    [Centroid]             VARBINARY (MAX)NULL,
    
    -- Spatial representations for semantic domains
    [CentroidSpatialKey]   GEOMETRY       NULL,  -- 3D centroid position
    [ConceptDomain]        GEOMETRY       NULL,  -- Voronoi domain polygon
    
    [VectorDimension]      INT            NOT NULL,
    [MemberCount]          INT            NOT NULL DEFAULT 0,
    [AtomCount]            INT            NOT NULL DEFAULT 0,
    [CoherenceScore]       FLOAT (53)     NULL,
    [Coherence]            FLOAT (53)     NULL,
    [SeparationScore]      FLOAT (53)     NULL,
    [SpatialBucket]        INT            NULL,
    [DiscoveryMethod]      NVARCHAR (100) NOT NULL,
    [ModelId]              INT            NOT NULL,
    [TenantId]             INT            NOT NULL DEFAULT (0),
    [DiscoveredAt]         DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastUpdatedAt]        DATETIME2 (7)  NULL,
    [IsActive]             BIT            NOT NULL DEFAULT CAST(1 AS BIT),
    CONSTRAINT [PK_Concepts] PRIMARY KEY CLUSTERED ([ConceptId] ASC),
    CONSTRAINT [FK_Concepts_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId]) ON DELETE CASCADE
);
GO

-- Spatial index for concept domains
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_Concepts_ConceptDomain' AND object_id = OBJECT_ID('provenance.Concepts'))
    CREATE SPATIAL INDEX [SIX_Concepts_ConceptDomain] 
    ON [provenance].[Concepts]([ConceptDomain])
    WHERE [ConceptDomain] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_Concepts_CentroidSpatialKey' AND object_id = OBJECT_ID('provenance.Concepts'))
    CREATE SPATIAL INDEX [SIX_Concepts_CentroidSpatialKey] 
    ON [provenance].[Concepts]([CentroidSpatialKey])
    WHERE [CentroidSpatialKey] IS NOT NULL;
GO