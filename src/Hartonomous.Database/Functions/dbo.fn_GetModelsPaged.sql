-- =============================================
-- fn_GetModelsPaged: Inline TVF for paged model queries
-- Replaces hard-coded SQL with paging in ModelsController
-- INLINE TVFs get full query optimizer benefits (unlike multi-statement TVFs)
-- Query optimizer can push predicates, optimize joins, use indexes
-- =============================================
CREATE FUNCTION dbo.fn_GetModelsPaged(
    @Offset INT,
    @PageSize INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        ModelId,
        ModelName,
        ModelType,
        ParameterCount,
        IngestionDate,
        LayerCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.vw_ModelsSummary
    ORDER BY IngestionDate DESC, ModelName
    OFFSET @Offset ROWS 
    FETCH NEXT @PageSize ROWS ONLY
);
GO
