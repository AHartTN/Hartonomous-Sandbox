CREATE NONCLUSTERED INDEX IX_IngestionJob_LastUpdatedAt
ON dbo.IngestionJob(LastUpdatedAt DESC)
INCLUDE (IngestionJobId, TenantId, JobStatus, ParentAtomId)
WHERE JobStatus <> 'Complete';
