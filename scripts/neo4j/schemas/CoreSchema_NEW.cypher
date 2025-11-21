// =============================================
// HARTONOMOUS NEO4J COMPLETE PROVENANCE & GOVERNANCE SCHEMA
// Full data lineage, auditability, explainability, and compliance tracking
// =============================================
// 
// PURPOSE: This schema provides comprehensive tracking of:
// 1. Atom Provenance - Complete lineage from source to derived atoms
// 2. Transformation Tracking - How atoms are processed, combined, chunked
// 3. Model Inference Audit - AI model decisions and explainability
// 4. Governance & Compliance - User actions, access control, data classification
// 5. Temporal Tracking - Historical evolution of atoms and models
// 6. Quality Metrics - Validation, confidence, error tracking
//
// =============================================

// =============================================
// SECTION 1: ATOM PROVENANCE & DATA LINEAGE
// =============================================

// --- Atom Nodes ---
// Core atomic unit of data in Hartonomous
CREATE CONSTRAINT atom_id_tenant_unique IF NOT EXISTS
FOR (a:Atom) REQUIRE (a.id, a.tenantId) IS UNIQUE;

CREATE INDEX atom_id IF NOT EXISTS
FOR (a:Atom) ON (a.id);

CREATE INDEX atom_tenant IF NOT EXISTS
FOR (a:Atom) ON (a.tenantId);

CREATE INDEX atom_content_hash IF NOT EXISTS
FOR (a:Atom) ON (a.contentHash);

CREATE INDEX atom_created IF NOT EXISTS
FOR (a:Atom) ON (a.createdAt);

CREATE INDEX atom_sync_status IF NOT EXISTS
FOR (a:Atom) ON (a.syncStatus);

// Property schema for :Atom nodes:
// - id: BIGINT - SQL database primary key
// - tenantId: INT - Multi-tenant isolation
// - contentHash: STRING - SHA256 hash for deduplication
// - atomType: STRING - 'chunk', 'embedding', 'document', 'composite'
// - sourceType: STRING - 'ingestion', 'transformation', 'inference', 'user_input'
// - content: STRING - Actual content (may be truncated for large atoms)
// - metadata: STRING (JSON) - Additional properties
// - syncType: STRING - 'CREATE', 'UPDATE', 'DELETE'
// - syncStatus: STRING - 'pending', 'synced', 'error'
// - lastSynced: DATETIME - Last Neo4j sync timestamp
// - createdAt: DATETIME - Original creation time
// - modifiedAt: DATETIME - Last modification time

// --- Tenant Nodes ---
CREATE CONSTRAINT tenant_id_unique IF NOT EXISTS
FOR (t:Tenant) REQUIRE t.id IS UNIQUE;

CREATE INDEX tenant_name IF NOT EXISTS
FOR (t:Tenant) ON (t.name);

// Property schema for :Tenant:
// - id: INT
// - name: STRING
// - tier: STRING - 'free', 'pro', 'enterprise'
// - createdAt: DATETIME
// - status: STRING - 'active', 'suspended', 'deleted'

// --- User Nodes ---
CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.id IS UNIQUE;

CREATE INDEX user_email IF NOT EXISTS
FOR (u:User) ON (u.email);

CREATE INDEX user_tenant IF NOT EXISTS
FOR (u:User) ON (u.tenantId);

// Property schema for :User:
// - id: STRING (GUID)
// - email: STRING
// - tenantId: INT
// - roles: LIST<STRING> - ['admin', 'analyst', 'viewer']
// - createdAt: DATETIME
// - lastLogin: DATETIME

// --- Document Nodes ---
CREATE CONSTRAINT document_id_tenant_unique IF NOT EXISTS
FOR (d:Document) REQUIRE (d.id, d.tenantId) IS UNIQUE;

CREATE INDEX document_filename IF NOT EXISTS
FOR (d:Document) ON (d.filename);

CREATE INDEX document_content_type IF NOT EXISTS
FOR (d:Document) ON (d.contentType);

