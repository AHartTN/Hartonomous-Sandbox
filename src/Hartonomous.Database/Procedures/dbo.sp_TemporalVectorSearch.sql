-- =============================================
-- sp_TemporalVectorSearch: Temporal table search with native VECTOR_DISTANCE
-- Replaces hard-coded SQL in SearchController temporal search (lines 422-462)
-- SQL Server 2025 native VECTOR_DISTANCE outperforms CLR for set-based operations
-- CLR is for per-row RBAR operations, T-SQL for set operations
-- PHASE 7.2: Added TenantId for security
-- =============================================
CREATE PROCEDURE dbo.sp_TemporalVectorSearch
    @QueryVector VECTOR(1998),
    @TopK INT = 10,
    @StartTime DATETIME2,
    @EndTime DATETIME2,
    @Modality VARCHAR(50) = NULL,
    @EmbeddingType VARCHAR(50) = NULL,
    @ModelId INT = NULL,
    @Dimension INT = 1536,
    @TenantId INT = 0  -- PHASE 7.2: Multi-tenancy
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopK)
        ae.AtomEmbeddingId,
        ae.AtomId,
        a.Modality,
        a.Subtype,
        a.SourceUri,
        a.CanonicalText,
        1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryVector) AS Similarity,
        a.CreatedAt,
        DATEDIFF(HOUR, a.CreatedAt, @EndTime) AS TemporalDistanceHours
    FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom FOR SYSTEM_TIME FROM @StartTime TO @EndTime a 
        ON a.AtomId = ae.AtomId
    WHERE ae.EmbeddingVector IS NOT NULL
      AND ae.Dimension = @Dimension
      AND ae.CreatedAt BETWEEN @StartTime AND @EndTime
      AND (@Modality IS NULL OR a.Modality = @Modality)
      AND (@EmbeddingType IS NULL OR ae.EmbeddingType = @EmbeddingType)
      AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
      -- PHASE 7.2: Multi-tenancy filter
      AND (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
    ORDER BY Similarity DESC
    FOR JSON PATH;
END;
GO
