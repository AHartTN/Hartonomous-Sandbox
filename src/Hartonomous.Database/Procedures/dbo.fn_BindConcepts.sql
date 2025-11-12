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