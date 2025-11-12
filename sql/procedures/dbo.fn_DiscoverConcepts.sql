-- fn_DiscoverConcepts: CLR wrapper for concept discovery
-- Uses DBSCAN clustering on spatial buckets to find emergent concepts
-- Returns table of ConceptId, Centroid, AtomCount, Coherence, SpatialBucket

IF OBJECT_ID('dbo.fn_DiscoverConcepts', 'TF') IS NOT NULL DROP FUNCTION dbo.fn_DiscoverConcepts;
GO
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
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ConceptDiscovery].fn_DiscoverConcepts;
GO