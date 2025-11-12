CREATE FUNCTION dbo.clr_CreateGeometryPointWithImportance(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT,
    @importance FLOAT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SVDGeometryFunctions].clr_CreateGeometryPointWithImportance;