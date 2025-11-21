-- =============================================
-- CLR Function: fn_GenerateMultiModal
-- Description: Generates output in specified target modality
-- =============================================
CREATE FUNCTION [dbo].[fn_GenerateMultiModal]
(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @targetModality NVARCHAR(50),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.MultiModalGeneration].[fn_GenerateMultiModal]
GO
