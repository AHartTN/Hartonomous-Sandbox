CREATE TABLE [provenance].[Concepts] (
    [ConceptId]            BIGINT         NOT NULL IDENTITY,
    [ConceptName]          NVARCHAR (200) NOT NULL,
    [Description]          NVARCHAR (MAX) NULL,
    [CentroidVector]       VARBINARY (MAX)NOT NULL,
    [Centroid]             VARBINARY (MAX)NULL,

    -- Spatial representations for semantic domains
    [CentroidSpatialKey]   GEOMETRY       NULL,  -- 3D centroid position
    [ConceptDomain]        GEOMETRY       NULL,  -- Voronoi domain polygon
    [HilbertValue]         BIGINT         NULL,  -- Hilbert curve index for fast lookups

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

-- Spatial index for concept domains (BOUNDING_BOX required for GEOMETRY type)
CREATE SPATIAL INDEX [SIX_Concepts_ConceptDomain] 
ON [provenance].[Concepts]([ConceptDomain])
WITH (BOUNDING_BOX = (-1, -1, 1, 1));
GO

CREATE SPATIAL INDEX [SIX_Concepts_CentroidSpatialKey] 
ON [provenance].[Concepts]([CentroidSpatialKey])
WITH (BOUNDING_BOX = (-1, -1, 1, 1));
GO