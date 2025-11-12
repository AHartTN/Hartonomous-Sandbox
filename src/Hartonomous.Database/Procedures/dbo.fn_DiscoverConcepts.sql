CREATE FUNCTION dbo.fn_DiscoverConcepts(
    @MinClusterSize INT,
    @CoherenceThreshold FLOAT,
    @MaxConcepts INT,
    @TenantId INT
)
RETURNS TABLE (
    ConceptId UNIQUEIDENTIFIER,
    Centroid VARBINARY(MAX),
    AtomCount INT,
    Coherence FLOAT,
    SpatialBucket INT
)
AS EXTERNAL NAME SqlClrFunctions.[Hartonomous.SqlClr.ConceptDiscovery].fn_DiscoverConcepts;