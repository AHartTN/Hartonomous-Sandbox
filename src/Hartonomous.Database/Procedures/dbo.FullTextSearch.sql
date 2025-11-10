-- sp_KeywordSearch: Full-text search with ranking
-- Uses CONTAINSTABLE for relevance scoring

CREATE OR ALTER PROCEDURE dbo.sp_KeywordSearch
    @Keywords NVARCHAR(MAX),
    @TopK INT = 10,
    @TenantId INT = 0,
    @ContentTypeFilter NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT TOP (@TopK)
            a.AtomId,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc,
            fts.RANK AS RelevanceScore,
            CAST(a.Content AS NVARCHAR(MAX)) AS ContentPreview
        FROM CONTAINSTABLE(dbo.Atoms, Content, @Keywords) fts
        INNER JOIN dbo.Atoms a ON fts.[KEY] = a.AtomId
        WHERE a.TenantId = @TenantId
              AND a.IsDeleted = 0
              AND (@ContentTypeFilter IS NULL OR a.ContentType = @ContentTypeFilter)
        ORDER BY fts.RANK DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- sp_SemanticSimilarity: Document similarity using semantic search
-- Finds documents similar to a given document

CREATE OR ALTER PROCEDURE dbo.sp_SemanticSimilarity
    @SourceAtomId BIGINT,
    @TopK INT = 10,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Verify full-text and semantic search is enabled
        IF NOT EXISTS (
            SELECT 1 
            FROM sys.fulltext_indexes fti
            INNER JOIN sys.objects o ON fti.object_id = o.object_id
            WHERE o.name = 'Atoms'
        )
        BEGIN
            RAISERROR('Full-text index not found on Atoms table', 16, 1);
            RETURN -1;
        END
        
        -- Find similar documents
        SELECT TOP (@TopK)
            sst.matched_document_key AS SimilarAtomId,
            sst.score AS SimilarityScore,
            a.ContentHash,
            a.ContentType,
            a.CreatedUtc
        FROM SEMANTICSIMILARITYTABLE(dbo.Atoms, Content, @SourceAtomId) sst
        INNER JOIN dbo.Atoms a ON sst.matched_document_key = a.AtomId
        WHERE a.TenantId = @TenantId
              AND a.IsDeleted = 0
        ORDER BY sst.score DESC;
        
        RETURN 0;
    END TRY
    BEGIN CATCH

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- sp_ExtractKeyPhrases: Extract key phrases from document
-- Uses semantic search key phrase extraction

CREATE OR ALTER PROCEDURE dbo.sp_ExtractKeyPhrases
    @AtomId BIGINT,
    @TopK INT = 20,
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT TOP (@TopK)
            keyphrase,
            score
        FROM SEMANTICKEYPHRASETABLE(dbo.Atoms, Content, @AtomId)
        ORDER BY score DESC
        FOR JSON PATH;
        
        RETURN 0;
    END TRY
    BEGIN CATCH

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;

-- sp_FindRelatedDocuments: Multi-signal document discovery
-- Combines FTS + vector + graph for comprehensive results

CREATE OR ALTER PROCEDURE dbo.sp_FindRelatedDocuments
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
        
