CREATE NONCLUSTERED INDEX IX_AtomRelation_RelationType_Weight
ON dbo.AtomRelation(RelationType, Weight DESC)
INCLUDE (SourceAtomId, TargetAtomId, Confidence, Importance);
