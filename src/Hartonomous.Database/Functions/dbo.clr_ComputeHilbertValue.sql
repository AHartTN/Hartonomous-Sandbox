-- ==================================================
-- CLR Scalar Function: dbo.clr_ComputeHilbertValue
-- Source: HilbertCurve.cs
-- ==================================================
-- Computes Hilbert curve value for a 3D GEOMETRY point
-- Uses 21-bit precision per dimension (63 total bits fitting in BIGINT)
-- Provides 1D ordering that preserves spatial locality
-- ==================================================

CREATE FUNCTION [dbo].[clr_ComputeHilbertValue]
(
    @spatialKey GEOMETRY,
    @precision INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[SpatialFunctions].[clr_ComputeHilbertValue]
GO

GRANT EXECUTE ON dbo.clr_ComputeHilbertValue TO PUBLIC;
GO
