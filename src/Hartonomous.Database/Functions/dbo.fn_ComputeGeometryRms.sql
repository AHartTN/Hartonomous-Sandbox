-- Auto-split from dbo.sp_AtomizeAudio.sql
-- Object: FUNCTION dbo.fn_ComputeGeometryRms

CREATE FUNCTION dbo.fn_ComputeGeometryRms(@Waveform GEOMETRY)
RETURNS FLOAT
AS
BEGIN
    IF @Waveform IS NULL OR @Waveform.STGeometryType() <> 'LINESTRING'
        RETURN 0;
    
    DECLARE @PointCount INT = @Waveform.STNumPoints();
    DECLARE @SumSquares FLOAT = 0;
    DECLARE @PointIndex INT = 1;
    DECLARE @Amplitude FLOAT;
    
    WHILE @PointIndex <= @PointCount
    BEGIN
        -- Y coordinate is amplitude
        SET @Amplitude = @Waveform.STPointN(@PointIndex).STY.Value;
        SET @SumSquares = @SumSquares + (@Amplitude * @Amplitude);
        SET @PointIndex = @PointIndex + 1;
    END
    
    RETURN SQRT(@SumSquares / @PointCount);
END;
GO