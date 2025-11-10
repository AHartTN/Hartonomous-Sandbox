-- fn_BindConcepts: CLR wrapper for concept binding
-- Associates atoms with discovered concepts based on embedding similarity
-- Returns table of AtomId, ConceptId, Similarity, IsPrimary

IF OBJECT_ID('dbo.fn_BindConcepts', 'TF') IS NOT NULL DROP FUNCTION dbo.fn_BindConcepts;
GO
CREATE FUNCTION dbo.fn_BindConcepts(
    @AtomId BIGINT,
    @SimilarityThreshold FLOAT,
    @MaxConceptsPerAtom INT,
    @TenantId INT
)
RETURNS TABLE (
    AtomId BIGINT,
    ConceptId UNIQUEIDENTIFIER,
    Similarity FLOAT,
    IsPrimary BIT
)
AS EXTERNAL NAME SqlClrFunctions.[Hartonomous.SqlClr.ConceptBinding].fn_BindConcepts;
GO