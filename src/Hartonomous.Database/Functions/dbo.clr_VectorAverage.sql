-- =============================================
-- CLR Function: clr_VectorAverage
-- Description: Computes the average of multiple vectors (aggregate)
-- Note: This is used by sp_RunInference
-- =============================================
CREATE FUNCTION [dbo].[clr_VectorAverage]
(
    @vector VARBINARY(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorOperations].[VectorAverage]
GO
