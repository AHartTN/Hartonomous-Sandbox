-- Enumerate segments from an AtomicStream provenance value
-- Returns table of segments with ordinals, kinds, timestamps, metadata, and payloads

CREATE FUNCTION [provenance].[clr_EnumerateAtomicStreamSegments]
(
    @stream dbo.AtomicStream
)
RETURNS TABLE
(
    segment_ordinal INT,
    segment_kind NVARCHAR(32),
    timestamp_utc DATETIME,
    content_type NVARCHAR(128),
    metadata NVARCHAR(MAX),
    payload VARBINARY(MAX)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AtomicStreamFunctions].[EnumerateSegments];
GO
