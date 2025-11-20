CREATE NONCLUSTERED INDEX IX_TensorAtom_AtomType
ON dbo.TensorAtom(AtomType, ModelId)
INCLUDE (TensorAtomId, AtomId, LayerId);
