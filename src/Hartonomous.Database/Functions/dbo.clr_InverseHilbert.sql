-- =============================================
-- CLR Function: clr_InverseHilbert
-- Description: Converts Hilbert value back to spatial geometry
-- =============================================
CREATE FUNCTION [dbo].[clr_InverseHilbert]
(
    @hilbertValue BIGINT,
    @precision INT
)
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.HilbertCurve].[clr_InverseHilbert]
GO
