CREATE FUNCTION dbo.fn_ParseModelCapabilities(@modelMetadataJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AutonomousFunctions].fn_ParseModelCapabilities;