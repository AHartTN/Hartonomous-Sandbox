-- =============================================
-- CLR Function: clr_ParseModelLayer
-- Description: Parses a model layer from blob data
-- =============================================
CREATE FUNCTION [dbo].[clr_ParseModelLayer]
(
    @modelBlob VARBINARY(MAX),
    @tensorName NVARCHAR(255),
    @modelFormatHint NVARCHAR(50)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ModelParsing].[clr_ParseModelLayer]
GO
