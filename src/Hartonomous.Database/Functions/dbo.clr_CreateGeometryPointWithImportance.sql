-- =============================================
-- CLR Function: clr_CreateGeometryPointWithImportance
-- Description: Creates a 3D point with importance metadata
-- =============================================
CREATE FUNCTION [dbo].[clr_CreateGeometryPointWithImportance]
(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT,
    @importance FLOAT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.SVDGeometryFunctions].[clr_CreateGeometryPointWithImportance]
GO
