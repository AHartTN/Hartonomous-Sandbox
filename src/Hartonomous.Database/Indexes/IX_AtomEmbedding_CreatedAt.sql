CREATE NONCLUSTERED INDEX IX_AtomEmbedding_CreatedAt
ON dbo.AtomEmbedding(CreatedAt DESC)
INCLUDE (AtomEmbeddingId, AtomId, ModelId, EmbeddingType);
