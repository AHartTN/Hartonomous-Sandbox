-- =============================================
-- CLR Function: clr_VectorNorm
-- Description: Computes the L2 norm (magnitude) of a vector
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorNorm]
(
    @vector VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorNorm]
GO
