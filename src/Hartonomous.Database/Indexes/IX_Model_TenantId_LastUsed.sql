CREATE NONCLUSTERED INDEX IX_Model_TenantId_LastUsed
ON dbo.Model(TenantId, LastUsed DESC)
INCLUDE (ModelId, ModelName, ModelType, UsageCount)
WHERE LastUsed IS NOT NULL;
