-- =============================================
-- Table: dbo.BillingMultipliers
-- Description: Rate multipliers for dynamic pricing based on dimensions/conditions.
--              Enables pricing adjustments for time, region, model type, priority, etc.
-- =============================================
CREATE TABLE [dbo].[BillingMultipliers]
(
    [MultiplierId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [RatePlanId]     UNIQUEIDENTIFIER NOT NULL,
    [Dimension]      NVARCHAR(32)     NOT NULL DEFAULT '',
    [Key]            NVARCHAR(128)    NOT NULL DEFAULT '',
    [Multiplier]     DECIMAL(18,6)    NOT NULL,
    [IsActive]       BIT              NOT NULL DEFAULT (1),
    [CreatedUtc]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc]     DATETIME2(7)     NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_BillingMultipliers] PRIMARY KEY CLUSTERED ([MultiplierId] ASC),

    CONSTRAINT [FK_BillingMultipliers_BillingRatePlans] 
        FOREIGN KEY ([RatePlanId]) 
        REFERENCES [dbo].[BillingRatePlans]([RatePlanId]) 
        ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_BillingMultipliers_Active]
    ON [dbo].[BillingMultipliers]([RatePlanId] ASC, [Dimension] ASC, [Key] ASC)
    WHERE ([IsActive] = 1);
GO