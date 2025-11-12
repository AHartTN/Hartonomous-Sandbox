CREATE FUNCTION provenance.clr_AtomicStreamSegments(@stream provenance.AtomicStream)
RETURNS TABLE
(
    segment_ordinal INT,
    segment_kind NVARCHAR(32),
    timestamp_utc DATETIME2(3),
    content_type NVARCHAR(128),
    metadata NVARCHAR(MAX),
    payload VARBINARY(MAX)
)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AtomicStreamFunctions].EnumerateSegments;
GO
