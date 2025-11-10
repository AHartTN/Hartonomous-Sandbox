-- Creates and repairs spatial indexes required by the platform. Runnable multiple times safely.

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

PRINT '============================================================';
PRINT 'CREATING SPATIAL INDEXES';
PRINT '============================================================';
GO

-- ==========================================
-- AtomEmbeddings.SpatialGeometry (Fine-grained)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_fine'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Dropping legacy idx_spatial_fine on AtomEmbeddings.SpatialGeometry...';
    DROP INDEX idx_spatial_fine ON dbo.AtomEmbeddings;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbeddings_SpatialGeometry' 
    AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Creating IX_AtomEmbeddings_SpatialGeometry on AtomEmbeddings.SpatialGeometry...';
    
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
    
    PRINT '  ✓ IX_AtomEmbeddings_SpatialGeometry created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_AtomEmbeddings_SpatialGeometry already exists';
END;
GO

-- ==========================================
-- AtomEmbeddings.SpatialCoarse (Fast filter)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_coarse'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Dropping legacy idx_spatial_coarse on AtomEmbeddings.SpatialCoarse...';
    DROP INDEX idx_spatial_coarse ON dbo.AtomEmbeddings;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbeddings_SpatialCoarse' 
    AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Creating IX_AtomEmbeddings_SpatialCoarse on AtomEmbeddings.SpatialCoarse...';
    
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
    
    PRINT '  ✓ IX_AtomEmbeddings_SpatialCoarse created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_AtomEmbeddings_SpatialCoarse already exists';
END;
GO

-- ==========================================
-- TensorAtoms.SpatialSignature (Weight signatures)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_signature'
      AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Dropping legacy idx_spatial_signature on TensorAtoms.SpatialSignature...';
    DROP INDEX idx_spatial_signature ON dbo.TensorAtoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TensorAtoms_SpatialSignature' 
    AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Creating IX_TensorAtoms_SpatialSignature on TensorAtoms.SpatialSignature...';
    
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
    
    PRINT '  ✓ IX_TensorAtoms_SpatialSignature created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_TensorAtoms_SpatialSignature already exists';
END;
GO

-- ==========================================
-- TensorAtoms.GeometryFootprint (Weight topology)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_geometry_footprint'
      AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Dropping legacy idx_geometry_footprint on TensorAtoms.GeometryFootprint...';
    DROP INDEX idx_geometry_footprint ON dbo.TensorAtoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TensorAtoms_GeometryFootprint' 
    AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Creating IX_TensorAtoms_GeometryFootprint on TensorAtoms.GeometryFootprint...';
    
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
    
    PRINT '  ✓ IX_TensorAtoms_GeometryFootprint created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_TensorAtoms_GeometryFootprint already exists';
END;
GO

-- ==========================================
-- Atoms.SpatialKey (Optional - if used)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_atom_spatial_key'
      AND object_id = OBJECT_ID('dbo.Atoms')
)
BEGIN
    PRINT 'Dropping legacy idx_atom_spatial_key on Atoms.SpatialKey...';
    DROP INDEX idx_atom_spatial_key ON dbo.Atoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Atoms_SpatialKey' 
    AND object_id = OBJECT_ID('dbo.Atoms')
)
BEGIN
    PRINT 'Creating IX_Atoms_SpatialKey on Atoms.SpatialKey...';
    
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
    
    PRINT '  ✓ IX_Atoms_SpatialKey created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_Atoms_SpatialKey already exists';
END;
GO

IF OBJECT_ID('dbo.TokenEmbeddingsGeo', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'idx_spatial_embedding'
          AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        PRINT 'Dropping legacy idx_spatial_embedding on TokenEmbeddingsGeo.SpatialProjection...';
        DROP INDEX idx_spatial_embedding ON dbo.TokenEmbeddingsGeo;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_TokenEmbeddingsGeo_SpatialProjection' 
        AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        PRINT 'Creating IX_TokenEmbeddingsGeo_SpatialProjection on TokenEmbeddingsGeo.SpatialProjection...';
        
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
        
        PRINT '  ✓ IX_TokenEmbeddingsGeo_SpatialProjection created';
    END
    ELSE
    BEGIN
        PRINT '  ✓ IX_TokenEmbeddingsGeo_SpatialProjection already exists';
    END;
END
ELSE
BEGIN
    PRINT '  WARNING: TokenEmbeddingsGeo table not found; skipping spatial projection index.';
END;
GO

-- ==========================================
-- CodeAtom.Embedding (AST structural search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CodeAtom_Embedding'
    AND object_id = OBJECT_ID('dbo.CodeAtom')
)
BEGIN
    PRINT 'Creating IX_CodeAtom_Embedding on CodeAtom.Embedding...';

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

    PRINT '  ✓ IX_CodeAtom_Embedding created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_CodeAtom_Embedding already exists';
END;
GO

-- ==========================================
-- AudioData.Spectrogram (Audio waveform search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioData_Spectrogram'
    AND object_id = OBJECT_ID('dbo.AudioData')
)
BEGIN
    PRINT 'Creating IX_AudioData_Spectrogram on AudioData.Spectrogram...';

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

    PRINT '  ✓ IX_AudioData_Spectrogram created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_AudioData_Spectrogram already exists';
END;
GO

-- ==========================================
-- VideoFrame.MotionVectors (Video motion analysis)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_VideoFrame_MotionVectors'
    AND object_id = OBJECT_ID('dbo.VideoFrame')
)
BEGIN
    PRINT 'Creating IX_VideoFrame_MotionVectors on VideoFrame.MotionVectors...';

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

    PRINT '  ✓ IX_VideoFrame_MotionVectors created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_VideoFrame_MotionVectors already exists';
END;
GO

-- ==========================================
-- Image.ContentRegions (Image region search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Image_ContentRegions'
    AND object_id = OBJECT_ID('dbo.Image')
)
BEGIN
    PRINT 'Creating IX_Image_ContentRegions on Image.ContentRegions...';

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

    PRINT '  ✓ IX_Image_ContentRegions created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_Image_ContentRegions already exists';
END;
GO

-- ==========================================
-- VERIFY INDEX CREATION
-- ==========================================
PRINT '';
PRINT 'Spatial Index Summary:';
GO

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
GO

PRINT '';
PRINT '============================================================';
PRINT 'SPATIAL INDEXES CREATED';
PRINT '============================================================';
GO
