-- Migration: Replace VECTOR(1998) chunked storage with GEOMETRY LINESTRING ZM
-- Date: 2025-10-28
-- Purpose: Enable variable-dimension tensor storage with Z/M coordinates for metadata
-- 
-- GEOMETRY advantages over VECTOR:
-- 1. No dimension limits (2^30 points vs 1998 max)
-- 2. Z coordinate for importance/gradient scores
-- 3. M coordinate for temporal/structural metadata  
-- 4. Spatial indexes for O(log n) queries
-- 5. No chunking required - complete tensors in single row

USE Hartonomous;
GO

-- Drop chunking index (no longer needed)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_layer_chunks' AND object_id = OBJECT_ID('dbo.ModelLayers'))
BEGIN
    PRINT 'Dropping idx_layer_chunks index...';
    DROP INDEX idx_layer_chunks ON dbo.ModelLayers;
END
GO

-- Drop old VECTOR/chunking columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'Weights')
BEGIN
    PRINT 'Dropping Weights column (VARBINARY/VECTOR)...';
    ALTER TABLE dbo.ModelLayers DROP COLUMN Weights;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'WeightsRaw')
BEGIN
    PRINT 'Dropping WeightsRaw column...';
    ALTER TABLE dbo.ModelLayers DROP COLUMN WeightsRaw;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'ActualDimension')
BEGIN
    PRINT 'Dropping ActualDimension column...';
    ALTER TABLE dbo.ModelLayers DROP COLUMN ActualDimension;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'ChunkIdx')
BEGIN
    PRINT 'Dropping ChunkIdx column...';
    ALTER TABLE dbo.ModelLayers DROP COLUMN ChunkIdx;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'TotalChunks')
BEGIN
    PRINT 'Dropping TotalChunks column...';
    ALTER TABLE dbo.ModelLayers DROP COLUMN TotalChunks;
END
GO

-- Add new GEOMETRY column for weights
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'WeightsGeometry')
BEGIN
    PRINT 'Adding WeightsGeometry GEOMETRY column...';
    ALTER TABLE dbo.ModelLayers ADD WeightsGeometry GEOMETRY NULL;
END
GO

-- Add tensor shape metadata (JSON array for reconstruction)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'TensorShape')
BEGIN
    PRINT 'Adding TensorShape column...';
    ALTER TABLE dbo.ModelLayers ADD TensorShape NVARCHAR(200) NULL;
END
GO

-- Add tensor data type (float32, float16, bfloat16)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ModelLayers') AND name = 'TensorDtype')
BEGIN
    PRINT 'Adding TensorDtype column...';
    ALTER TABLE dbo.ModelLayers ADD TensorDtype NVARCHAR(20) NULL DEFAULT 'float32';
END
GO

-- Create spatial index on WeightsGeometry for O(log n) queries
-- Bounding box tuned for typical weight ranges (-10 to 10)
-- MEDIUM grid density (8x8 = 64 cells per level) for 4 levels
-- CELLS_PER_OBJECT=16 (default) balances precision vs storage
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_weights_spatial' AND object_id = OBJECT_ID('dbo.ModelLayers'))
BEGIN
    PRINT 'Creating spatial index on WeightsGeometry...';
    CREATE SPATIAL INDEX idx_weights_spatial ON dbo.ModelLayers(WeightsGeometry)
    WITH (
        BOUNDING_BOX = (-10, -10, 10, 10),  -- Covers typical weight ranges
        GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM),  -- 8x8 grid at each of 4 levels
        CELLS_PER_OBJECT = 16  -- Default tessellation limit
    );
END
GO

PRINT 'Migration completed successfully!';
PRINT 'ModelLayers now supports variable-dimension tensors via GEOMETRY LINESTRING ZM';
PRINT 'Encoding: X=index, Y=weight, Z=importance, M=temporal';
GO
