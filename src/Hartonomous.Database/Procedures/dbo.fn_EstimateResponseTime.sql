CREATE FUNCTION dbo.fn_EstimateResponseTime(@complexity FLOAT)
RETURNS INT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_EstimateResponseTime;