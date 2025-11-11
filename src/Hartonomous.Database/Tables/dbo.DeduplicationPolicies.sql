CREATE TABLE [dbo].[DeduplicationPolicies] (
    [DeduplicationPolicyId] INT            NOT NULL IDENTITY,
    [PolicyName]            NVARCHAR (128) NOT NULL,
    [SemanticThreshold]     FLOAT (53)     NULL,
    [SpatialThreshold]      FLOAT (53)     NULL,
    [Metadata]              NVARCHAR(MAX)  NULL,
    [IsActive]              BIT            NOT NULL DEFAULT CAST(1 AS BIT),
    [CreatedAt]             DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_DeduplicationPolicies] PRIMARY KEY CLUSTERED ([DeduplicationPolicyId] ASC)
);
