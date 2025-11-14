-- =============================================
-- Hilbert Curve Scalar Functions
-- =============================================
-- Wraps CLR Hilbert curve functions for use in SQL

-- Compute Hilbert value for a GEOMETRY point
CREATE OR ALTER FUNCTION [dbo].[fn_ComputeHilbertValue] (
    @spatialKey GEOMETRY
)
RETURNS BIGINT
AS
BEGIN
    -- Using 21-bit precision for 63-bit Hilbert value (3 * 21 = 63 bits)
    RETURN [dbo].[clr_ComputeHilbertValue](@spatialKey, 21);
END
GO

-- Inverse Hilbert: convert 1D value back to 3D point
CREATE OR ALTER FUNCTION [dbo].[fn_InverseHilbert] (
    @hilbertValue BIGINT
)
RETURNS GEOMETRY
AS
BEGIN
    RETURN [dbo].[clr_InverseHilbert](@hilbertValue, 21);
END
GO

-- Get Hilbert range start for a bounding box (useful for range queries)
CREATE OR ALTER FUNCTION [dbo].[fn_HilbertRangeStart] (
    @boundingBox GEOMETRY
)
RETURNS BIGINT
AS
BEGIN
    RETURN [dbo].[clr_HilbertRangeStart](@boundingBox, 21);
END
GO
