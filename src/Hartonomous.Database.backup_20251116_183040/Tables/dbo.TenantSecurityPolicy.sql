CREATE TABLE [dbo].[TenantSecurityPolicy] (
    [PolicyId]    INT            NOT NULL IDENTITY,
    [TenantId]    NVARCHAR (128) NOT NULL,
    [PolicyName]  NVARCHAR (100) NOT NULL,
    [PolicyType]  NVARCHAR (50)  NOT NULL,
    [PolicyRules] NVARCHAR (MAX) NOT NULL,
    [IsActive]    BIT            NOT NULL DEFAULT CAST(1 AS BIT),
    [EffectiveFrom] DATETIME2 (7)  NULL,
    [EffectiveTo] DATETIME2 (7)  NULL,
    [CreatedUtc]  DATETIME2 (7)  NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]  DATETIME2 (7)  NULL,
    [CreatedBy]   NVARCHAR (256) NULL,
    [UpdatedBy]   NVARCHAR (256) NULL,
    CONSTRAINT [PK_TenantSecurityPolicy] PRIMARY KEY CLUSTERED ([PolicyId] ASC)
);
