-- =============================================
-- Table: dbo.BillingCredit
-- Description: Tracks billing credits, refunds, and promotional adjustments
-- Purpose: Manages tenant account credits for refunds, promotions, and trial periods
-- =============================================

CREATE TABLE [dbo].[BillingCredit]
(
    [BillingCreditId] BIGINT NOT NULL IDENTITY(1,1),
    [TenantId] INT NOT NULL,
    [Amount] DECIMAL(18, 2) NOT NULL,
    [CreditType] NVARCHAR(50) NOT NULL, -- 'refund', 'adjustment', 'promotion', 'trial'
    [Reason] NVARCHAR(MAX) NULL,
    [ReferenceId] NVARCHAR(255) NULL, -- Invoice ID, Payment ID, etc.
    [AppliedToInvoiceId] BIGINT NULL,
    [CreatedBy] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_BillingCredit_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [ExpiresAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_BillingCredit] PRIMARY KEY CLUSTERED ([BillingCreditId] ASC),
    CONSTRAINT [FK_BillingCredit_Tenant] FOREIGN KEY ([TenantId])
        REFERENCES [dbo].[TenantGuidMapping]([TenantId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BillingCredit_Invoice] FOREIGN KEY ([AppliedToInvoiceId])
        REFERENCES [dbo].[BillingInvoice]([InvoiceId]) ON DELETE SET NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_BillingCredit_TenantId]
    ON [dbo].[BillingCredit]([TenantId], [CreatedAt] DESC)
    INCLUDE ([Amount], [CreditType]);
GO

CREATE NONCLUSTERED INDEX [IX_BillingCredit_CreditType]
    ON [dbo].[BillingCredit]([CreditType], [CreatedAt] DESC)
    INCLUDE ([TenantId], [Amount]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tracks billing credits, refunds, and promotional adjustments. Manages tenant account credits for various purposes including refunds, promotions, and trial periods.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'BillingCredit';
GO
