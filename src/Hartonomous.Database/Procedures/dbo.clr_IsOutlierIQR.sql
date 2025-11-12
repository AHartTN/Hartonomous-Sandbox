CREATE FUNCTION dbo.clr_IsOutlierIQR(@value FLOAT, @q1 FLOAT, @q3 FLOAT, @iqrMultiplier FLOAT)
RETURNS BIT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.PerformanceAnalysis].IsOutlierIQR;