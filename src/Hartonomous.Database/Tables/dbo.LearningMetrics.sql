CREATE TABLE [dbo].[LearningMetrics]
(
    [MetricId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [AnalysisId] UNIQUEIDENTIFIER NOT NULL,
    [MetricType] NVARCHAR(100) NOT NULL,
    [MetricValue] DECIMAL(18,4) NOT NULL,
    [MeasuredAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT [PK_LearningMetrics] PRIMARY KEY CLUSTERED ([MetricId]),
    CONSTRAINT [FK_LearningMetrics_AutonomousImprovementHistory] 
        FOREIGN KEY ([AnalysisId]) 
        REFERENCES [dbo].[AutonomousImprovementHistory]([ImprovementId])
);
GO

CREATE NONCLUSTERED INDEX [IX_LearningMetrics_AnalysisId] 
    ON [dbo].[LearningMetrics]([AnalysisId]) 
    INCLUDE ([MetricType], [MetricValue], [MeasuredAt]);
GO

CREATE NONCLUSTERED INDEX [IX_LearningMetrics_MetricType_MeasuredAt] 
    ON [dbo].[LearningMetrics]([MetricType], [MeasuredAt] DESC)
    INCLUDE ([MetricValue]);
GO