// Property schema for :Document:
// - id: BIGINT
// - tenantId: INT
// - filename: STRING
// - contentType: STRING - 'text/plain', 'application/pdf', etc.
// - sourceUrl: STRING
// - uploadedBy: STRING (user ID)
// - uploadedAt: DATETIME
// - sizeBytes: LONG
// - status: STRING - 'processing', 'ready', 'error'

// =============================================
// SECTION 2: ATOM RELATIONSHIPS (PROVENANCE)
// =============================================

// --- DERIVED_FROM ---
// Core provenance relationship: childAtom DERIVED_FROM parentAtom
// Used for: chunking, embedding generation, transformations
CREATE INDEX derived_from_type IF NOT EXISTS
FOR ()-[r:DERIVED_FROM]-() ON (r.derivationType);

CREATE INDEX derived_from_timestamp IF NOT EXISTS
FOR ()-[r:DERIVED_FROM]-() ON (r.timestamp);

// Relationship properties:
// - derivationType: STRING - 'chunked', 'embedded', 'summarized', 'transformed', 'merged'
// - operationId: STRING (GUID) - Links to SQL OperationProvenance
// - confidence: FLOAT - Confidence in the derivation (0.0-1.0)
// - timestamp: DATETIME
// - metadata: STRING (JSON) - Operation-specific details

// --- MERGED_FROM ---
// Multiple atoms merged into one composite atom
CREATE INDEX merged_from_timestamp IF NOT EXISTS
FOR ()-[r:MERGED_FROM]-() ON (r.timestamp);

// Relationship properties:
// - mergeStrategy: STRING - 'concatenate', 'interleave', 'weighted_avg'
// - weight: FLOAT - Contribution weight of source atom (0.0-1.0)
// - sequenceIndex: INT - Order in merge operation
// - timestamp: DATETIME

// --- BELONGS_TO_DOCUMENT ---
// Atom belongs to a source document
// Relationship properties:
// - chunkIndex: INT - Position within document
// - startOffset: INT - Character offset start
// - endOffset: INT - Character offset end

// --- BELONGS_TO_TENANT ---
// Atom/Document/Model belongs to tenant (for multi-tenancy queries)

// --- CREATED_BY ---
// Atom/Document created by user (for access audit)
// Relationship properties:
// - operation: STRING - 'upload', 'ingest', 'transform'
// - timestamp: DATETIME

// --- ACCESSED_BY ---
// User accessed atom (for governance tracking)
CREATE INDEX accessed_timestamp IF NOT EXISTS
FOR ()-[r:ACCESSED_BY]-() ON (r.timestamp);

// Relationship properties:
// - operation: STRING - 'read', 'update', 'delete', 'query'
// - timestamp: DATETIME
// - ipAddress: STRING
// - userAgent: STRING

// =============================================
// SECTION 3: TRANSFORMATION & OPERATION TRACKING
// =============================================

// --- Operation Nodes ---
CREATE CONSTRAINT operation_id_unique IF NOT EXISTS
FOR (o:Operation) REQUIRE o.id IS UNIQUE;

CREATE INDEX operation_type IF NOT EXISTS
FOR (o:Operation) ON (o.type);

CREATE INDEX operation_status IF NOT EXISTS
FOR (o:Operation) ON (o.status);

CREATE INDEX operation_timestamp IF NOT EXISTS
FOR (o:Operation) ON (o.timestamp);

// Property schema for :Operation:
// - id: STRING (GUID) - Maps to SQL OperationProvenance.OperationId
// - type: STRING - 'chunking', 'embedding', 'inference', 'merge', 'validation'
// - status: STRING - 'running', 'completed', 'failed', 'cancelled'
// - timestamp: DATETIME
// - durationMs: INT
// - tenantId: INT
// - userId: STRING
// - errorMessage: STRING - If failed
// - metadata: STRING (JSON)

// --- Operation Relationships ---
// (:Operation)-[:INPUT_ATOM]->(:Atom) - Input atoms
// (:Operation)-[:OUTPUT_ATOM]->(:Atom) - Output atoms
// (:Operation)-[:EXECUTED_BY]->(:User)
// (:Operation)-[:FAILED_DUE_TO]->(:Error)

