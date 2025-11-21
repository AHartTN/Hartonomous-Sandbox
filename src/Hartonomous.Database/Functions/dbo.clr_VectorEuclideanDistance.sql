-- =============================================
-- CLR Function: clr_VectorEuclideanDistance
-- Description: Computes Euclidean distance between two vectors
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorEuclideanDistance]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorEuclideanDistance]
GO
