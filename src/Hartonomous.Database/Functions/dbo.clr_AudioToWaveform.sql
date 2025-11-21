-- =============================================
-- CLR Function: clr_AudioToWaveform
-- Description: Converts audio data to a waveform geometry
-- =============================================
CREATE FUNCTION [dbo].[clr_AudioToWaveform]
(
    @audioData VARBINARY(MAX),
    @channelCount INT,
    @sampleRate INT,
    @maxPoints INT
)
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AudioProcessing].[AudioToWaveform]
GO
