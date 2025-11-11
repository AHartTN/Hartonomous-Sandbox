CREATE TABLE [provenance].[Concepts] (
    [ConceptId]       BIGINT         NOT NULL IDENTITY,
    [ConceptName]     NVARCHAR (200) NOT NULL,
    [Description]     NVARCHAR (MAX) NULL,
    [CentroidVector]  VARBINARY (MAX)NOT NULL,
    [VectorDimension] INT            NOT NULL,
    [MemberCount]     INT            NOT NULL DEFAULT 0,
    [CoherenceScore]  FLOAT (53)     NULL,
    [SeparationScore] FLOAT (53)     NULL,
    [DiscoveryMethod] NVARCHAR (100) NOT NULL,
    [ModelId]         INT            NOT NULL,
    [DiscoveredAt]    DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastUpdatedAt]   DATETIME2 (7)  NULL,
    [IsActive]        BIT            NOT NULL DEFAULT CAST(1 AS BIT),
    CONSTRAINT [PK_Concepts] PRIMARY KEY CLUSTERED ([ConceptId] ASC),
    CONSTRAINT [FK_Concepts_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId]) ON DELETE CASCADE
);