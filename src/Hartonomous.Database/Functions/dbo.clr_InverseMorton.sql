-- =============================================
-- CLR Function: clr_InverseMorton
-- Description: Converts Morton value back to spatial geometry
-- =============================================
CREATE FUNCTION [dbo].[clr_InverseMorton]
(
    @mortonValue BIGINT,
    @precision INT
)
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.HilbertCurve].[clr_InverseMorton]
GO
