-- Auto-split from dbo.FullTextSearch.sql
-- Object: PROCEDURE dbo.sp_SemanticSimilarity

CREATE PROCEDURE dbo.sp_SemanticSimilarity
    @SourceAtomId BIGINT,
    @TopK INT = 10,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @SourceEmbedding VECTOR(1998);
        
        -- Get source embedding
        SELECT @SourceEmbedding = EmbeddingVector
        FROM dbo.AtomEmbeddings
        WHERE AtomId = @SourceAtomId AND TenantId = @TenantId;
        
        IF @SourceEmbedding IS NULL
        BEGIN
            RAISERROR('Source atom has no embedding vector', 16, 1);
            RETURN -1;
        END
        
        -- Find similar atoms using vector cosine similarity
        SELECT TOP (@TopK)
            ae.AtomId AS SimilarAtomId,
            (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @SourceEmbedding)) * 100.0 AS SimilarityScore,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc
        FROM dbo.AtomEmbeddings ae
        INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId AND ae.TenantId = a.TenantId
        WHERE ae.TenantId = @TenantId
              AND ae.AtomId != @SourceAtomId
              AND ae.EmbeddingVector IS NOT NULL
              AND a.IsDeleted = 0
        ORDER BY VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @SourceEmbedding) ASC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO

-- sp_ExtractKeyPhrases: Extract key phrases from document
-- Uses semantic search key phrase extraction


GO
