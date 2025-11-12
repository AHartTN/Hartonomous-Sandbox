CREATE AGGREGATE dbo.AutoencoderCompression(@input NVARCHAR(MAX), @latentDim INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AutoencoderCompression];