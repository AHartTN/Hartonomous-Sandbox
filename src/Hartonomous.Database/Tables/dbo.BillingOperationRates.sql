-- =============================================
-- Table: dbo.BillingOperationRates
-- Description: Operation-specific billing rates within rate plans.
--              Enables granular pricing for different operation types (inference, embedding, storage).
-- =============================================
CREATE TABLE [dbo].[BillingOperationRates]
(
    [OperationRateId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [RatePlanId]        UNIQUEIDENTIFIER NOT NULL,
    [Operation]         NVARCHAR(128)    NOT NULL DEFAULT '',
    [UnitOfMeasure]     NVARCHAR(64)     NOT NULL DEFAULT '',
    [Category]          NVARCHAR(64)     NULL,
    [Description]       NVARCHAR(256)    NULL,
    [Rate]              DECIMAL(18,6)    NOT NULL,
    [IsActive]          BIT              NOT NULL DEFAULT (1),
    [CreatedUtc]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]        DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_BillingOperationRates] PRIMARY KEY CLUSTERED ([OperationRateId] ASC),

    CONSTRAINT [FK_BillingOperationRates_BillingRatePlans] 
        FOREIGN KEY ([RatePlanId]) 
        REFERENCES [dbo].[BillingRatePlans]([RatePlanId]) 
        ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_BillingOperationRates_Active]
    ON [dbo].[BillingOperationRates]([RatePlanId] ASC, [Operation] ASC)
    WHERE ([IsActive] = 1);
GO