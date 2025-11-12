CREATE FUNCTION dbo.clr_ImageToPointCloud(@image VARBINARY(MAX), @width INT, @height INT, @sampleStep INT)
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageToPointCloud;
GO
