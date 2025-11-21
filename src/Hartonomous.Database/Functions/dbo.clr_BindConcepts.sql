-- =============================================
-- CLR Function: fn_BindConcepts (TVF)
-- Description: Binds concepts to an atom based on similarity
-- =============================================
CREATE FUNCTION [dbo].[fn_BindConcepts]
(
    @atomId BIGINT,
    @similarityThreshold FLOAT,
    @maxConceptsPerAtom INT,
    @tenantId INT
)
RETURNS TABLE
(
    ConceptId INT,
    Similarity FLOAT,
    ConceptName NVARCHAR(256)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ConceptDiscovery].[fn_BindConcepts]
GO
