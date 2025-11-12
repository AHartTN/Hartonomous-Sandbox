CREATE FUNCTION dbo.fn_DetermineSla(@complexity FLOAT, @tenantId INT)
RETURNS NVARCHAR(50)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_DetermineSla;