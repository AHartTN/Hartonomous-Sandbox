-- =============================================
-- CLR Function: clr_GenerateSequence (TVF)
-- Description: Generates sequence of atoms from seed embedding
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateSequence]
(
    @seedEmbedding VARBINARY(MAX),
    @modelsJson NVARCHAR(MAX),
    @maxTokens INT,
    @temperature FLOAT,
    @topK INT,
    @requiredModality NVARCHAR(50)
)
RETURNS TABLE
(
    SequenceIndex INT,
    AtomId BIGINT,
    Probability FLOAT,
    Modality NVARCHAR(50)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.GenerationFunctions].[GenerateSequence]
GO
