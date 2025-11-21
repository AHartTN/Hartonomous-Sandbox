-- =============================================
-- Table: dbo.InferenceFeedback
-- Description: Stores user feedback for RLHF (Reinforcement Learning from Human Feedback)
-- Purpose: Collects ratings and comments to adjust relationship weights
-- =============================================

CREATE TABLE [dbo].[InferenceFeedback]
(
    [FeedbackId] BIGINT NOT NULL IDENTITY(1,1),
    [InferenceRequestId] BIGINT NOT NULL,
    [Rating] INT NOT NULL,
    [Comments] NVARCHAR(2000) NULL,
    [UserId] NVARCHAR(128) NULL,
    [FeedbackTimestamp] DATETIME2(7) NOT NULL CONSTRAINT [DF_InferenceFeedback_Timestamp] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_InferenceFeedback] PRIMARY KEY CLUSTERED ([FeedbackId] ASC),
    CONSTRAINT [CK_InferenceFeedback_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
    CONSTRAINT [FK_InferenceFeedback_InferenceRequest] 
        FOREIGN KEY ([InferenceRequestId]) 
        REFERENCES [dbo].[InferenceRequest]([InferenceId])
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceFeedback_InferenceRequestId]
    ON [dbo].[InferenceFeedback]([InferenceRequestId]);
GO

CREATE NONCLUSTERED INDEX [IX_InferenceFeedback_Rating]
    ON [dbo].[InferenceFeedback]([Rating]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Stores user feedback for RLHF. Ratings (1-5) adjust AtomRelation weights to improve inference quality over time.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'InferenceFeedback';
GO
