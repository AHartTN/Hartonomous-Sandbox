CREATE TABLE [dbo].[BillingOperationRates] (
    [OperationRateId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [RatePlanId]      UNIQUEIDENTIFIER NOT NULL,
    [Operation]       NVARCHAR (128)   NOT NULL DEFAULT N'',
    [UnitOfMeasure]   NVARCHAR (64)    NOT NULL DEFAULT N'',
    [Category]        NVARCHAR (64)    NULL,
    [Description]     NVARCHAR (256)   NULL,
    [Rate]            DECIMAL (18, 6)  NOT NULL,
    [IsActive]        BIT              NOT NULL DEFAULT CAST(1 AS BIT),
    [CreatedUtc]      DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]      DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BillingOperationRates] PRIMARY KEY CLUSTERED ([OperationRateId] ASC),
    CONSTRAINT [FK_BillingOperationRates_BillingRatePlans_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [dbo].[BillingRatePlans] ([RatePlanId]) ON DELETE CASCADE
);
