-- =============================================
-- CLR Function: clr_ReconstructFromSVD
-- Description: Reconstructs matrix from SVD components
-- =============================================
CREATE FUNCTION [dbo].[clr_ReconstructFromSVD]
(
    @UJson NVARCHAR(MAX),
    @SJson NVARCHAR(MAX),
    @VTJson NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.SVDGeometryFunctions].[clr_ReconstructFromSVD]
GO
