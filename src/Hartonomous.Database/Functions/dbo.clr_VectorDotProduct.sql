-- =============================================
-- CLR Function: clr_VectorDotProduct
-- Description: Computes the dot product of two vectors
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorDotProduct]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorDotProduct]
GO
