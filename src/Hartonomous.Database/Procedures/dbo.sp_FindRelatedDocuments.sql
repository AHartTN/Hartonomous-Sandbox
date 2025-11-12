CREATE PROCEDURE dbo.sp_FindRelatedDocuments
    @AtomId BIGINT,
    @TopK INT = 10,
    @TenantId INT = NULL, -- Optional tenant filtering
    @IncludeSemanticText BIT = 1,
    @IncludeVectorSimilarity BIT = 1,
    @IncludeGraphNeighbors BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @Results TABLE (
            RelatedAtomId BIGINT,
            SemanticScore FLOAT,
            VectorScore FLOAT,
            GraphScore FLOAT,
            CombinedScore FLOAT
        );
        
        -- 1. Semantic text similarity
        IF @IncludeSemanticText = 1
        BEGIN
            INSERT INTO @Results (RelatedAtomId, SemanticScore, VectorScore, GraphScore)
            SELECT 
                sst.matched_document_key,
                sst.score / 100.0, -- Normalize to 0-1
                0.0,
                0.0
            FROM SEMANTICSIMILARITYTABLE(dbo.Atoms, Content, @AtomId) sst;
        END
        
        -- 2. Vector embedding similarity
        IF @IncludeVectorSimilarity = 1
        BEGIN
            DECLARE @QueryEmbedding VARBINARY(MAX);
            
            -- Get query embedding without tenant filter (AtomEmbeddings has no TenantId)
            SELECT @QueryEmbedding = EmbeddingVector
            FROM dbo.AtomEmbeddings
            WHERE AtomId = @AtomId;
            
            IF @QueryEmbedding IS NOT NULL
            BEGIN
                MERGE @Results AS target
                USING (
                    SELECT 
                        ae.AtomId,
                        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryEmbedding) AS VectorScore
                    FROM dbo.AtomEmbeddings ae
                    LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
                    WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
                          AND ae.AtomId != @AtomId
                ) AS source
                ON target.RelatedAtomId = source.AtomId
                WHEN MATCHED THEN
                    UPDATE SET VectorScore = source.VectorScore
                WHEN NOT MATCHED THEN
                    INSERT (RelatedAtomId, SemanticScore, VectorScore, GraphScore)
                    VALUES (source.AtomId, 0.0, source.VectorScore, 0.0);
            END
        END
        
        -- 3. Graph neighbors (1-hop)
        IF @IncludeGraphNeighbors = 1
        BEGIN
            MERGE @Results AS target
            USING (
                SELECT DISTINCT edge.$to_id AS AtomId, 0.8 AS GraphScore
                FROM provenance.AtomGraphEdges edge
                WHERE edge.$from_id = @AtomId
                UNION
                SELECT DISTINCT edge.$from_id AS AtomId, 0.8 AS GraphScore
                FROM provenance.AtomGraphEdges edge
                WHERE edge.$to_id = @AtomId
            ) AS source
            ON target.RelatedAtomId = source.AtomId
            WHEN MATCHED THEN
                UPDATE SET GraphScore = source.GraphScore
            WHEN NOT MATCHED THEN
                INSERT (RelatedAtomId, SemanticScore, VectorScore, GraphScore)
                VALUES (source.AtomId, 0.0, 0.0, source.GraphScore);
        END
        
        -- Compute combined score (equal weighting)
        UPDATE @Results
        SET CombinedScore = (SemanticScore + VectorScore + GraphScore) / 3.0;
        
        -- Return top K results
        SELECT TOP (@TopK)
            r.RelatedAtomId AS AtomId,
            r.SemanticScore,
            r.VectorScore,
            r.GraphScore,
            r.CombinedScore,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc
        FROM @Results r
        INNER JOIN dbo.Atoms a ON r.RelatedAtomId = a.AtomId
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
        ORDER BY r.CombinedScore DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;