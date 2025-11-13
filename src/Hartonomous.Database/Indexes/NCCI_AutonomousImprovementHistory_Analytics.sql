-- Nonclustered columnstore index for analytical queries on autonomous improvement patterns
-- Enables fast pattern analysis queries on improvement history
CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCI_AutonomousImprovementHistory_Analytics]
    ON [dbo].[AutonomousImprovementHistory]
    (
        [ChangeType],
        [RiskLevel],
        [EstimatedImpact],
        [SuccessScore],
        [TestsPassed],
        [TestsFailed],
        [PerformanceDelta],
        [WasDeployed],
        [WasRolledBack],
        [StartedAt],
        [CompletedAt]
    );
