-- =============================================
-- CLR Function: clr_AudioDownsample
-- Description: Downsamples audio data by a factor
-- =============================================
CREATE FUNCTION [dbo].[clr_AudioDownsample]
(
    @audioData VARBINARY(MAX),
    @channelCount INT,
    @factor INT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AudioProcessing].[AudioDownsample]
GO
