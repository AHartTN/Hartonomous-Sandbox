CREATE FUNCTION dbo.clr_BuildPerformanceVector(
    @durationMs INT,
    @tokenCount INT,
    @hourOfDay INT,
    @dayOfWeek INT,
    @vectorDimension INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.PerformanceAnalysis].BuildPerformanceVector;