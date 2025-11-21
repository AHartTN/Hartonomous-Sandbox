-- =============================================
-- CLR Function: fn_GetTimeWindow
-- Description: Gets time window from stream
-- =============================================
CREATE FUNCTION [dbo].[fn_GetTimeWindow]
(
    @componentStream VARBINARY(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.StreamOrchestrator].[fn_GetTimeWindow]
GO
