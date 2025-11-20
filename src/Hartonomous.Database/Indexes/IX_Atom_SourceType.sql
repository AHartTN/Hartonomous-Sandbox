CREATE NONCLUSTERED INDEX IX_Atom_SourceType
ON dbo.Atom(SourceType, Modality)
INCLUDE (AtomId, TenantId, ContentHash)
WHERE SourceType IS NOT NULL;
