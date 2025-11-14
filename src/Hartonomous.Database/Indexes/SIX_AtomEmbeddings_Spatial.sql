CREATE SPATIAL INDEX [IX_AtomEmbeddings_Spatial]
    ON [dbo].[AtomEmbeddings]([SpatialGeometry])
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM)
    );
