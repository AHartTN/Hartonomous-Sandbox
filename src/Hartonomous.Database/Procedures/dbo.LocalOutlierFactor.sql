CREATE AGGREGATE dbo.LocalOutlierFactor(@vector NVARCHAR(MAX), @k INT)
RETURNS FLOAT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.LocalOutlierFactor];
GO