CREATE INDEX operation_input_timestamp IF NOT EXISTS
FOR ()-[r:INPUT_ATOM]-() ON (r.timestamp);

CREATE INDEX operation_output_timestamp IF NOT EXISTS
FOR ()-[r:OUTPUT_ATOM]-() ON (r.timestamp);

// =============================================
// SECTION 4: MODEL INFERENCE & EXPLAINABILITY
// =============================================

// --- Inference Nodes ---
CREATE CONSTRAINT inference_id_unique IF NOT EXISTS
FOR (i:Inference) REQUIRE i.id IS UNIQUE;

CREATE INDEX inference_timestamp IF NOT EXISTS
FOR (i:Inference) ON (i.timestamp);

CREATE INDEX inference_task IF NOT EXISTS
FOR (i:Inference) ON (i.taskType);

CREATE INDEX inference_confidence IF NOT EXISTS
FOR (i:Inference) ON (i.confidence);

// Property schema for :Inference:
// - id: STRING (GUID) - Inference ID
// - taskType: STRING - 'text-generation', 'embedding', 'classification', 'rag'
// - prompt: STRING - Input prompt
// - confidence: FLOAT - Overall confidence (0.0-1.0)
// - timestamp: DATETIME
// - durationMs: INT
// - tenantId: INT
// - userId: STRING
// - modelIds: LIST<INT> - Models used
// - metadata: STRING (JSON)

// --- Model Nodes ---
CREATE CONSTRAINT model_id_unique IF NOT EXISTS
FOR (m:Model) REQUIRE m.id IS UNIQUE;

CREATE INDEX model_name IF NOT EXISTS
FOR (m:Model) ON (m.name);

CREATE INDEX model_type IF NOT EXISTS
FOR (m:Model) ON (m.type);

// Property schema for :Model:
// - id: INT - SQL Model.ModelId
// - name: STRING - 'bert-base', 'gpt-4', 'llama-7b'
// - type: STRING - 'transformer', 'embedding', 'classifier'
// - version: STRING - Semantic version
// - provider: STRING - 'openai', 'huggingface', 'local'
// - createdAt: DATETIME
// - status: STRING - 'active', 'deprecated', 'retired'

// --- ModelVersion Nodes ---
CREATE CONSTRAINT model_version_unique IF NOT EXISTS
FOR (mv:ModelVersion) REQUIRE (mv.modelId, mv.version) IS UNIQUE;

CREATE INDEX model_version_deployed IF NOT EXISTS
FOR (mv:ModelVersion) ON (mv.deployedAt);

// Property schema for :ModelVersion:
// - modelId: INT
// - version: STRING - 'v1.0', 'checkpoint-2024-11-20'
// - deployedAt: DATETIME
// - retiredAt: DATETIME
// - performanceScore: FLOAT
// - metadata: STRING (JSON)

// --- Decision Nodes ---
CREATE CONSTRAINT decision_id_unique IF NOT EXISTS
FOR (d:Decision) REQUIRE d.id IS UNIQUE;

CREATE INDEX decision_confidence IF NOT EXISTS
FOR (d:Decision) ON (d.confidence);

// Property schema for :Decision:
// - id: STRING (GUID)
// - outputText: STRING - Generated output
// - confidence: FLOAT
// - tokenCount: INT
// - alternatives: LIST<STRING> - Alternative outputs considered

// --- Evidence Nodes ---
CREATE CONSTRAINT evidence_id_unique IF NOT EXISTS
FOR (e:Evidence) REQUIRE e.id IS UNIQUE;

CREATE INDEX evidence_type IF NOT EXISTS
FOR (e:Evidence) ON (e.type);

// Property schema for :Evidence:
// - id: STRING (GUID)
// - type: STRING - 'vector_similarity', 'prior_inference', 'knowledge_base'
// - source: STRING - Source identifier
// - similarityScore: FLOAT
// - relevanceScore: FLOAT
// - content: STRING - Evidence content

// --- ReasoningMode Nodes ---
CREATE CONSTRAINT reasoning_mode_type_unique IF NOT EXISTS
FOR (rm:ReasoningMode) REQUIRE rm.type IS UNIQUE;

// Property schema for :ReasoningMode:
// - type: STRING - 'vector_similarity', 'spatial_query', 'graph_traversal', 'hybrid', 'symbolic_logic'
// - description: STRING
// - created: DATETIME

// --- Context Nodes ---
CREATE INDEX context_domain IF NOT EXISTS
FOR (c:Context) ON (c.domain);

// Property schema for :Context:
// - domain: STRING - 'text_generation', 'image_generation', etc.
// - userLevel: STRING - 'beginner', 'expert'
// - language: STRING - 'en', 'es', etc.
// - timeOfDay: STRING

// --- Inference Relationships ---
// (:Inference)-[:USED_MODEL {weight, contribution, confidence}]->(:Model)
// (:Inference)-[:USED_REASONING {weight, numOperations, durationMs}]->(:ReasoningMode)
// (:Inference)-[:RESULTED_IN]->(:Decision)
// (:Inference)-[:CONSIDERED_ALTERNATIVE]->(:Alternative)
// (:Inference)-[:IN_CONTEXT]->(:Context)
// (:Inference)-[:INFLUENCED_BY {strength}]->(:Inference) - Causal chains
// (:Inference)-[:REQUESTED_BY]->(:User)
// (:Inference)-[:USED_ATOM]->(:Atom) - Atoms used as context/input

CREATE INDEX used_model_weight IF NOT EXISTS
FOR ()-[r:USED_MODEL]-() ON (r.weight);

CREATE INDEX used_reasoning_weight IF NOT EXISTS
FOR ()-[r:USED_REASONING]-() ON (r.weight);

// (:Decision)-[:SUPPORTED_BY {strength}]->(:Evidence)
// (:Decision)-[:RATED_BY]->(:Feedback)
// (:Decision)-[:PRODUCED_ATOM]->(:Atom) - Atom generated from decision

// (:Model)-[:VERSION]->(:ModelVersion)
// (:ModelVersion)-[:EVOLVED_TO {reason, improvement}]->(:ModelVersion)
// (:Model)-[:PERFORMS_WELL_IN]->(:Context)
// (:Model)-[:PERFORMS_POORLY_IN]->(:Context)

// (:Evidence)-[:REFERENCES_ATOM]->(:Atom) - Evidence sourced from atom

// =============================================
// SECTION 5: ERROR TRACKING & QUALITY METRICS
// =============================================

// --- Error Nodes ---
CREATE CONSTRAINT error_id_unique IF NOT EXISTS
FOR (e:Error) REQUIRE e.id IS UNIQUE;

CREATE INDEX error_type IF NOT EXISTS
FOR (e:Error) ON (e.type);

CREATE INDEX error_timestamp IF NOT EXISTS
FOR (e:Error) ON (e.timestamp);

// Property schema for :Error:
// - id: STRING (GUID)
// - type: STRING - 'validation_failure', 'transformation_error', 'inference_failure'
// - message: STRING
// - stackTrace: STRING
// - timestamp: DATETIME
// - severity: STRING - 'warning', 'error', 'critical'
// - resolved: BOOLEAN
// - resolvedAt: DATETIME

// --- Validation Nodes ---
CREATE INDEX validation_status IF NOT EXISTS
FOR (v:Validation) ON (v.status);

CREATE INDEX validation_timestamp IF NOT EXISTS
FOR (v:Validation) ON (v.timestamp);

// Property schema for :Validation:
// - id: STRING (GUID)
// - validationType: STRING - 'schema', 'content', 'quality'
// - status: STRING - 'passed', 'failed', 'warning'
// - score: FLOAT - Quality score (0.0-1.0)
// - timestamp: DATETIME
// - rules: LIST<STRING> - Validation rules applied
// - violations: LIST<STRING> - Rules violated

// --- Error Relationships ---
// (:Operation)-[:FAILED_WITH]->(:Error)
// (:Atom)-[:FAILED_VALIDATION]->(:Validation)
// (:Error)-[:AFFECTED_ATOM]->(:Atom)
// (:Error)-[:OCCURRED_IN_OPERATION]->(:Operation)
// (:Error)-[:SIMILAR_TO]->(:Error) - Error clustering

