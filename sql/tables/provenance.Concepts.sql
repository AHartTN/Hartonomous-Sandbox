-- provenance.Concepts Table
-- Stores discovered semantic concepts from unsupervised clustering
-- Centroids enable concept-based semantic search and categorization

CREATE TABLE provenance.Concepts (
    ConceptId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConceptName NVARCHAR(200) NULL, -- User-assigned label (initially NULL, can be populated via UI)
    Centroid VARBINARY(MAX) NOT NULL, -- Embedding vector representing concept centroid
    AtomCount INT NOT NULL DEFAULT 0, -- Number of atoms bound to this concept
    Coherence FLOAT NOT NULL, -- Cluster tightness metric (0.0-1.0, higher = better)
    SpatialBucket INT NULL, -- Representative spatial bucket for fast filtering
    DiscoveredUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastUpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TenantId INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1, -- Soft delete flag
    
    INDEX IX_Concepts_TenantId_IsActive NONCLUSTERED (TenantId, IsActive) INCLUDE (ConceptId, Coherence),
    INDEX IX_Concepts_Coherence NONCLUSTERED (Coherence DESC) WHERE IsActive = 1,
    INDEX IX_Concepts_SpatialBucket NONCLUSTERED (SpatialBucket) WHERE IsActive = 1 AND SpatialBucket IS NOT NULL
);
GO

-- provenance.AtomConcepts Table
-- Many-to-many relationship: Atoms can belong to multiple concepts
-- Multi-label classification with similarity scores

CREATE TABLE provenance.AtomConcepts (
    AtomConceptId BIGINT IDENTITY(1,1) PRIMARY KEY,
    AtomId BIGINT NOT NULL,
    ConceptId UNIQUEIDENTIFIER NOT NULL,
    Similarity FLOAT NOT NULL, -- Cosine similarity to concept centroid (0.0-1.0)
    IsPrimary BIT NOT NULL DEFAULT 0, -- Primary concept for this atom (highest similarity)
    BindingUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TenantId INT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_AtomConcepts_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    CONSTRAINT FK_AtomConcepts_Concepts FOREIGN KEY (ConceptId) REFERENCES provenance.Concepts(ConceptId) ON DELETE CASCADE,
    CONSTRAINT UQ_AtomConcepts_Atom_Concept UNIQUE (AtomId, ConceptId),
    CONSTRAINT CK_AtomConcepts_Similarity CHECK (Similarity >= 0.0 AND Similarity <= 1.0),
    
    INDEX IX_AtomConcepts_AtomId NONCLUSTERED (AtomId) INCLUDE (ConceptId, Similarity, IsPrimary),
    INDEX IX_AtomConcepts_ConceptId NONCLUSTERED (ConceptId) INCLUDE (AtomId, Similarity, IsPrimary),
    INDEX IX_AtomConcepts_Similarity NONCLUSTERED (Similarity DESC) INCLUDE (AtomId, ConceptId),
    INDEX IX_AtomConcepts_TenantId NONCLUSTERED (TenantId) INCLUDE (AtomId, ConceptId)
);
GO

-- provenance.ConceptEvolution Table
-- Tracks concept drift over time (temporal versioning of centroids)
-- Enables concept lifecycle analysis and stability metrics

CREATE TABLE provenance.ConceptEvolution (
    EvolutionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ConceptId UNIQUEIDENTIFIER NOT NULL,
    PreviousCentroid VARBINARY(MAX) NULL,
    NewCentroid VARBINARY(MAX) NOT NULL,
    CentroidShift FLOAT NULL, -- Cosine distance between old and new centroid
    AtomCountDelta INT NULL, -- Change in atoms bound to concept
    CoherenceDelta FLOAT NULL, -- Change in cluster tightness
    EvolutionType NVARCHAR(50) NOT NULL, -- 'Discovered', 'Updated', 'Split', 'Merged', 'Archived'
    UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TenantId INT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_ConceptEvolution_Concepts FOREIGN KEY (ConceptId) REFERENCES provenance.Concepts(ConceptId) ON DELETE CASCADE,
    CONSTRAINT CK_ConceptEvolution_Type CHECK (EvolutionType IN ('Discovered', 'Updated', 'Split', 'Merged', 'Archived')),
    
    INDEX IX_ConceptEvolution_ConceptId_UpdatedUtc NONCLUSTERED (ConceptId, UpdatedUtc DESC),
    INDEX IX_ConceptEvolution_Type NONCLUSTERED (EvolutionType) INCLUDE (ConceptId, UpdatedUtc),
    INDEX IX_ConceptEvolution_CentroidShift NONCLUSTERED (CentroidShift DESC) WHERE CentroidShift IS NOT NULL
);
GO

-- Trigger: Update LastUpdatedUtc on concept changes
CREATE OR ALTER TRIGGER trg_Concepts_UpdateTimestamp
ON provenance.Concepts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE c
    SET LastUpdatedUtc = SYSUTCDATETIME()
    FROM provenance.Concepts c
    INNER JOIN inserted i ON c.ConceptId = i.ConceptId;
END;
GO

-- Trigger: Track concept evolution on centroid updates
CREATE OR ALTER TRIGGER trg_Concepts_TrackEvolution
ON provenance.Concepts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO provenance.ConceptEvolution (
        ConceptId,
        PreviousCentroid,
        NewCentroid,
        CentroidShift,
        AtomCountDelta,
        CoherenceDelta,
        EvolutionType,
        TenantId
    )
    SELECT 
        i.ConceptId,
        d.Centroid AS PreviousCentroid,
        i.Centroid AS NewCentroid,
        1.0 - VECTOR_DISTANCE('cosine', d.Centroid, i.Centroid) AS CentroidShift,
        i.AtomCount - d.AtomCount AS AtomCountDelta,
        i.Coherence - d.Coherence AS CoherenceDelta,
        'Updated' AS EvolutionType,
        i.TenantId
    FROM inserted i
    INNER JOIN deleted d ON i.ConceptId = d.ConceptId
    WHERE i.Centroid != d.Centroid; -- Only track when centroid actually changes
END;
GO
