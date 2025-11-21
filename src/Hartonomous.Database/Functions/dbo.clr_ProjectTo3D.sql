-- =============================================
-- CLR Function: fn_ProjectTo3D
-- Description: Projects high-dimensional vector to 3D space
-- =============================================
CREATE FUNCTION [dbo].[fn_ProjectTo3D]
(
    @vector VARBINARY(MAX)
)
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.SpatialOperations].[fn_ProjectTo3D]
GO
