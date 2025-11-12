CREATE FUNCTION dbo.clr_ImageAverageColor(@image VARBINARY(MAX), @width INT, @height INT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageAverageColor;