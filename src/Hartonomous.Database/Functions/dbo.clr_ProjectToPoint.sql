CREATE FUNCTION dbo.clr_ProjectToPoint(@vectorJson NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_ProjectToPoint;