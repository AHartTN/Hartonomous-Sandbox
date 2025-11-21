-- =============================================
-- CLR Function: fn_GenerateAudio
-- Description: Generates audio using multi-modal generation
-- =============================================
CREATE FUNCTION [dbo].[fn_GenerateAudio]
(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxSamples INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.MultiModalGeneration].[fn_GenerateAudio]
GO
