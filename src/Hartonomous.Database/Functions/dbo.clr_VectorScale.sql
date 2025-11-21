-- =============================================
-- CLR Function: clr_VectorScale
-- Description: Scales a vector by a scalar value
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorScale]
(
    @vector VARBINARY(MAX),
    @scalar FLOAT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorScale]
GO
