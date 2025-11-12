CREATE FUNCTION dbo.clr_DeconstructImageToPatches(
    @rawImage VARBINARY(MAX),
    @imageWidth INT,
    @imageHeight INT,
    @patchSize INT,
    @strideSize INT
)
RETURNS TABLE (
    PatchIndex INT,
    RowIndex INT,
    ColIndex INT,
    PatchX INT,
    PatchY INT,
    PatchWidth INT,
    PatchHeight INT,
    PatchGeometry GEOMETRY,
    MeanR FLOAT,
    MeanG FLOAT,
    MeanB FLOAT,
    Variance FLOAT
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].DeconstructImageToPatches;