CREATE INDEX error_similarity IF NOT EXISTS
FOR ()-[r:SIMILAR_TO]-() ON (r.similarity);

// Relationship properties for :SIMILAR_TO:
// - similarity: FLOAT - Cosine similarity of error messages (0.0-1.0)
// - clusteringMethod: STRING - 'embedding', 'text_match'

// =============================================
// SECTION 6: TEMPORAL TRACKING & VERSIONING
// =============================================

// --- AtomVersion Nodes ---
CREATE CONSTRAINT atom_version_unique IF NOT EXISTS
FOR (av:AtomVersion) REQUIRE (av.atomId, av.version) IS UNIQUE;

CREATE INDEX atom_version_timestamp IF NOT EXISTS
FOR (av:AtomVersion) ON (av.timestamp);

// Property schema for :AtomVersion:
// - atomId: BIGINT
// - version: INT - Version number (1, 2, 3...)
// - content: STRING - Content at this version
// - contentHash: STRING
// - timestamp: DATETIME
// - changeType: STRING - 'created', 'updated', 'deleted'
// - changedBy: STRING (user ID)

// --- Temporal Relationships ---
// (:Atom)-[:HAS_VERSION]->(:AtomVersion)
// (:AtomVersion)-[:SUPERSEDED_BY]->(:AtomVersion)
// (:AtomVersion)-[:MODIFIED_BY]->(:User)

// =============================================
// SECTION 7: DATA CLASSIFICATION & GOVERNANCE
// =============================================

// --- DataClassification Nodes ---
CREATE CONSTRAINT classification_name_unique IF NOT EXISTS
FOR (dc:DataClassification) REQUIRE dc.name IS UNIQUE;

// Property schema for :DataClassification:
// - name: STRING - 'PII', 'PHI', 'Financial', 'Public', 'Internal', 'Confidential', 'Restricted'
// - description: STRING
// - retentionDays: INT - Data retention policy
// - accessLevel: STRING - 'public', 'internal', 'confidential', 'restricted'
// - complianceFrameworks: LIST<STRING> - ['GDPR', 'HIPAA', 'SOX', 'CCPA']

// --- ComplianceRule Nodes ---
CREATE INDEX compliance_framework IF NOT EXISTS
FOR (cr:ComplianceRule) ON (cr.framework);

// Property schema for :ComplianceRule:
// - id: STRING
// - framework: STRING - 'GDPR', 'HIPAA', etc.
// - ruleId: STRING - 'GDPR-Article-17', 'HIPAA-164.308'
// - description: STRING
// - enforcementLevel: STRING - 'required', 'recommended'

// --- AuditLog Nodes ---
CREATE INDEX audit_log_timestamp IF NOT EXISTS
FOR (al:AuditLog) ON (al.timestamp);

CREATE INDEX audit_log_action IF NOT EXISTS
FOR (al:AuditLog) ON (al.action);

// Property schema for :AuditLog:
// - id: STRING (GUID)
// - action: STRING - 'access', 'modify', 'delete', 'export', 'query'
// - resourceType: STRING - 'atom', 'document', 'model', 'inference'
// - resourceId: STRING
// - userId: STRING
// - tenantId: INT
// - timestamp: DATETIME
// - ipAddress: STRING
// - success: BOOLEAN
// - details: STRING (JSON)

// --- Governance Relationships ---
// (:Atom)-[:CLASSIFIED_AS]->(:DataClassification)
// (:Document)-[:CLASSIFIED_AS]->(:DataClassification)
// (:DataClassification)-[:REQUIRES_COMPLIANCE]->(:ComplianceRule)
// (:User)-[:HAS_ROLE]->(:Role)
// (:Role)-[:CAN_ACCESS]->(:DataClassification)
// (:AuditLog)-[:LOGGED_ACTION_ON_ATOM]->(:Atom)
// (:AuditLog)-[:LOGGED_ACTION_BY_USER]->(:User)

// =============================================
// SECTION 8: FEEDBACK & CONTINUOUS IMPROVEMENT
// =============================================

