-- =============================================
-- Spatial similarity search with atomic reconstruction
-- Complexity: O(log n) + O(k) via spatial bucket indexing
-- =============================================
CREATE PROCEDURE dbo.sp_AtomicSpatialSearch
    @QueryX FLOAT,
    @QueryY FLOAT,
    @QueryZ FLOAT,
    @Radius FLOAT = 0.1,
    @TopK INT = 10,
    @RelationType NVARCHAR(128) = 'embedding_dimension'
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;  -- Performance optimization
    
    -- Step 1: Spatial bucket coarse filter (O(1))
    DECLARE @TargetBucket BIGINT = dbo.fn_ComputeSpatialBucket(@QueryX, @QueryY, @QueryZ);
    
    -- Step 2: Find candidate atoms via trilateration (O(log n))
    DECLARE @Candidates TABLE (
        SourceAtomId BIGINT NOT NULL,
        Distance FLOAT NOT NULL,
        PRIMARY KEY (SourceAtomId)
    );
    
    INSERT INTO @Candidates (SourceAtomId, Distance)
    SELECT DISTINCT TOP (@TopK * 10)  -- Over-sample for refinement
        ar.SourceAtomId,
        SQRT(
            POWER(ar.CoordX - @QueryX, 2) + 
            POWER(ar.CoordY - @QueryY, 2) + 
            POWER(ar.CoordZ - @QueryZ, 2)
        ) AS Distance
    FROM dbo.AtomRelations ar WITH (INDEX(IX_AtomRelations_SpatialBucket))
    WHERE 
        ar.SpatialBucket = @TargetBucket
        AND ar.RelationType = @RelationType
        AND ar.CoordX BETWEEN @QueryX - @Radius AND @QueryX + @Radius
        AND ar.CoordY BETWEEN @QueryY - @Radius AND @QueryY + @Radius
        AND ar.CoordZ BETWEEN @QueryZ - @Radius AND @QueryZ + @Radius
    ORDER BY 
        SQRT(
            POWER(ar.CoordX - @QueryX, 2) + 
            POWER(ar.CoordY - @QueryY, 2) + 
            POWER(ar.CoordZ - @QueryZ, 2)
        );
    
    -- Step 3: Return top K results with metadata (O(k))
    SELECT TOP (@TopK)
        c.SourceAtomId,
        a.ContentHash,
        a.Modality,
        a.Subtype,
        a.CanonicalText,
        a.ReferenceCount,
        c.Distance,
        a.CreatedAt
    FROM @Candidates c
    INNER JOIN dbo.Atoms a ON a.AtomId = c.SourceAtomId
    ORDER BY c.Distance;
    
    RETURN 0;
END
