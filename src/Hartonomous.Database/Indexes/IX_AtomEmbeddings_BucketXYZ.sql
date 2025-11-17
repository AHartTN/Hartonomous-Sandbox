CREATE NONCLUSTERED INDEX [IX_AtomEmbeddingSpatialMetadata_BucketXYZ]
    ON [dbo].[AtomEmbeddingSpatialMetadatum]([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ])
    WHERE [SpatialBucketX] IS NOT NULL;
