CREATE TABLE [dbo].[BillingMultiplier] (
    [MultiplierId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [RatePlanId]   UNIQUEIDENTIFIER NOT NULL,
    [Dimension]    NVARCHAR (32)    NOT NULL DEFAULT N'',
    [Key]          NVARCHAR (128)   NOT NULL DEFAULT N'',
    [Multiplier]   DECIMAL (18, 6)  NOT NULL,
    [IsActive]     BIT              NOT NULL DEFAULT CAST(1 AS BIT),
    [CreatedUtc]   DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]   DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BillingMultipliers] PRIMARY KEY CLUSTERED ([MultiplierId] ASC),
    CONSTRAINT [FK_BillingMultipliers_BillingRatePlans_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [dbo].[BillingRatePlan] ([RatePlanId]) ON DELETE CASCADE
);
