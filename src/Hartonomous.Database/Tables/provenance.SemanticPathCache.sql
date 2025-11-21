-- =============================================
-- Semantic Path Cache Table
-- =============================================
-- Purpose: Persistent caching for A* pathfinding results
-- Avoids re-computing expensive semantic distances
-- IDEMPOTENT: Can be run multiple times safely
-- =============================================

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = 'provenance' 
      AND TABLE_NAME = 'SemanticPathCache'
)
BEGIN
    CREATE TABLE [provenance].[SemanticPathCache] (
        [StartAtomId]       BIGINT          NOT NULL,
        [TargetConceptId]   INT             NOT NULL,
        [PathJson]          NVARCHAR(MAX)   NOT NULL,  -- JSON array of path nodes
        [TotalCost]         FLOAT           NOT NULL,  -- Accumulated semantic distance
        [CreatedAt]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [ExpiresAt]         DATETIME2(7)    NOT NULL,  -- TTL for cache invalidation
        [HitCount]          INT             NOT NULL DEFAULT 0,  -- Cache hit tracking
        [LastAccessedAt]    DATETIME2(7)    NULL,

        CONSTRAINT [PK_SemanticPathCache] PRIMARY KEY CLUSTERED (
            [StartAtomId] ASC,
            [TargetConceptId] ASC
        ),

        CONSTRAINT [FK_SemanticPathCache_StartAtom] FOREIGN KEY ([StartAtomId])
            REFERENCES [dbo].[Atom] ([AtomId]),

        CONSTRAINT [FK_SemanticPathCache_TargetConcept] FOREIGN KEY ([TargetConceptId])
            REFERENCES [dbo].[Concept] ([ConceptId])
    );

    PRINT '? Created table: provenance.SemanticPathCache';
END
ELSE
BEGIN
    PRINT '? Table provenance.SemanticPathCache already exists';
END
GO

-- Non-clustered index for expiration cleanup (IDEMPOTENT)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID('provenance.SemanticPathCache') 
      AND name = 'IX_SemanticPathCache_Expiration'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SemanticPathCache_Expiration]
    ON [provenance].[SemanticPathCache] ([ExpiresAt] ASC)
    INCLUDE ([StartAtomId], [TargetConceptId]);
    
    PRINT '? Created index: IX_SemanticPathCache_Expiration';
END
ELSE
BEGIN
    PRINT '? Index IX_SemanticPathCache_Expiration already exists';
END
GO

-- Index for cache hit analysis (IDEMPOTENT)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID('provenance.SemanticPathCache') 
      AND name = 'IX_SemanticPathCache_HitCount'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SemanticPathCache_HitCount]
    ON [provenance].[SemanticPathCache] ([HitCount] DESC)
    INCLUDE ([StartAtomId], [TargetConceptId], [TotalCost]);
    
    PRINT '? Created index: IX_SemanticPathCache_HitCount';
END
ELSE
BEGIN
    PRINT '? Index IX_SemanticPathCache_HitCount already exists';
END
GO

PRINT '? provenance.SemanticPathCache deployment complete';
GO
