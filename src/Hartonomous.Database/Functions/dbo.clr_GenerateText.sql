-- =============================================
-- CLR Function: fn_GenerateText
-- Description: Generates text using multi-modal generation
-- =============================================
CREATE FUNCTION [dbo].[fn_GenerateText]
(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.MultiModalGeneration].[fn_GenerateText]
GO
