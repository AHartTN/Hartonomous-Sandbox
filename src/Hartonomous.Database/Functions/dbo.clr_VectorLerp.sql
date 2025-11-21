-- =============================================
-- CLR Function: clr_VectorLerp
-- Description: Linear interpolation between two vectors
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorLerp]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX),
    @t FLOAT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorLerp]
GO
