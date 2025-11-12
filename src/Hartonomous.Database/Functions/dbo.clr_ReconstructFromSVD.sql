CREATE FUNCTION dbo.clr_ReconstructFromSVD(
    @UJson NVARCHAR(MAX),
    @SJson NVARCHAR(MAX),
    @VTJson NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_ReconstructFromSVD;