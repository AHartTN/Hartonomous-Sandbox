CREATE NONCLUSTERED INDEX IX_TensorAtomCoefficient_ModelId_LayerIdx
ON dbo.TensorAtomCoefficient(ModelId, LayerIdx)
INCLUDE (TensorAtomId, PositionX, PositionY, PositionZ);
