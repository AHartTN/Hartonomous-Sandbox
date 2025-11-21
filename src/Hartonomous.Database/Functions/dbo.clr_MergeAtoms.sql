-- =============================================
-- CLR Function: fn_MergeAtoms
-- Description: Merges a duplicate atom into a primary atom
-- =============================================
CREATE FUNCTION [dbo].[fn_MergeAtoms]
(
    @primaryAtomId BIGINT,
    @duplicateAtomId BIGINT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.EmbeddingFunctions].[fn_MergeAtoms]
GO
