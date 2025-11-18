-- =============================================
-- sp_TemporalVectorSearch: Temporal table search with native VECTOR_DISTANCE
-- Replaces hard-coded SQL in SearchController temporal search (lines 422-462)
-- SQL Server 2025 native VECTOR_DISTANCE outperforms CLR for set-based operations
-- CLR is for per-row RBAR operations, T-SQL for set operations
-- =============================================
CREATE PROCEDURE dbo.sp_TemporalVectorSearch
    @QueryVector VECTOR(1998),
    @TopK INT = 10,
    @StartTime DATETIME2,
    @EndTime DATETIME2,
    @Modality VARCHAR(50) = NULL,
    @EmbeddingType VARCHAR(50) = NULL,
    @ModelId INT = NULL,
    @Dimension INT = 1998
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopK)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceUri,
        a.SourceType,
        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryVector) AS Similarity,
        a.CreatedAt,
        DATEDIFF(HOUR, a.CreatedAt, @EndTime) AS TemporalDistanceHours
    FROM dbo.AtomEmbedding FOR SYSTEM_TIME FROM @StartTime TO @EndTime ae
    INNER JOIN dbo.Atom FOR SYSTEM_TIME FROM @StartTime TO @EndTime a 
        ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND ae.Dimension = @Dimension
      AND (@Modality IS NULL OR a.Modality = @Modality)
      AND (@EmbeddingType IS NULL OR ae.EmbeddingType = @EmbeddingType)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
    ORDER BY Similarity DESC
    FOR JSON PATH;
END;
GO
