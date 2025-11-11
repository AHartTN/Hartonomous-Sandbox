CREATE TABLE [dbo].[BillingRatePlans] (
    [RatePlanId]              UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId]                NVARCHAR (64)    NULL,
    [PlanCode]                NVARCHAR (64)    NOT NULL DEFAULT N'',
    [Name]                    NVARCHAR (128)   NOT NULL DEFAULT N'',
    [Description]             NVARCHAR (256)   NULL,
    [DefaultRate]             DECIMAL (18, 6)  NOT NULL DEFAULT 0.01,
    [MonthlyFee]              DECIMAL (18, 2)  NOT NULL DEFAULT 0.0,
    [UnitPricePerDcu]         DECIMAL (18, 6)  NOT NULL DEFAULT 0.00008,
    [IncludedPublicStorageGb] DECIMAL (18, 2)  NOT NULL DEFAULT 0.0,
    [IncludedPrivateStorageGb]DECIMAL (18, 2)  NOT NULL DEFAULT 0.0,
    [IncludedSeatCount]       INT              NOT NULL DEFAULT 1,
    [AllowsPrivateData]       BIT              NOT NULL DEFAULT CAST(0 AS BIT),
    [CanQueryPublicCorpus]    BIT              NOT NULL DEFAULT CAST(0 AS BIT),
    [IsActive]                BIT              NOT NULL DEFAULT CAST(1 AS BIT),
    [CreatedUtc]              DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]              DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BillingRatePlans] PRIMARY KEY CLUSTERED ([RatePlanId] ASC)
);
