-- =============================================
-- CLR Function: fn_ParseModelCapabilities (TVF)
-- Description: Parses capabilities from model name
-- =============================================
CREATE FUNCTION [dbo].[fn_ParseModelCapabilities]
(
    @modelName NVARCHAR(256)
)
RETURNS TABLE
(
    Capability NVARCHAR(100),
    Confidence FLOAT
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AutonomousFunctions].[fn_ParseModelCapabilities]
GO
