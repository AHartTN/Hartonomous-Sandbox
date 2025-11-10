-- Creates and repairs spatial indexes required by the platform. Runnable multiple times safely.

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

-- ==========================================
-- AtomEmbeddings.SpatialGeometry (Fine-grained)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_fine'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    DROP INDEX idx_spatial_fine ON dbo.AtomEmbeddings;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbeddings_SpatialGeometry' 
    AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
    ON dbo.AtomEmbeddings (SpatialGeometry)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = MEDIUM
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    END
ELSE
BEGIN
    END;

-- ==========================================
-- AtomEmbeddings.SpatialCoarse (Fast filter)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_coarse'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    DROP INDEX idx_spatial_coarse ON dbo.AtomEmbeddings;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbeddings_SpatialCoarse' 
    AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialCoarse
    ON dbo.AtomEmbeddings (SpatialCoarse)
    WITH (
        BOUNDING_BOX = (-100, -100, 100, 100),
        GRIDS = (
            LEVEL_1 = LOW,
            LEVEL_2 = LOW,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 8,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    END
ELSE
BEGIN
    END;

-- ==========================================
-- TensorAtoms.SpatialSignature (Weight signatures)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_signature'
      AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    DROP INDEX idx_spatial_signature ON dbo.TensorAtoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TensorAtoms_SpatialSignature' 
    AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    CREATE SPATIAL INDEX IX_TensorAtoms_SpatialSignature
    ON dbo.TensorAtoms (SpatialSignature)
    WITH (
        BOUNDING_BOX = (-500, -500, 500, 500),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 12,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    END
ELSE
BEGIN
    END;

-- ==========================================
-- TensorAtoms.GeometryFootprint (Weight topology)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_geometry_footprint'
      AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    DROP INDEX idx_geometry_footprint ON dbo.TensorAtoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TensorAtoms_GeometryFootprint' 
    AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    CREATE SPATIAL INDEX IX_TensorAtoms_GeometryFootprint
    ON dbo.TensorAtoms (GeometryFootprint)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = HIGH,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    END
ELSE
BEGIN
    END;

-- ==========================================
-- Atoms.SpatialKey (Optional - if used)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_atom_spatial_key'
      AND object_id = OBJECT_ID('dbo.Atoms')
)
BEGIN
    DROP INDEX idx_atom_spatial_key ON dbo.Atoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Atoms_SpatialKey' 
    AND object_id = OBJECT_ID('dbo.Atoms')
)
BEGIN
    CREATE SPATIAL INDEX IX_Atoms_SpatialKey
    ON dbo.Atoms (SpatialKey)
    WITH (
        BOUNDING_BOX = (-10000, -10000, 10000, 10000),
        GRIDS = (
            LEVEL_1 = LOW,
            LEVEL_2 = LOW,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 8,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    END
ELSE
BEGIN
    END;

IF OBJECT_ID('dbo.TokenEmbeddingsGeo', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'idx_spatial_embedding'
          AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        DROP INDEX idx_spatial_embedding ON dbo.TokenEmbeddingsGeo;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_TokenEmbeddingsGeo_SpatialProjection' 
        AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        CREATE SPATIAL INDEX IX_TokenEmbeddingsGeo_SpatialProjection
        ON dbo.TokenEmbeddingsGeo(SpatialProjection)
        USING GEOMETRY_GRID
        WITH (
            BOUNDING_BOX = (-100, -100, 100, 100),
            GRIDS = (
                LEVEL_1 = HIGH,
                LEVEL_2 = HIGH,
                LEVEL_3 = MEDIUM,
                LEVEL_4 = LOW
            ),
            CELLS_PER_OBJECT = 16
        );
        
        END
    ELSE
    BEGIN
        END;
END
ELSE
BEGIN
    END;

-- ==========================================
-- CodeAtom.Embedding (AST structural search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CodeAtom_Embedding'
    AND object_id = OBJECT_ID('dbo.CodeAtom')
)
BEGIN
    CREATE SPATIAL INDEX IX_CodeAtom_Embedding
    ON dbo.CodeAtom (Embedding)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    END
ELSE
BEGIN
    END;

-- ==========================================
-- AudioData.Spectrogram (Audio waveform search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioData_Spectrogram'
    AND object_id = OBJECT_ID('dbo.AudioData')
)
BEGIN
    CREATE SPATIAL INDEX IX_AudioData_Spectrogram
    ON dbo.AudioData (Spectrogram)
    WITH (
        BOUNDING_BOX = (-500, -500, 500, 500),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 12,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    END
ELSE
BEGIN
    END;

-- ==========================================
-- VideoFrame.MotionVectors (Video motion analysis)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_VideoFrame_MotionVectors'
    AND object_id = OBJECT_ID('dbo.VideoFrame')
)
BEGIN
    CREATE SPATIAL INDEX IX_VideoFrame_MotionVectors
    ON dbo.VideoFrame (MotionVectors)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    END
ELSE
BEGIN
    END;

-- ==========================================
-- Image.ContentRegions (Image region search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Image_ContentRegions'
    AND object_id = OBJECT_ID('dbo.Image')
)
BEGIN
    CREATE SPATIAL INDEX IX_Image_ContentRegions
    ON dbo.Image (ContentRegions)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = HIGH,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    END
ELSE
BEGIN
    END;

-- ==========================================
-- VERIFY INDEX CREATION
-- ==========================================

SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    st.bounding_box_xmin,
    st.bounding_box_ymin,
    st.bounding_box_xmax,
    st.bounding_box_ymax,
    st.level_1_grid_desc,
    st.cells_per_object
FROM sys.indexes i
INNER JOIN sys.spatial_index_tessellations st ON i.object_id = st.object_id AND i.index_id = st.index_id
WHERE OBJECT_NAME(i.object_id) IN ('AtomEmbeddings', 'TensorAtoms', 'Atoms', 'TokenEmbeddingsGeo', 'CodeAtom', 'AudioData', 'VideoFrame', 'Image')
ORDER BY OBJECT_NAME(i.object_id), i.name;
