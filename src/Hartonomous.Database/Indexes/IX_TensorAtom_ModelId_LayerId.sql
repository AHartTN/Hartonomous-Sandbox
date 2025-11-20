CREATE NONCLUSTERED INDEX IX_TensorAtom_ModelId_LayerId
ON dbo.TensorAtom(ModelId, LayerId)
INCLUDE (TensorAtomId, AtomId, AtomType, ImportanceScore);
