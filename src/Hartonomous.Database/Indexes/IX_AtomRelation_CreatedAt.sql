CREATE NONCLUSTERED INDEX IX_AtomRelation_CreatedAt
ON dbo.AtomRelation(CreatedAt DESC)
INCLUDE (SourceAtomId, TargetAtomId, RelationType, Weight);
