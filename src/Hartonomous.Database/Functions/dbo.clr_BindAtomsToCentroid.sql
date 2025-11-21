-- =============================================
-- CLR Function: fn_BindAtomsToCentroid (TVF)
-- Description: Binds atoms to a concept centroid
-- =============================================
CREATE FUNCTION [dbo].[fn_BindAtomsToCentroid]
(
    @conceptCentroid VARBINARY(MAX),
    @similarityThreshold FLOAT,
    @tenantId INT
)
RETURNS TABLE
(
    AtomId BIGINT,
    Similarity FLOAT,
    AtomModality NVARCHAR(50)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ConceptDiscovery].[fn_BindAtomsToCentroid]
GO
