-- Auto-split from dbo.FullTextSearch.sql
-- Object: PROCEDURE dbo.sp_FindRelatedDocuments

CREATE PROCEDURE dbo.sp_FindRelatedDocuments
    @AtomId BIGINT,
    @TopK INT = 10,
    @TenantId INT = 0,
    @IncludeSemanticText BIT = 1,
    @IncludeVectorSimilarity BIT = 1,
    @IncludeGraphNeighbors BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @Results TABLE (
            RelatedAtomId BIGINT,
            VectorScore FLOAT,
            GraphScore FLOAT,
            CombinedScore FLOAT
        );
        
        -- 1. Vector embedding similarity (replaces old SEMANTICSIMILARITYTABLE)
        IF @IncludeVectorSimilarity = 1
        BEGIN
            DECLARE @QueryEmbedding VECTOR(1998);
            
            SELECT @QueryEmbedding = SpatialKey
            FROM dbo.AtomEmbeddings
            WHERE AtomId = @AtomId AND TenantId = @TenantId;
            
            IF @QueryEmbedding IS NOT NULL
            BEGIN
                INSERT INTO @Results (RelatedAtomId, VectorScore, GraphScore)
                SELECT 
                    ae.AtomId,
                    1.0 - VECTOR_DISTANCE('cosine', ae.SpatialKey, @QueryEmbedding) AS VectorScore,
                    0.0
                FROM dbo.AtomEmbeddings ae
                WHERE ae.TenantId = @TenantId
                      AND ae.AtomId != @AtomId
                      AND ae.SpatialKey IS NOT NULL;
            END
        END
        
        -- 2. Graph neighbors (1-hop)
        IF @IncludeGraphNeighbors = 1
        BEGIN
            MERGE @Results AS target
            USING (
                SELECT DISTINCT edge.ToAtomId AS AtomId, 0.8 AS GraphScore
                FROM provenance.AtomGraphEdges edge
                WHERE edge.FromAtomId = @AtomId
                UNION
                SELECT DISTINCT edge.FromAtomId AS AtomId, 0.8 AS GraphScore
                FROM provenance.AtomGraphEdges edge
                WHERE edge.ToAtomId = @AtomId
            ) AS source
            ON target.RelatedAtomId = source.AtomId
            WHEN MATCHED THEN
                UPDATE SET GraphScore = source.GraphScore
            WHEN NOT MATCHED THEN
                INSERT (RelatedAtomId, VectorScore, GraphScore)
                VALUES (source.AtomId, 0.0, source.GraphScore);
        END
        
        -- Compute combined score (equal weighting)
        UPDATE @Results
        SET CombinedScore = (VectorScore + GraphScore) / 2.0;
        
        -- Return top K results
        SELECT TOP (@TopK)
            r.RelatedAtomId AS AtomId,
            r.VectorScore,
            r.GraphScore,
            r.CombinedScore,
            a.ContentHash,
            a.ContentType,
            a.CreatedAt
        FROM @Results r
        INNER JOIN dbo.Atoms a ON r.RelatedAtomId = a.AtomId
        WHERE a.TenantId = @TenantId
        ORDER BY r.CombinedScore DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
