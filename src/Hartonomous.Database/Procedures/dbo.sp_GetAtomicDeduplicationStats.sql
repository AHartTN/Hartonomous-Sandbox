-- =============================================
-- Get deduplication statistics
-- Analyzes deduplication efficiency across atomic vectors
-- =============================================
CREATE PROCEDURE dbo.sp_GetAtomicDeduplicationStats
    @RelationType NVARCHAR(128) = 'embedding_dimension'
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    -- Overall statistics
    SELECT 
        COUNT(DISTINCT ar.SourceAtomId) AS TotalVectors,
        COUNT(*) AS TotalRelations,
        COUNT(DISTINCT ar.TargetAtomId) AS UniqueAtoms,
        CAST(COUNT(*) AS FLOAT) / NULLIF(COUNT(DISTINCT ar.TargetAtomId), 0) AS AvgReuse,
        (1.0 - CAST(COUNT(DISTINCT ar.TargetAtomId) AS FLOAT) / NULLIF(COUNT(*), 0)) * 100 AS DeduplicationPct
    FROM dbo.AtomRelations ar
    WHERE ar.RelationType = @RelationType;
    
    -- Top deduplicated values
    SELECT TOP 20
        a.AtomId,
        a.CanonicalText AS FloatValue,
        a.ReferenceCount,
        COUNT(DISTINCT ar.SourceAtomId) AS VectorCount
    FROM dbo.Atoms a
    INNER JOIN dbo.AtomRelations ar ON ar.TargetAtomId = a.AtomId
    WHERE ar.RelationType = @RelationType
    GROUP BY a.AtomId, a.CanonicalText, a.ReferenceCount
    ORDER BY a.ReferenceCount DESC;
    
    RETURN 0;
END
