CREATE PROCEDURE dbo.sp_UpdateAtomEmbeddingSpatialMetadata
    @embedding_id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NoZBucket INT = -2147483648;
    DECLARE
        @bucketX INT,
        @bucketY INT,
        @bucketZ INT,
        @hasSpatial BIT,
        @hasZ BIT,
        @projZ FLOAT,
        @bucketKeyZ INT;

    SELECT
        @bucketX = SpatialBucketX,
        @bucketY = SpatialBucketY,
        @bucketZ = SpatialBucketZ,
        @projZ = SpatialProjZ,
        @hasSpatial = CASE WHEN SpatialBucketX IS NOT NULL AND SpatialBucketY IS NOT NULL THEN 1 ELSE 0 END
    FROM dbo.AtomEmbeddings
    WHERE AtomEmbeddingId = @embedding_id;

    IF @@ROWCOUNT = 0
        RETURN;

    IF @hasSpatial = 0
        RETURN;

    SET @hasZ = CASE WHEN @projZ IS NOT NULL THEN 1 ELSE 0 END;
    SET @bucketKeyZ = CASE WHEN @hasZ = 1 THEN ISNULL(@bucketZ, 0) ELSE @NoZBucket END;

    ;WITH BucketAgg AS
    (
        SELECT
            SpatialBucketX,
            SpatialBucketY,
            CASE WHEN SpatialProjZ IS NOT NULL THEN ISNULL(SpatialBucketZ, 0) ELSE @NoZBucket END AS SpatialBucketZ,
            CASE WHEN SpatialProjZ IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasZ,
            COUNT_BIG(*) AS EmbeddingCount,
            MIN(SpatialProjX) AS MinProjX,
            MAX(SpatialProjX) AS MaxProjX,
            MIN(SpatialProjY) AS MinProjY,
            MAX(SpatialProjY) AS MaxProjY,
            MIN(SpatialProjZ) AS MinProjZ,
            MAX(SpatialProjZ) AS MaxProjZ
        FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialBucket))
        WHERE SpatialBucketX = @bucketX
          AND SpatialBucketY = @bucketY
          AND CASE WHEN SpatialProjZ IS NOT NULL THEN ISNULL(SpatialBucketZ, 0) ELSE @NoZBucket END = @bucketKeyZ
        GROUP BY SpatialBucketX, SpatialBucketY, CASE WHEN SpatialProjZ IS NOT NULL THEN ISNULL(SpatialBucketZ, 0) ELSE @NoZBucket END, CASE WHEN SpatialProjZ IS NOT NULL THEN 1 ELSE 0 END, SpatialProjZ
    )
    MERGE dbo.AtomEmbeddingSpatialMetadata AS target
    USING BucketAgg AS source
        ON target.SpatialBucketX = source.SpatialBucketX
       AND target.SpatialBucketY = source.SpatialBucketY
       AND target.SpatialBucketZ = source.SpatialBucketZ
       AND target.HasZ = source.HasZ
    WHEN MATCHED THEN
        UPDATE SET
            EmbeddingCount = source.EmbeddingCount,
            MinProjX = source.MinProjX,
            MaxProjX = source.MaxProjX,
            MinProjY = source.MinProjY,
            MaxProjY = source.MaxProjY,
            MinProjZ = source.MinProjZ,
            MaxProjZ = source.MaxProjZ,
            UpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (
            SpatialBucketX,
            SpatialBucketY,
            SpatialBucketZ,
            HasZ,
            EmbeddingCount,
            MinProjX,
            MaxProjX,
            MinProjY,
            MaxProjY,
            MinProjZ,
            MaxProjZ,
            UpdatedAt)
        VALUES (
            source.SpatialBucketX,
            source.SpatialBucketY,
            source.SpatialBucketZ,
            source.HasZ,
            source.EmbeddingCount,
            source.MinProjX,
            source.MaxProjX,
            source.MinProjY,
            source.MaxProjY,
            source.MinProjZ,
            source.MaxProjZ,
            SYSUTCDATETIME())
    WHEN NOT MATCHED BY SOURCE
         AND target.SpatialBucketX = @bucketX
         AND target.SpatialBucketY = @bucketY
         AND target.SpatialBucketZ = @bucketKeyZ
         AND target.HasZ = @hasZ
        THEN DELETE;
END;
GO
