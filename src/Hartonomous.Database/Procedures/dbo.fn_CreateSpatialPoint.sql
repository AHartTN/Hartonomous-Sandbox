CREATE FUNCTION dbo.fn_CreateSpatialPoint(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT = NULL
)
RETURNS GEOMETRY
AS
BEGIN
    DECLARE @result GEOMETRY;

    IF @z IS NULL
        SET @result = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ')', 0);
    ELSE
        SET @result = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ' ' + CAST(@z AS NVARCHAR(50)) + ')', 0);

    RETURN @result;
END;
GO