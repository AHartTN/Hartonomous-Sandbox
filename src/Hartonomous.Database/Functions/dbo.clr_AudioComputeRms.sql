-- =============================================
-- CLR Function: clr_AudioComputeRms
-- Description: Computes RMS (Root Mean Square) of audio data
-- =============================================
CREATE FUNCTION [dbo].[clr_AudioComputeRms]
(
    @audioData VARBINARY(MAX),
    @channelCount INT
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AudioProcessing].[AudioComputeRms]
GO
