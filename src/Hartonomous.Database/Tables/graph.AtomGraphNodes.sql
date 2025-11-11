CREATE TABLE graph.AtomGraphNodes (
    NodeId BIGINT IDENTITY(1,1) NOT NULL,
    AtomId BIGINT NOT NULL,
    NodeType NVARCHAR(50) NOT NULL,
    Metadata JSON,
    EmbeddingX FLOAT NULL,
    EmbeddingY FLOAT NULL,
    EmbeddingZ FLOAT NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AtomGraphNodes PRIMARY KEY (NodeId),
    CONSTRAINT CK_AtomGraphNodes_NodeType CHECK (NodeType IN ('Atom', 'Model', 'Concept', 'Component', 'Embedding')),
    INDEX IX_AtomGraphNodes_AtomId (AtomId),
    INDEX IX_AtomGraphNodes_NodeType (NodeType)
) AS NODE;