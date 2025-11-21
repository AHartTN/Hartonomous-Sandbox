-- =============================================
-- CLR Function: clr_GenerateTextSequence (TVF)
-- Description: Generates text sequence from seed embedding
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateTextSequence]
(
    @seedEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT
)
RETURNS TABLE
(
    SequenceIndex INT,
    AtomId BIGINT,
    TokenText NVARCHAR(MAX),
    Probability FLOAT
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.GenerationFunctions].[GenerateTextSequence]
GO
