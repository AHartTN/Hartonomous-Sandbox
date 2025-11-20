CREATE NONCLUSTERED INDEX IX_AtomEmbedding_AtomId_ModelId
ON dbo.AtomEmbedding(AtomId, ModelId)
INCLUDE (EmbeddingType, Dimension, SpatialKey);
