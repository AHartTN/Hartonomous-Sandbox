CREATE NONCLUSTERED INDEX IX_Atom_CreatedAt
ON dbo.Atom(CreatedAt DESC)
INCLUDE (AtomId, Modality, TenantId, ContentHash);
