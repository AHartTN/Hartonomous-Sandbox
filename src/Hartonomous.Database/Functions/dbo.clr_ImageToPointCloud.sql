-- =============================================
-- CLR Function: clr_ImageToPointCloud
-- Description: Converts image data to a point cloud geometry
-- =============================================
CREATE FUNCTION [dbo].[clr_ImageToPointCloud]
(
    @imageData VARBINARY(MAX),
    @width INT,
    @height INT,
    @sampleStep INT
)
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImageProcessing].[ImageToPointCloud]
GO
