-- =============================================
-- CLR Function: fn_DiscoverConcepts (TVF)
-- Description: Discovers concept clusters from atom embeddings
-- =============================================
CREATE FUNCTION [dbo].[fn_DiscoverConcepts]
(
    @minClusterSize INT,
    @coherenceThreshold FLOAT,
    @maxConcepts INT,
    @tenantId INT
)
RETURNS TABLE
(
    ConceptId INT,
    CentroidVector VARBINARY(MAX),
    ClusterSize INT,
    CoherenceScore FLOAT,
    TopTerms NVARCHAR(MAX)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ConceptDiscovery].[fn_DiscoverConcepts]
GO