// --- Feedback Nodes ---
CREATE INDEX feedback_rating IF NOT EXISTS
FOR (f:Feedback) ON (f.rating);

CREATE INDEX feedback_timestamp IF NOT EXISTS
FOR (f:Feedback) ON (f.timestamp);

// Property schema for :Feedback:
// - id: STRING (GUID)
// - rating: INT - 1-5 star rating
// - comment: STRING
// - timestamp: DATETIME
// - feedbackType: STRING - 'accuracy', 'relevance', 'quality', 'speed'
// - resolved: BOOLEAN

// --- Feedback Relationships ---
// (:Decision)-[:RATED_BY]->(:Feedback)
// (:Feedback)-[:PROVIDED_BY]->(:User)
// (:Feedback)-[:ABOUT_MODEL]->(:Model)
// (:Feedback)-[:LED_TO_IMPROVEMENT]->(:ModelVersion) - Feedback drove model update

// =============================================
// SECTION 9: INITIALIZATION - REFERENCE DATA
// =============================================

// Create common reasoning mode nodes (persist across inferences)
MERGE (vector:ReasoningMode {type: 'vector_similarity'})
ON CREATE SET vector.description = 'Semantic similarity via vector embeddings and DiskANN',
              vector.created = datetime();

MERGE (spatial:ReasoningMode {type: 'spatial_query'})
ON CREATE SET spatial.description = 'Geometric reasoning via spatial indexes and operations',
              spatial.created = datetime();

MERGE (graph:ReasoningMode {type: 'graph_traversal'})
ON CREATE SET graph.description = 'Symbolic/causal reasoning via graph pattern matching',
              graph.created = datetime();

MERGE (hybrid:ReasoningMode {type: 'hybrid'})
ON CREATE SET hybrid.description = 'Combined multi-modal reasoning',
              hybrid.created = datetime();

MERGE (symbolic:ReasoningMode {type: 'symbolic_logic'})
ON CREATE SET symbolic.description = 'Classical logical inference via SQL predicates',
              symbolic.created = datetime();

// Create common context categories
MERGE (ctx_text:Context {domain: 'text_generation'});
MERGE (ctx_image:Context {domain: 'image_generation'});
MERGE (ctx_audio:Context {domain: 'audio_generation'});
MERGE (ctx_multimodal:Context {domain: 'multimodal'});
MERGE (ctx_embedding:Context {domain: 'embedding_generation'});
MERGE (ctx_search:Context {domain: 'semantic_search'});

// Create standard data classifications
MERGE (pii:DataClassification {name: 'PII'})
ON CREATE SET pii.description = 'Personally Identifiable Information',
              pii.retentionDays = 2555, -- 7 years
              pii.accessLevel = 'restricted',
              pii.complianceFrameworks = ['GDPR', 'CCPA'];

MERGE (phi:DataClassification {name: 'PHI'})
ON CREATE SET phi.description = 'Protected Health Information',
              phi.retentionDays = 2555,
              phi.accessLevel = 'restricted',
              phi.complianceFrameworks = ['HIPAA'];

MERGE (financial:DataClassification {name: 'Financial'})
ON CREATE SET financial.description = 'Financial Data',
              financial.retentionDays = 2555,
              financial.accessLevel = 'confidential',
              financial.complianceFrameworks = ['SOX', 'PCI-DSS'];

MERGE (public:DataClassification {name: 'Public'})
ON CREATE SET public.description = 'Publicly Available Data',
              public.retentionDays = 3650, -- 10 years
              public.accessLevel = 'public',
              public.complianceFrameworks = [];

MERGE (internal:DataClassification {name: 'Internal'})
ON CREATE SET internal.description = 'Internal Use Only',
              internal.retentionDays = 1825, -- 5 years
              internal.accessLevel = 'internal',
              internal.complianceFrameworks = [];

MERGE (confidential:DataClassification {name: 'Confidential'})
ON CREATE SET confidential.description = 'Confidential Business Data',
              confidential.retentionDays = 2555,
              confidential.accessLevel = 'confidential',
              confidential.complianceFrameworks = ['SOX'];

RETURN 'Neo4j schema initialized successfully - Full provenance, governance, and auditability' as status;
