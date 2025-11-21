-- =============================================
-- CLR Function: clr_VectorCosineSimilarity
-- Description: Computes cosine similarity between two vectors
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorCosineSimilarity]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorCosineSimilarity]
GO
