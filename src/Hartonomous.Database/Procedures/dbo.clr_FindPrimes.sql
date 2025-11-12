CREATE FUNCTION dbo.clr_FindPrimes(@maxValue BIGINT)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.PrimeNumberSearch].clr_FindPrimes;