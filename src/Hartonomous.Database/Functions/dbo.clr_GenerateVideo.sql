-- =============================================
-- CLR Function: fn_GenerateVideo
-- Description: Generates video using multi-modal generation
-- =============================================
CREATE FUNCTION [dbo].[fn_GenerateVideo]
(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxFrames INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.MultiModalGeneration].[fn_GenerateVideo]
GO
