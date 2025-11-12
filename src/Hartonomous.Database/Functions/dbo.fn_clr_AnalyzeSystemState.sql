CREATE FUNCTION dbo.fn_clr_AnalyzeSystemState(@targetArea NVARCHAR(MAX))
RETURNS TABLE (
    AnalysisJson NVARCHAR(MAX)
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.Analysis.AutonomousAnalyticsTVF].fn_clr_AnalyzeSystemState;