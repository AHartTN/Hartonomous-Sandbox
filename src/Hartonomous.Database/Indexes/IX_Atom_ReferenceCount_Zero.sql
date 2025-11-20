CREATE NONCLUSTERED INDEX IX_Atom_ReferenceCount_Zero
ON dbo.Atom(ReferenceCount, CreatedAt DESC)
INCLUDE (AtomId, TenantId, Modality)
WHERE ReferenceCount = 0;
