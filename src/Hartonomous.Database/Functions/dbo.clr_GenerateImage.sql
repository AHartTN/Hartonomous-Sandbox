-- =============================================
-- CLR Function: fn_GenerateImage
-- Description: Generates image using multi-modal generation
-- =============================================
CREATE FUNCTION [dbo].[fn_GenerateImage]
(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxPatches INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.MultiModalGeneration].[fn_GenerateImage]
GO
