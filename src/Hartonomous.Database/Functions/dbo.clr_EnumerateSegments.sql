CREATE FUNCTION dbo.clr_EnumerateSegments(@stream VARBINARY(MAX))
RETURNS TABLE (
    SegmentIndex INT,
    SegmentType NVARCHAR(50),
    SegmentData VARBINARY(MAX)
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AtomicStreamFunctions].EnumerateSegments;