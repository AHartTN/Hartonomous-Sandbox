-- =============================================
-- CLR Function: clr_VectorSubtract
-- Description: Subtracts two vectors element-wise
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorSubtract]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorSubtract]
GO
