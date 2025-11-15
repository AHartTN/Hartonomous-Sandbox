-- =============================================
-- fn_DetermineSla: Determine SLA Tier Based on Complexity
-- Maps complexity score to SLA response time tier
-- =============================================
CREATE FUNCTION [dbo].[fn_DetermineSla](
    @complexity INT
)
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @sla NVARCHAR(20);

    IF @complexity < 1000
        SET @sla = 'realtime';      -- < 100ms
    ELSE IF @complexity < 10000
        SET @sla = 'interactive';   -- < 1s
    ELSE IF @complexity < 100000
        SET @sla = 'standard';      -- < 10s
    ELSE
        SET @sla = 'batch';         -- > 10s

    RETURN @sla;
END;
GO
