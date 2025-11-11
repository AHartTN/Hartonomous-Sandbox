CREATE FUNCTION dbo.fn_NormalizeJSON(@json NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    IF @json IS NULL OR ISJSON(@json) = 0
        RETURN @json;

    DECLARE @normalized NVARCHAR(MAX);

    SELECT @normalized = (
        SELECT [key], value
        FROM OPENJSON(@json)
        ORDER BY [key]
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    RETURN @normalized;
END;
GO