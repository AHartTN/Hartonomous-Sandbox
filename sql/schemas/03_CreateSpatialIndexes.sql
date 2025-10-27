-- CREATE SPATIAL INDEXES with proper SET options
-- Requires SET QUOTED_IDENTIFIER ON for spatial indexes
USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '============================================================';
PRINT 'CREATING SPATIAL INDEXES';
PRINT '============================================================';
GO

-- Drop existing indexes if they exist (cleanup)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_spatial_fine' AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
    DROP INDEX idx_spatial_fine ON dbo.Embeddings_Production;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_spatial_coarse' AND object_id = OBJECT_ID('dbo.Embeddings_Production'))
    DROP INDEX idx_spatial_coarse ON dbo.Embeddings_Production;
GO

-- Create fine-grained spatial index
PRINT 'Creating fine spatial index on Embeddings_Production...';
CREATE SPATIAL INDEX idx_spatial_fine
ON dbo.Embeddings_Production(spatial_geometry)
WITH (
    BOUNDING_BOX = (-10, -10, 10, 10),
    GRIDS = (
        LEVEL_1 = HIGH,
        LEVEL_2 = HIGH,
        LEVEL_3 = MEDIUM,
        LEVEL_4 = LOW
    ),
    CELLS_PER_OBJECT = 16
);
PRINT '  ✓ Fine spatial index created';
GO

-- Create coarse-grained spatial index for initial filtering
PRINT 'Creating coarse spatial index on Embeddings_Production...';
CREATE SPATIAL INDEX idx_spatial_coarse
ON dbo.Embeddings_Production(spatial_coarse)
WITH (
    BOUNDING_BOX = (-10, -10, 10, 10),
    GRIDS = (
        LEVEL_1 = MEDIUM,
        LEVEL_2 = LOW,
        LEVEL_3 = LOW,
        LEVEL_4 = LOW
    ),
    CELLS_PER_OBJECT = 8
);
PRINT '  ✓ Coarse spatial index created';
GO

PRINT '';
PRINT '============================================================';
PRINT 'SPATIAL INDEXES CREATED SUCCESSFULLY';
PRINT '============================================================';
PRINT 'Indexes created:';
PRINT '  1. idx_spatial_fine (4-level hierarchy, high density)';
PRINT '  2. idx_spatial_coarse (fast filtering, low density)';
PRINT '';
PRINT 'Usage:';
PRINT '  - Multi-resolution queries use both indexes';
PRINT '  - Coarse index for initial filtering';
PRINT '  - Fine index for precise spatial queries';
PRINT '============================================================';
GO
