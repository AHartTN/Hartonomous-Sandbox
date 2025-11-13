CREATE SPATIAL INDEX [IX_AtomEmbeddings_Coarse]
    ON [dbo].[AtomEmbeddings]([SpatialCoarse])
    WITH (GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW));
