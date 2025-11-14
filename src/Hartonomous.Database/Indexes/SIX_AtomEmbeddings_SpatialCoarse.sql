CREATE SPATIAL INDEX [IX_AtomEmbeddings_Coarse]
    ON [dbo].[AtomEmbeddings]([SpatialCoarse])
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (LEVEL_1 = LOW, LEVEL_2 = LOW, LEVEL_3 = LOW, LEVEL_4 = LOW)
    );
