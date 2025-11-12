CREATE AGGREGATE dbo.MatrixFactorization(@userVector NVARCHAR(MAX), @itemVector NVARCHAR(MAX), @latentFactors INT)
RETURNS NVARCHAR(MAX)
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.MatrixFactorization];