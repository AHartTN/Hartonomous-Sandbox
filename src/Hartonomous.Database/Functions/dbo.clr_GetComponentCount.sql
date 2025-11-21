-- =============================================
-- CLR Function: fn_GetComponentCount
-- Description: Gets component count from stream
-- =============================================
CREATE FUNCTION [dbo].[fn_GetComponentCount]
(
    @componentStream VARBINARY(MAX)
)
RETURNS INT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.StreamOrchestrator].[fn_GetComponentCount]
GO
