CREATE FUNCTION dbo.clr_ParseModelLayer(
    @modelBlob VARBINARY(MAX),
    @tensorName NVARCHAR(256),
    @modelFormatHint NVARCHAR(50)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ModelParsing].clr_ParseModelLayer;