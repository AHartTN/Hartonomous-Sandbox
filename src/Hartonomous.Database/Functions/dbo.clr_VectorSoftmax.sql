-- =============================================
-- CLR Function: clr_VectorSoftmax
-- Description: Applies softmax transformation to a vector
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorSoftmax]
(
    @vector VARBINARY(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorSoftmax]
GO
