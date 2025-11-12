CREATE FUNCTION dbo.clr_SvdDecompose(
    @weightArrayJson NVARCHAR(MAX),
    @rows INT,
    @cols INT,
    @maxRank INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_SvdDecompose;