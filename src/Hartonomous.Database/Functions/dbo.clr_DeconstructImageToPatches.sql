-- =============================================
-- CLR Function: clr_DeconstructImageToPatches (TVF)
-- Description: Deconstructs an image into patches
-- =============================================
CREATE FUNCTION [dbo].[clr_DeconstructImageToPatches]
(
    @rawImage VARBINARY(MAX),
    @imageWidth INT,
    @imageHeight INT,
    @patchSize INT,
    @strideSize INT
)
RETURNS TABLE
(
    PatchIndex INT,
    PatchX INT,
    PatchY INT,
    PatchData VARBINARY(MAX)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageProcessing].[DeconstructImageToPatches]
GO
