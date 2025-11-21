-- =============================================
-- Concept: Semantic Concept Clustering
-- Represents clusters, domains, and categories of Atoms
-- =============================================
CREATE TABLE [dbo].[Concept] (
    [ConceptId]         BIGINT          IDENTITY(1,1) NOT NULL,
    [TenantId]          INT             NOT NULL DEFAULT 0,
    [ConceptName]       NVARCHAR(256)   NOT NULL,
    [Description]       NVARCHAR(MAX)   NULL,
    [ConceptType]       NVARCHAR(50)    NULL,          -- 'Cluster', 'Domain', 'Category', etc.
    [ParentConceptId]   BIGINT          NULL,          -- Hierarchical relationships
    
    -- Semantic representation
    [CentroidVector]    VARBINARY(MAX)  NULL,          -- Centroid embedding for clustering
    [CentroidSpatialKey] HIERARCHYID    NULL,          -- Spatial key for geometric queries
    [Domain]            geometry        NULL,          -- Spatial domain boundary
    [Radius]            FLOAT           NULL,          -- Domain radius for containment checks
    
    -- Metadata
    [AtomCount]         INT             NOT NULL DEFAULT 0,  -- Number of atoms in this concept
    [Confidence]        DECIMAL(5,4)    NULL,          -- Clustering confidence (0-1)
    [Metadata]          json            NULL,          -- Extensible metadata
    
    -- Audit fields
    [CreatedAt]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2(7)    NULL,
    [CreatedBy]         NVARCHAR(128)   NULL,
    
    CONSTRAINT [PK_Concept] PRIMARY KEY CLUSTERED ([ConceptId] ASC),
    CONSTRAINT [UQ_Concept_TenantName] UNIQUE ([TenantId], [ConceptName]),
    CONSTRAINT [FK_Concept_Parent] FOREIGN KEY ([ParentConceptId]) 
        REFERENCES [dbo].[Concept]([ConceptId]),
    CONSTRAINT [CK_Concept_Confidence] CHECK ([Confidence] >= 0 AND [Confidence] <= 1)
);
GO

-- =============================================
-- INDEXES
-- =============================================

-- Multi-tenant queries
CREATE NONCLUSTERED INDEX [IX_Concept_TenantId]
    ON [dbo].[Concept]([TenantId], [ConceptType])
    INCLUDE ([ConceptName], [AtomCount]);
GO

-- Hierarchical navigation
CREATE NONCLUSTERED INDEX [IX_Concept_Parent]
    ON [dbo].[Concept]([ParentConceptId])
    INCLUDE ([ConceptId], [ConceptName])
    WHERE [ParentConceptId] IS NOT NULL;
GO

-- Name lookups
CREATE NONCLUSTERED INDEX [IX_Concept_Name]
    ON [dbo].[Concept]([ConceptName])
    INCLUDE ([ConceptId], [TenantId]);
GO

-- Spatial queries
CREATE SPATIAL INDEX [SIDX_Concept_Domain]
    ON [dbo].[Concept]([Domain])
    USING GEOMETRY_GRID
    WITH (
        BOUNDING_BOX = (-180, -90, 180, 90),
        GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
        CELLS_PER_OBJECT = 16
    );
GO
