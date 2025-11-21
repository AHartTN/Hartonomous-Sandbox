-- =============================================
-- CLR Function: clr_ComputeMortonValue
-- Description: Computes Morton (Z-order) curve value from spatial key
-- =============================================
CREATE FUNCTION [dbo].[clr_ComputeMortonValue]
(
    @spatialKey GEOMETRY,
    @precision INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.HilbertCurve].[clr_ComputeMortonValue]
GO
