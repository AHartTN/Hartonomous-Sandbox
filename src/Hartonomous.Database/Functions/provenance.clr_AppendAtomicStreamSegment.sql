-- Append a segment to an AtomicStream provenance container
-- Adds timestamped segment with kind, content type, metadata, and binary payload

CREATE FUNCTION [provenance].[clr_AppendAtomicStreamSegment]
(
    @stream dbo.AtomicStream,
    @kind NVARCHAR(32),
    @timestampUtc DATETIME,
    @contentType NVARCHAR(128),
    @metadata NVARCHAR(MAX),
    @payload VARBINARY(MAX)
)
RETURNS dbo.AtomicStream
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AtomicStream].[AppendSegment];
GO
