CREATE NONCLUSTERED INDEX IX_IngestionJob_ParentAtomId
ON dbo.IngestionJob(ParentAtomId, JobStatus)
INCLUDE (IngestionJobId, TenantId, LastUpdatedAt);
