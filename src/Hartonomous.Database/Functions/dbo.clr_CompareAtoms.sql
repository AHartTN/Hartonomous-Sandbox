-- =============================================
-- CLR Function: fn_CompareAtoms
-- Description: Computes similarity between two atoms
-- =============================================
CREATE FUNCTION [dbo].[fn_CompareAtoms]
(
    @atomId1 BIGINT,
    @atomId2 BIGINT,
    @tenantId INT
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.EmbeddingFunctions].[fn_CompareAtoms]
GO
