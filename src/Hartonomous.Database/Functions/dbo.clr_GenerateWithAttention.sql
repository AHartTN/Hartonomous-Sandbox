-- =============================================
-- CLR Function: fn_GenerateWithAttention
-- Description: Generates output using attention mechanism
-- =============================================
CREATE FUNCTION [dbo].[fn_GenerateWithAttention]
(
    @modelId INT,
    @inputAtomIds NVARCHAR(MAX),
    @contextJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @topP FLOAT,
    @attentionHeads INT,
    @tenantId INT
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AttentionGeneration].[fn_GenerateWithAttention]
GO
