CREATE FUNCTION provenance.clr_CreateAtomicStream
(
    @streamId UNIQUEIDENTIFIER,
    @createdUtc DATETIME,
    @scope NVARCHAR(128),
    @model NVARCHAR(128),
    @metadata NVARCHAR(MAX)
)
RETURNS VARBINARY(MAX) -- TODO: Change back to provenance.AtomicStream
AS EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AtomicStream].[Create];
GO