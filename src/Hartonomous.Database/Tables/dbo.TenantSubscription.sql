-- =============================================
-- Table: dbo.TenantSubscription
-- Description: Tracks tenant subscriptions with Stripe integration
-- Purpose: Manages recurring subscription lifecycle for multi-tenant billing
-- =============================================

CREATE TABLE [dbo].[TenantSubscription]
(
    [TenantSubscriptionId] BIGINT NOT NULL IDENTITY(1,1),
    [TenantId] INT NOT NULL,
    [StripeSubscriptionId] NVARCHAR(255) NOT NULL,
    [PlanId] NVARCHAR(255) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL, -- 'active', 'canceled', 'past_due', 'trialing', 'unpaid'
    [CurrentPeriodStart] DATETIME2(7) NULL,
    [CurrentPeriodEnd] DATETIME2(7) NULL,
    [CancelAtPeriodEnd] BIT NOT NULL CONSTRAINT [DF_TenantSubscription_CancelAtPeriodEnd] DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantSubscription_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantSubscription_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [CanceledAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_TenantSubscription] PRIMARY KEY CLUSTERED ([TenantSubscriptionId] ASC),
    CONSTRAINT [UQ_TenantSubscription_StripeId] UNIQUE NONCLUSTERED ([StripeSubscriptionId]),
    CONSTRAINT [FK_TenantSubscription_Tenant] FOREIGN KEY ([TenantId])
        REFERENCES [dbo].[TenantGuidMapping]([TenantId]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_TenantSubscription_TenantId]
    ON [dbo].[TenantSubscription]([TenantId])
    INCLUDE ([Status], [CurrentPeriodEnd]);
GO

CREATE NONCLUSTERED INDEX [IX_TenantSubscription_Status]
    ON [dbo].[TenantSubscription]([Status], [CreatedAt] DESC)
    INCLUDE ([TenantId], [PlanId]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tracks tenant subscriptions with Stripe integration. Manages recurring subscription lifecycle including trial periods, cancellations, and billing cycles.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TenantSubscription';
GO
