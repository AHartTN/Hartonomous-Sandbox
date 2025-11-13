CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_BucketXYZ]
    ON [dbo].[AtomEmbeddings]([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ])
    WHERE [SpatialBucketX] IS NOT NULL;
