-- =============================================
-- CLR Function: clr_HilbertRangeStart
-- Description: Computes Hilbert range start for a bounding box
-- =============================================
CREATE FUNCTION [dbo].[clr_HilbertRangeStart]
(
    @boundingBox GEOMETRY,
    @precision INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.HilbertCurve].[clr_HilbertRangeStart]
GO
