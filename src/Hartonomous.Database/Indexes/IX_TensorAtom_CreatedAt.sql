CREATE NONCLUSTERED INDEX IX_TensorAtom_CreatedAt
ON dbo.TensorAtom(CreatedAt DESC)
INCLUDE (TensorAtomId, AtomId, ModelId, LayerId);
