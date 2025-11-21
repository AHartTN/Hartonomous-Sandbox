-- =============================================
-- Table: dbo.BillingPayment
-- Description: Tracks payment transactions with Stripe integration
-- Purpose: Records all payment attempts, successes, and failures for audit trail
-- =============================================

CREATE TABLE [dbo].[BillingPayment]
(
    [BillingPaymentId] BIGINT NOT NULL IDENTITY(1,1),
    [TenantId] INT NOT NULL,
    [StripePaymentIntentId] NVARCHAR(255) NOT NULL,
    [Amount] DECIMAL(18, 2) NOT NULL,
    [Currency] NVARCHAR(3) NOT NULL CONSTRAINT [DF_BillingPayment_Currency] DEFAULT 'usd',
    [Status] NVARCHAR(50) NOT NULL, -- 'succeeded', 'failed', 'pending', 'canceled'
    [PaymentMethod] NVARCHAR(255) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [ReceiptUrl] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_BillingPayment_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_BillingPayment_UpdatedAt] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_BillingPayment] PRIMARY KEY CLUSTERED ([BillingPaymentId] ASC),
    CONSTRAINT [UQ_BillingPayment_StripeId] UNIQUE NONCLUSTERED ([StripePaymentIntentId]),
    CONSTRAINT [FK_BillingPayment_Tenant] FOREIGN KEY ([TenantId])
        REFERENCES [dbo].[TenantGuidMapping]([TenantId]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_BillingPayment_TenantId]
    ON [dbo].[BillingPayment]([TenantId], [CreatedAt] DESC)
    INCLUDE ([Amount], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_BillingPayment_Status]
    ON [dbo].[BillingPayment]([Status], [CreatedAt] DESC)
    INCLUDE ([TenantId], [Amount]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tracks all payment transactions with Stripe integration. Provides complete audit trail of payment attempts, successes, and failures.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'BillingPayment';
GO
