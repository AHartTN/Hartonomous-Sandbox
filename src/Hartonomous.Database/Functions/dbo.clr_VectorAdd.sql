-- =============================================
-- CLR Function: clr_VectorAdd
-- Description: Adds two vectors element-wise
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorAdd]
(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorAdd]
GO
