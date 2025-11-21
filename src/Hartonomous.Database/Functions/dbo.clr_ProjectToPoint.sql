-- =============================================
-- CLR Function: clr_ProjectToPoint
-- Description: Projects vector JSON to 3D point
-- =============================================
CREATE FUNCTION [dbo].[clr_ProjectToPoint]
(
    @vectorJson NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.SVDGeometryFunctions].[clr_ProjectToPoint]
GO
