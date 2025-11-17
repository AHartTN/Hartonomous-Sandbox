CREATE NONCLUSTERED INDEX [IX_AtomEmbeddingSpatialMetadata_BucketXYZ]
    ON [dbo].[AtomEmbeddingSpatialMetadata]([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ])
    WHERE [SpatialBucketX] IS NOT NULL;
