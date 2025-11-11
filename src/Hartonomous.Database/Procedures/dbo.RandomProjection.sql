CREATE AGGREGATE dbo.RandomProjection(@vector NVARCHAR(MAX), @targetDim INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.RandomProjection];
GO