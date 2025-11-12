CREATE AGGREGATE dbo.DBSCANCluster(@vector NVARCHAR(MAX), @eps FLOAT, @minPts INT)
RETURNS INT
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.DBSCANCluster];