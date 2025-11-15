-- =============================================
-- fn_EstimateResponseTime: Estimate Response Time in Milliseconds
-- Returns estimated latency based on complexity and SLA tier
-- =============================================
CREATE FUNCTION [dbo].[fn_EstimateResponseTime](
    @complexity INT,
    @sla NVARCHAR(20)
)
RETURNS INT
AS
BEGIN
    DECLARE @baseTime INT;

    -- Base time from SLA tier
    SET @baseTime = CASE @sla
        WHEN 'realtime' THEN 50
        WHEN 'interactive' THEN 500
        WHEN 'standard' THEN 5000
        WHEN 'batch' THEN 30000
        ELSE 10000
    END;

    -- Adjust for complexity (logarithmic scaling)
    DECLARE @adjustedTime INT = @baseTime + (@complexity / 100);

    RETURN @adjustedTime;
END;
GO
