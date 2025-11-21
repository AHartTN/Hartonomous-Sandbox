-- =============================================
-- CLR Function: clr_AudioComputePeak
-- Description: Computes peak amplitude of audio data
-- =============================================
CREATE FUNCTION [dbo].[clr_AudioComputePeak]
(
    @audioData VARBINARY(MAX),
    @channelCount INT
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AudioProcessing].[AudioComputePeak]
GO
