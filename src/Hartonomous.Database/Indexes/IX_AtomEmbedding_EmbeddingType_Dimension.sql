CREATE NONCLUSTERED INDEX IX_AtomEmbedding_EmbeddingType_Dimension
ON dbo.AtomEmbedding(EmbeddingType, Dimension, ModelId)
INCLUDE (AtomEmbeddingId, AtomId, TenantId);
