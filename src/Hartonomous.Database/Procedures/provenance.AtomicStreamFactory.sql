
CREATE FUNCTION provenance.clr_CreateAtomicStream
(
    @streamId UNIQUEIDENTIFIER,
    @createdUtc DATETIME,
    @scope NVARCHAR(128),
    @model NVARCHAR(128),
    @metadata NVARCHAR(MAX)
)
RETURNS provenance.AtomicStream
AS EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AtomicStream].[Create];

CREATE FUNCTION provenance.clr_AppendAtomicStreamSegment
(
    @stream provenance.AtomicStream,
    @kind NVARCHAR(32),
    @timestampUtc DATETIME,
    @contentType NVARCHAR(128),
    @metadata NVARCHAR(MAX),
    @payload VARBINARY(MAX)
)
RETURNS provenance.AtomicStream
AS EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AtomicStream].[AppendSegment];
