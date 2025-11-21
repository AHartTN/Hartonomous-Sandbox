-- =============================================
-- CLR Function: clr_VectorNormalize
-- Description: Normalizes a vector to unit length
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorNormalize]
(
    @vector VARBINARY(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorNormalize]
GO
