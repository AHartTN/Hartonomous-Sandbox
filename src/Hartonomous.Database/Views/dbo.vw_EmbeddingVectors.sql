CREATE VIEW dbo.vw_EmbeddingVectors
WITH SCHEMABINDING
AS
SELECT 
    relations.SourceAtomId,
    relations.SequenceIndex AS ComponentIndex,
    dbo.clr_BinaryToFloat(atoms.AtomicValue) AS ComponentValue,
    relations.AtomRelationId
FROM dbo.AtomRelations AS relations
INNER JOIN dbo.Atoms AS atoms ON atoms.AtomId = relations.TargetAtomId
WHERE relations.RelationType = 'embedding_dimension';
