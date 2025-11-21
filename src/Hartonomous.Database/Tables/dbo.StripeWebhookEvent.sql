-- =============================================
-- Table: dbo.StripeWebhookEvent
-- Description: Audit log for all Stripe webhook events
-- Purpose: Complete audit trail of Stripe webhook processing for debugging and compliance
-- =============================================

CREATE TABLE [dbo].[StripeWebhookEvent]
(
    [WebhookEventId] BIGINT NOT NULL IDENTITY(1,1),
    [StripeEventId] NVARCHAR(255) NOT NULL,
    [EventType] NVARCHAR(255) NOT NULL,
    [EventData] NVARCHAR(MAX) NOT NULL, -- JSON payload
    [ProcessedSuccessfully] BIT NOT NULL CONSTRAINT [DF_StripeWebhookEvent_Processed] DEFAULT 0,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [ReceivedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_StripeWebhookEvent_ReceivedAt] DEFAULT (SYSUTCDATETIME()),
    [ProcessedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_StripeWebhookEvent] PRIMARY KEY CLUSTERED ([WebhookEventId] ASC),
    CONSTRAINT [UQ_StripeWebhookEvent_EventId] UNIQUE NONCLUSTERED ([StripeEventId])
);
GO

CREATE NONCLUSTERED INDEX [IX_StripeWebhookEvent_Type]
    ON [dbo].[StripeWebhookEvent]([EventType], [ReceivedAt] DESC)
    INCLUDE ([ProcessedSuccessfully]);
GO

CREATE NONCLUSTERED INDEX [IX_StripeWebhookEvent_ReceivedAt]
    ON [dbo].[StripeWebhookEvent]([ReceivedAt] DESC)
    INCLUDE ([EventType], [ProcessedSuccessfully]);
GO

CREATE NONCLUSTERED INDEX [IX_StripeWebhookEvent_Failed]
    ON [dbo].[StripeWebhookEvent]([ProcessedSuccessfully], [ReceivedAt] DESC)
    WHERE [ProcessedSuccessfully] = 0;
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Complete audit log of all Stripe webhook events. Provides debugging and compliance trail for webhook processing including failures and retries.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'StripeWebhookEvent';
GO
