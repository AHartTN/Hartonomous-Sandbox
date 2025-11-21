-- =============================================
-- CLR Function: fn_ComputeEmbedding
-- Description: Computes embedding for an atom using a model
-- =============================================
CREATE FUNCTION [dbo].[fn_ComputeEmbedding]
(
    @atomId BIGINT,
    @modelId INT,
    @tenantId INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.EmbeddingFunctions].[fn_ComputeEmbedding]
GO
