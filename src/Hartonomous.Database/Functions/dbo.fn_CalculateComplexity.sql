CREATE FUNCTION dbo.fn_CalculateComplexity(@atomId BIGINT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_CalculateComplexity;