CREATE TABLE [dbo].[BillingInvoice] (
    [InvoiceId]          BIGINT          NOT NULL IDENTITY,
    [TenantId]           INT             NOT NULL,
    [InvoiceNumber]      NVARCHAR (100)  NOT NULL,
    [BillingPeriodStart] DATETIME2 (7)   NOT NULL,
    [BillingPeriodEnd]   DATETIME2 (7)   NOT NULL,
    [Subtotal]           DECIMAL (18, 2) NOT NULL,
    [Discount]           DECIMAL (18, 2) NOT NULL DEFAULT 0,
    [Tax]                DECIMAL (18, 2) NOT NULL DEFAULT 0,
    [Total]              DECIMAL (18, 2) NOT NULL,
    [Status]             NVARCHAR (50)   NOT NULL, -- 'Pending', 'Paid', 'Overdue', 'Cancelled'
    [GeneratedUtc]       DATETIME2 (7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [PaidUtc]            DATETIME2 (7)   NULL,
    [MetadataJson]       NVARCHAR (MAX)  NULL,
    
    -- Stripe integration columns
    [StripeInvoiceId]    NVARCHAR (255)  NULL,
    [StripeStatus]       NVARCHAR (50)   NULL,
    [StripePdfUrl]       NVARCHAR (500)  NULL,
    [StripeHostedUrl]    NVARCHAR (500)  NULL,
    
    CONSTRAINT [PK_BillingInvoices] PRIMARY KEY CLUSTERED ([InvoiceId] ASC),
    CONSTRAINT [UQ_BillingInvoices_Number] UNIQUE NONCLUSTERED ([InvoiceNumber]),
    INDEX [IX_BillingInvoices_Tenant] ([TenantId], [GeneratedUtc] DESC),
    INDEX [IX_BillingInvoices_Status] ([Status], [GeneratedUtc] DESC),
    INDEX [IX_BillingInvoices_StripeId] ([StripeInvoiceId]) INCLUDE ([InvoiceId], [TenantId], [Status])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_BillingInvoices_StripeId]
    ON [dbo].[BillingInvoice]([StripeInvoiceId])
    WHERE [StripeInvoiceId] IS NOT NULL;
GO
