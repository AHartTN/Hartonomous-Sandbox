-- Create a new AtomicStream provenance container
-- Initializes stream with ID, timestamp, scope, model, and metadata

CREATE FUNCTION [provenance].[clr_CreateAtomicStream]
(
    @streamId UNIQUEIDENTIFIER,
    @createdUtc DATETIME,
    @scope NVARCHAR(MAX),
    @model NVARCHAR(MAX),
    @metadata NVARCHAR(MAX)
)
RETURNS dbo.AtomicStream
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AtomicStream].[Create];
GO
