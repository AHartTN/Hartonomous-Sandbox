-- =============================================
-- CLR Function: clr_VectorArgMax
-- Description: Returns the index of the maximum element in a vector
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorArgMax]
(
    @vector VARBINARY(MAX)
)
RETURNS INT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorArgMax]
GO
