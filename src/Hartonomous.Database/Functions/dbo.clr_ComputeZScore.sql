CREATE FUNCTION dbo.clr_ComputeZScore(@value FLOAT, @mean FLOAT, @stdDev FLOAT)
RETURNS FLOAT
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.PerformanceAnalysis].ComputeZScore;