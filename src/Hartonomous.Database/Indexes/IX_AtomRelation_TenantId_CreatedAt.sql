CREATE NONCLUSTERED INDEX IX_AtomRelation_TenantId_CreatedAt
ON dbo.AtomRelation(TenantId, CreatedAt DESC)
INCLUDE (AtomRelationId, SourceAtomId, TargetAtomId, RelationType);
