CREATE NONCLUSTERED INDEX IX_Atom_TenantId_Modality_CreatedAt
ON dbo.Atom(TenantId, Modality, CreatedAt DESC)
INCLUDE (AtomId, ContentHash, Subtype);
