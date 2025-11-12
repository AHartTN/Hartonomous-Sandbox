CREATE FUNCTION dbo.clr_ImageLuminanceHistogram(@image VARBINARY(MAX), @width INT, @height INT, @binCount INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageLuminanceHistogram;