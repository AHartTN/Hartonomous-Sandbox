CREATE SPATIAL INDEX [IX_AtomEmbeddings_Spatial]
    ON [dbo].[AtomEmbeddings]([SpatialGeometry])
    WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
