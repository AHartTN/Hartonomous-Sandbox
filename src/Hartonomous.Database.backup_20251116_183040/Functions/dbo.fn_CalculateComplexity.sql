-- =============================================
-- fn_CalculateComplexity: Estimate Computational Complexity
-- Returns complexity score based on input size and model characteristics
-- =============================================
CREATE FUNCTION [dbo].[fn_CalculateComplexity](
    @inputSize INT,
    @modelType NVARCHAR(100)
)
RETURNS INT
AS
BEGIN
    DECLARE @complexity INT = 1;

    -- Base complexity from input size
    SET @complexity = @inputSize;

    -- Model-specific multipliers
    IF @modelType LIKE '%transformer%' OR @modelType LIKE '%bert%'
        SET @complexity = @complexity * 10;  -- O(n²) attention
    ELSE IF @modelType LIKE '%lstm%' OR @modelType LIKE '%gru%'
        SET @complexity = @complexity * 5;   -- O(n) recurrence
    ELSE IF @modelType LIKE '%cnn%' OR @modelType LIKE '%convolution%'
        SET @complexity = @complexity * 3;   -- O(n) convolution
    ELSE
        SET @complexity = @complexity * 2;   -- Default linear

    RETURN @complexity;
END;
GO
