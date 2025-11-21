// =============================================
// HARTONOMOUS NEO4J PROVENANCE QUERY LIBRARY
// Comprehensive queries for lineage, governance, auditability, and analytics
// =============================================

// =============================================
// SECTION 1: ATOM LINEAGE QUERIES
// =============================================

// Q1.1: Get complete upstream lineage for an atom (ancestors)
// Purpose: Trace data back to its source
// Use case: Data lineage audit, compliance verification
MATCH path = (atom:Atom {id: $atomId, tenantId: $tenantId})-[:DERIVED_FROM*]->(ancestor:Atom)
RETURN 
    ancestor.id as atomId,
    ancestor.atomType as atomType,
    ancestor.sourceType as sourceType,
    ancestor.createdAt as createdAt,
    length(path) as depth,
    [rel in relationships(path) | rel.derivationType] as derivationChain
ORDER BY depth ASC;

// Q1.2: Get complete downstream lineage for an atom (descendants)
// Purpose: Impact analysis - what depends on this atom
// Use case: Change impact assessment, deletion risk analysis
MATCH path = (atom:Atom {id: $atomId, tenantId: $tenantId})<-[:DERIVED_FROM*]-(descendant:Atom)
RETURN 
    descendant.id as atomId,
    descendant.atomType as atomType,
    descendant.syncStatus as syncStatus,
    length(path) as depth,
    count(descendant) as totalDescendants
ORDER BY depth ASC;

// Q1.3: Find root source atoms (no parents)
// Purpose: Identify original data sources
// Use case: Data origin audit, source validation
MATCH (atom:Atom {tenantId: $tenantId})
WHERE NOT (atom)-[:DERIVED_FROM]->()
RETURN 
    atom.id as atomId,
    atom.sourceType as sourceType,
    atom.createdAt as createdAt,
    atom.contentHash as contentHash
ORDER BY atom.createdAt DESC
LIMIT 100;

// Q1.4: Find leaf atoms (no children)
// Purpose: Identify final derived outputs
// Use case: Result validation, output tracking
MATCH (atom:Atom {tenantId: $tenantId})
WHERE NOT ()-[:DERIVED_FROM]->(atom)
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.createdAt as createdAt
ORDER BY atom.createdAt DESC
LIMIT 100;

// Q1.5: Get full lineage tree with all relationships
// Purpose: Complete provenance visualization
// Use case: Compliance reports, debugging complex transformations
MATCH path = (root:Atom {id: $rootAtomId, tenantId: $tenantId})-[:DERIVED_FROM*0..10]-(related:Atom)
RETURN path
LIMIT 500;

// Q1.6: Find atoms derived from multiple sources (merge operations)
// Purpose: Track data fusion operations
// Use case: Data quality verification, merge audit
MATCH (atom:Atom {tenantId: $tenantId})-[r:MERGED_FROM]->(source:Atom)
WITH atom, count(source) as numSources, collect(source.id) as sourceIds
WHERE numSources > 1
RETURN 
    atom.id as atomId,
    numSources,
    sourceIds,
    atom.createdAt as createdAt
ORDER BY numSources DESC;

// Q1.7: Get lineage with transformation operations
// Purpose: Detailed transformation audit
// Use case: Process validation, operation tracking
MATCH (atom:Atom {id: $atomId, tenantId: $tenantId})-[:DERIVED_FROM]->(parent:Atom)
OPTIONAL MATCH (op:Operation)-[:INPUT_ATOM]->(parent)
OPTIONAL MATCH (op)-[:OUTPUT_ATOM]->(atom)
RETURN 
    parent.id as parentAtomId,
    atom.id as childAtomId,
    op.id as operationId,
    op.type as operationType,
    op.timestamp as timestamp,
    op.durationMs as durationMs,
    op.status as status
ORDER BY op.timestamp DESC;

// Q1.8: Find atoms by content hash (deduplication check)
// Purpose: Identify duplicate content across lineage
// Use case: Storage optimization, duplicate detection
MATCH (atom:Atom {contentHash: $contentHash, tenantId: $tenantId})
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.sourceType as sourceType,
    atom.createdAt as createdAt,
    count(*) as duplicateCount;

// Q1.9: Get lineage depth statistics
// Purpose: Understand transformation complexity
// Use case: Process optimization, complexity analysis
MATCH (atom:Atom {tenantId: $tenantId})
OPTIONAL MATCH path = (atom)-[:DERIVED_FROM*]->(ancestor)
WITH atom, max(length(path)) as maxDepth
RETURN 
    maxDepth,
    count(atom) as atomCount
ORDER BY maxDepth DESC;

// Q1.10: Find atoms with circular dependencies (should be zero)
// Purpose: Data quality check
// Use case: Integrity validation, error detection
MATCH path = (atom:Atom {tenantId: $tenantId})-[:DERIVED_FROM*1..20]->(atom)
RETURN 
    atom.id as atomId,
    length(path) as circularPathLength,
    [n in nodes(path) | n.id] as pathAtomIds;

// =============================================
// SECTION 2: DOCUMENT & CHUNK TRACKING
// =============================================

// Q2.1: Get all atoms (chunks) belonging to a document
// Purpose: Document content reconstruction
// Use case: Document retrieval, content validation
MATCH (doc:Document {id: $documentId, tenantId: $tenantId})<-[r:BELONGS_TO_DOCUMENT]-(atom:Atom)
RETURN 
    atom.id as atomId,
    atom.content as content,
    r.chunkIndex as chunkIndex,
    r.startOffset as startOffset,
    r.endOffset as endOffset
ORDER BY r.chunkIndex ASC;

// Q2.2: Get document with statistics
// Purpose: Document overview and metadata
// Use case: Document management, status tracking
MATCH (doc:Document {id: $documentId, tenantId: $tenantId})
OPTIONAL MATCH (doc)<-[:BELONGS_TO_DOCUMENT]-(atom:Atom)
RETURN 
    doc.id as documentId,
    doc.filename as filename,
    doc.contentType as contentType,
    doc.sizeBytes as sizeBytes,
    doc.status as status,
    doc.uploadedAt as uploadedAt,
    count(atom) as numChunks;

// Q2.3: Find documents uploaded by user
// Purpose: User activity tracking
// Use case: Access audit, user management
MATCH (user:User {id: $userId})<-[:CREATED_BY]-(doc:Document {tenantId: $tenantId})
RETURN 
    doc.id as documentId,
    doc.filename as filename,
    doc.uploadedAt as uploadedAt,
    doc.status as status
ORDER BY doc.uploadedAt DESC;

// Q2.4: Get document processing status
// Purpose: Track document ingestion progress
// Use case: Pipeline monitoring, error detection
MATCH (doc:Document {tenantId: $tenantId})
OPTIONAL MATCH (doc)<-[:BELONGS_TO_DOCUMENT]-(atom:Atom)
RETURN 
    doc.id as documentId,
    doc.filename as filename,
    doc.status as docStatus,
    count(atom) as chunksCreated,
    doc.uploadedAt as uploadedAt
ORDER BY doc.uploadedAt DESC
LIMIT 100;

// =============================================
// SECTION 3: OPERATION & TRANSFORMATION QUERIES
// =============================================

// Q3.1: Get operation details with inputs and outputs
// Purpose: Detailed operation audit
// Use case: Debugging, performance analysis
MATCH (op:Operation {id: $operationId})
OPTIONAL MATCH (op)-[:INPUT_ATOM]->(inputAtom:Atom)
OPTIONAL MATCH (op)-[:OUTPUT_ATOM]->(outputAtom:Atom)
OPTIONAL MATCH (op)-[:EXECUTED_BY]->(user:User)
RETURN 
    op.id as operationId,
    op.type as operationType,
    op.status as status,
    op.timestamp as timestamp,
    op.durationMs as durationMs,
    user.email as executedBy,
    collect(DISTINCT inputAtom.id) as inputAtomIds,
    collect(DISTINCT outputAtom.id) as outputAtomIds;

// Q3.2: Find failed operations
// Purpose: Error tracking and resolution
// Use case: Operations troubleshooting
MATCH (op:Operation {tenantId: $tenantId})
WHERE op.status = 'failed'
OPTIONAL MATCH (op)-[:FAILED_WITH]->(error:Error)
RETURN 
    op.id as operationId,
    op.type as operationType,
    op.timestamp as timestamp,
    op.errorMessage as errorMessage,
    error.message as detailedError,
    error.severity as severity
ORDER BY op.timestamp DESC
LIMIT 50;

// Q3.3: Get operation performance statistics
// Purpose: Performance monitoring
// Use case: Optimization, SLA tracking
MATCH (op:Operation {tenantId: $tenantId})
WHERE op.status = 'completed' AND op.type = $operationType
RETURN 
    op.type as operationType,
    count(op) as totalOperations,
    avg(op.durationMs) as avgDurationMs,
    min(op.durationMs) as minDurationMs,
    max(op.durationMs) as maxDurationMs,
    percentileDisc(op.durationMs, 0.50) as p50DurationMs,
    percentileDisc(op.durationMs, 0.95) as p95DurationMs,
    percentileDisc(op.durationMs, 0.99) as p99DurationMs;

// Q3.4: Find operations in a time range
// Purpose: Temporal analysis
// Use case: Time-based reporting, activity monitoring
MATCH (op:Operation {tenantId: $tenantId})
WHERE op.timestamp >= datetime($startDate) AND op.timestamp <= datetime($endDate)
RETURN 
    op.id as operationId,
    op.type as operationType,
    op.status as status,
    op.timestamp as timestamp,
    op.durationMs as durationMs
ORDER BY op.timestamp DESC;

// Q3.5: Get operation type distribution
// Purpose: Workload analysis
// Use case: Capacity planning, resource allocation
MATCH (op:Operation {tenantId: $tenantId})
WHERE op.timestamp >= datetime() - duration({days: 7})
RETURN 
    op.type as operationType,
    count(op) as operationCount,
    sum(CASE WHEN op.status = 'completed' THEN 1 ELSE 0 END) as completedCount,
    sum(CASE WHEN op.status = 'failed' THEN 1 ELSE 0 END) as failedCount,
    avg(op.durationMs) as avgDurationMs
ORDER BY operationCount DESC;

// =============================================
// SECTION 4: MODEL INFERENCE & EXPLAINABILITY
// =============================================

// Q4.1: Get inference details with model contributions
// Purpose: Explain inference decision
// Use case: AI transparency, debugging
MATCH (inf:Inference {id: $inferenceId})
OPTIONAL MATCH (inf)-[r:USED_MODEL]->(model:Model)
OPTIONAL MATCH (inf)-[:RESULTED_IN]->(decision:Decision)
OPTIONAL MATCH (inf)-[:REQUESTED_BY]->(user:User)
RETURN 
    inf.id as inferenceId,
    inf.taskType as taskType,
    inf.confidence as confidence,
    inf.timestamp as timestamp,
    user.email as requestedBy,
    collect({
        modelName: model.name,
        weight: r.weight,
        confidence: r.confidence,
        durationMs: r.durationMs
    }) as models,
    decision.outputText as output;

// Q4.2: Get evidence supporting a decision
// Purpose: Decision explainability
// Use case: Transparency, compliance
MATCH (inf:Inference {id: $inferenceId})-[:RESULTED_IN]->(decision:Decision)
MATCH (decision)-[r:SUPPORTED_BY]->(evidence:Evidence)
RETURN 
    evidence.type as evidenceType,
    evidence.source as source,
    evidence.similarityScore as similarityScore,
    evidence.content as content,
    r.strength as evidenceStrength
ORDER BY r.strength DESC;

// Q4.3: Get atoms used in inference
// Purpose: Track input data for inference
// Use case: Data lineage for AI outputs
MATCH (inf:Inference {id: $inferenceId})-[:USED_ATOM]->(atom:Atom)
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.content as content,
    atom.createdAt as createdAt;

// Q4.4: Find inferences influenced by prior inferences
// Purpose: Causal chain analysis
// Use case: Understanding inference dependencies
MATCH path = (prior:Inference)-[:INFLUENCED_BY*1..5]->(current:Inference {id: $inferenceId})
RETURN 
    prior.id as priorInferenceId,
    prior.taskType as taskType,
    prior.confidence as confidence,
    length(path) as chainLength,
    [rel in relationships(path) | rel.strength] as influenceStrengths;

// Q4.5: Get model performance by task type
// Purpose: Model selection optimization
// Use case: Model recommendation, performance tracking
MATCH (inf:Inference {tenantId: $tenantId})
WHERE inf.taskType = $taskType AND inf.confidence > 0.8
MATCH (inf)-[r:USED_MODEL]->(model:Model)
RETURN 
    model.name as modelName,
    model.type as modelType,
    count(inf) as numInferences,
    avg(r.weight) as avgContributionWeight,
    avg(inf.confidence) as avgConfidence,
    avg(inf.durationMs) as avgDurationMs
ORDER BY avgContributionWeight DESC;

// Q4.6: Find low-confidence inferences
// Purpose: Quality monitoring
// Use case: Error detection, model improvement
MATCH (inf:Inference {tenantId: $tenantId})
WHERE inf.confidence < 0.6
OPTIONAL MATCH (inf)-[:USED_MODEL]->(model:Model)
OPTIONAL MATCH (inf)-[:USED_REASONING]->(rm:ReasoningMode)
RETURN 
    inf.id as inferenceId,
    inf.taskType as taskType,
    inf.confidence as confidence,
    inf.timestamp as timestamp,
    collect(DISTINCT model.name) as models,
    collect(DISTINCT rm.type) as reasoningModes
ORDER BY inf.confidence ASC
LIMIT 50;

// Q4.7: Get reasoning mode distribution
// Purpose: Understand reasoning patterns
// Use case: System optimization, pattern analysis
MATCH (inf:Inference {tenantId: $tenantId})-[r:USED_REASONING]->(rm:ReasoningMode)
WHERE inf.timestamp >= datetime() - duration({days: 30})
RETURN 
    rm.type as reasoningMode,
    count(inf) as numInferences,
    avg(r.weight) as avgWeight,
    avg(inf.confidence) as avgConfidence,
    avg(r.durationMs) as avgDurationMs
ORDER BY numInferences DESC;

// Q4.8: Find alternatives considered in inference
// Purpose: Decision rationale
// Use case: Explainability, transparency
MATCH (inf:Inference {id: $inferenceId})-[:CONSIDERED_ALTERNATIVE]->(alt:Alternative)
RETURN 
    alt.description as alternative,
    alt.confidence as confidence,
    alt.reasonNotChosen as reason
ORDER BY alt.confidence DESC;

// Q4.9: Get context for inference
// Purpose: Situational understanding
// Use case: Context-aware analysis
MATCH (inf:Inference {id: $inferenceId})-[:IN_CONTEXT]->(ctx:Context)
RETURN 
    ctx.domain as domain,
    ctx.userLevel as userLevel,
    ctx.language as language,
    ctx.timeOfDay as timeOfDay;

// Q4.10: Get user feedback on inferences
// Purpose: Quality assessment
// Use case: Model improvement, satisfaction tracking
MATCH (inf:Inference)-[:RESULTED_IN]->(decision:Decision)-[:RATED_BY]->(feedback:Feedback)
MATCH (feedback)-[:PROVIDED_BY]->(user:User {tenantId: $tenantId})
MATCH (inf)-[:USED_MODEL]->(model:Model)
WHERE feedback.timestamp >= datetime() - duration({days: 30})
RETURN 
    model.name as modelName,
    avg(feedback.rating) as avgRating,
    count(feedback) as numRatings,
    sum(CASE WHEN feedback.rating >= 4 THEN 1 ELSE 0 END) as positiveRatings,
    sum(CASE WHEN feedback.rating <= 2 THEN 1 ELSE 0 END) as negativeRatings
ORDER BY avgRating DESC;

// =============================================
// SECTION 5: ERROR TRACKING & CLUSTERING
// =============================================

// Q5.1: Find similar errors (error clustering)
// Purpose: Pattern detection in failures
// Use case: Root cause analysis, batch fixes
MATCH (error1:Error {id: $errorId})-[r:SIMILAR_TO]-(error2:Error)
WHERE r.similarity > 0.7
RETURN 
    error2.id as similarErrorId,
    error2.type as errorType,
    error2.message as message,
    error2.timestamp as timestamp,
    r.similarity as similarity,
    error2.resolved as resolved
ORDER BY r.similarity DESC
LIMIT 20;

// Q5.2: Get error statistics by type
// Purpose: Error trend analysis
// Use case: Quality monitoring, prioritization
MATCH (error:Error {tenantId: $tenantId})
WHERE error.timestamp >= datetime() - duration({days: 7})
RETURN 
    error.type as errorType,
    error.severity as severity,
    count(error) as errorCount,
    sum(CASE WHEN error.resolved THEN 1 ELSE 0 END) as resolvedCount,
    avg(duration.inDays(error.timestamp, error.resolvedAt).days) as avgResolutionDays
ORDER BY errorCount DESC;

// Q5.3: Find atoms affected by errors
// Purpose: Impact assessment
// Use case: Data quality remediation
MATCH (error:Error {id: $errorId})-[:AFFECTED_ATOM]->(atom:Atom)
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.syncStatus as syncStatus,
    atom.createdAt as createdAt;

// Q5.4: Get errors in operations
// Purpose: Operation failure analysis
// Use case: Process improvement
MATCH (op:Operation {tenantId: $tenantId})-[:FAILED_WITH]->(error:Error)
WHERE op.timestamp >= datetime() - duration({days: 30})
RETURN 
    op.type as operationType,
    error.type as errorType,
    count(*) as occurrences,
    collect(DISTINCT error.message)[0..5] as sampleMessages
ORDER BY occurrences DESC;

// Q5.5: Find unresolved critical errors
// Purpose: Urgent issue tracking
// Use case: Incident management
MATCH (error:Error {tenantId: $tenantId})
WHERE error.severity = 'critical' AND error.resolved = false
OPTIONAL MATCH (error)-[:OCCURRED_IN_OPERATION]->(op:Operation)
RETURN 
    error.id as errorId,
    error.type as errorType,
    error.message as message,
    error.timestamp as timestamp,
    op.type as operationType
ORDER BY error.timestamp DESC
LIMIT 20;

// =============================================
// SECTION 6: ACCESS AUDIT & GOVERNANCE
// =============================================

// Q6.1: Get all access events for an atom
// Purpose: Access audit trail
// Use case: Security compliance, investigation
MATCH (atom:Atom {id: $atomId, tenantId: $tenantId})<-[r:ACCESSED_BY]-(user:User)
RETURN 
    user.email as userEmail,
    r.operation as operation,
    r.timestamp as timestamp,
    r.ipAddress as ipAddress,
    r.userAgent as userAgent
ORDER BY r.timestamp DESC;

// Q6.2: Get user activity summary
// Purpose: User behavior analysis
// Use case: Access patterns, anomaly detection
MATCH (user:User {id: $userId})-[r:ACCESSED_BY]->(:Atom {tenantId: $tenantId})
WHERE r.timestamp >= datetime() - duration({days: 30})
RETURN 
    r.operation as operation,
    count(*) as accessCount,
    collect(DISTINCT r.ipAddress)[0..5] as ipAddresses
ORDER BY accessCount DESC;

// Q6.3: Find atoms by classification
// Purpose: Data governance
// Use case: Compliance reporting, access control
MATCH (atom:Atom {tenantId: $tenantId})-[:CLASSIFIED_AS]->(dc:DataClassification {name: $classification})
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.createdAt as createdAt,
    dc.accessLevel as accessLevel,
    dc.retentionDays as retentionDays;

// Q6.4: Get compliance report
// Purpose: Regulatory compliance
// Use case: Audit preparation, compliance verification
MATCH (dc:DataClassification)-[:REQUIRES_COMPLIANCE]->(rule:ComplianceRule {framework: $framework})
MATCH (atom:Atom {tenantId: $tenantId})-[:CLASSIFIED_AS]->(dc)
RETURN 
    dc.name as classification,
    rule.ruleId as ruleId,
    rule.description as requirement,
    count(atom) as affectedAtomCount;

// Q6.5: Find atoms exceeding retention policy
// Purpose: Data retention compliance
// Use case: Automated cleanup, GDPR compliance
MATCH (atom:Atom {tenantId: $tenantId})-[:CLASSIFIED_AS]->(dc:DataClassification)
WHERE duration.inDays(atom.createdAt, datetime()).days > dc.retentionDays
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.createdAt as createdAt,
    dc.name as classification,
    dc.retentionDays as retentionDays,
    duration.inDays(atom.createdAt, datetime()).days as actualRetentionDays
ORDER BY actualRetentionDays DESC;

// Q6.6: Audit log query
// Purpose: Comprehensive audit trail
// Use case: Security investigation, compliance
MATCH (al:AuditLog {tenantId: $tenantId})
WHERE al.timestamp >= datetime($startDate) AND al.timestamp <= datetime($endDate)
OPTIONAL MATCH (al)-[:LOGGED_ACTION_BY_USER]->(user:User)
RETURN 
    al.action as action,
    al.resourceType as resourceType,
    al.resourceId as resourceId,
    user.email as userEmail,
    al.timestamp as timestamp,
    al.success as success,
    al.ipAddress as ipAddress
ORDER BY al.timestamp DESC
LIMIT 1000;

// Q6.7: Find unauthorized access attempts
// Purpose: Security monitoring
// Use case: Intrusion detection
MATCH (al:AuditLog {tenantId: $tenantId})
WHERE al.success = false AND al.action = 'access'
  AND al.timestamp >= datetime() - duration({days: 7})
OPTIONAL MATCH (al)-[:LOGGED_ACTION_BY_USER]->(user:User)
RETURN 
    user.email as userEmail,
    al.resourceType as resourceType,
    al.ipAddress as ipAddress,
    count(*) as failedAttempts,
    collect(al.timestamp)[0..5] as recentAttempts
GROUP BY user.email, al.resourceType, al.ipAddress
HAVING failedAttempts > 3
ORDER BY failedAttempts DESC;

// =============================================
// SECTION 7: TEMPORAL & VERSION TRACKING
// =============================================

// Q7.1: Get version history for an atom
// Purpose: Change tracking
// Use case: Audit trail, rollback support
MATCH (atom:Atom {id: $atomId})-[:HAS_VERSION]->(version:AtomVersion)
OPTIONAL MATCH (version)-[:MODIFIED_BY]->(user:User)
RETURN 
    version.version as versionNumber,
    version.changeType as changeType,
    version.timestamp as timestamp,
    user.email as modifiedBy,
    version.contentHash as contentHash
ORDER BY version.version DESC;

// Q7.2: Compare two versions of an atom
// Purpose: Diff analysis
// Use case: Change review, debugging
MATCH (atom:Atom {id: $atomId})-[:HAS_VERSION]->(v1:AtomVersion {version: $version1})
MATCH (atom)-[:HAS_VERSION]->(v2:AtomVersion {version: $version2})
RETURN 
    v1.version as version1,
    v1.content as content1,
    v1.timestamp as timestamp1,
    v2.version as version2,
    v2.content as content2,
    v2.timestamp as timestamp2,
    v1.contentHash = v2.contentHash as contentUnchanged;

// Q7.3: Find recently modified atoms
// Purpose: Activity monitoring
// Use case: Change tracking, notification
MATCH (atom:Atom {tenantId: $tenantId})
WHERE atom.modifiedAt >= datetime() - duration({hours: 24})
OPTIONAL MATCH (atom)-[:HAS_VERSION]->(version:AtomVersion)
WITH atom, max(version.version) as latestVersion
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.modifiedAt as modifiedAt,
    latestVersion
ORDER BY atom.modifiedAt DESC
LIMIT 100;

// Q7.4: Get model version evolution
// Purpose: Track model improvements
// Use case: ML ops, performance tracking
MATCH path = (mv1:ModelVersion {modelId: $modelId})-[:EVOLVED_TO*]->(mv2:ModelVersion)
RETURN 
    mv1.version as fromVersion,
    mv2.version as toVersion,
    length(path) as evolutionSteps,
    [rel in relationships(path) | rel.reason] as evolutionReasons,
    mv2.performanceScore - mv1.performanceScore as performanceImprovement
ORDER BY length(path) DESC;

// =============================================
// SECTION 8: ADVANCED ANALYTICS & INSIGHTS
// =============================================

// Q8.1: Get tenant-wide statistics
// Purpose: System health overview
// Use case: Dashboards, reporting
MATCH (t:Tenant {id: $tenantId})
OPTIONAL MATCH (t)<-[:BELONGS_TO_TENANT]-(atom:Atom)
OPTIONAL MATCH (t)<-[:BELONGS_TO_TENANT]-(doc:Document)
OPTIONAL MATCH (op:Operation {tenantId: $tenantId})
OPTIONAL MATCH (inf:Inference {tenantId: $tenantId})
RETURN 
    t.name as tenantName,
    count(DISTINCT atom) as totalAtoms,
    count(DISTINCT doc) as totalDocuments,
    count(DISTINCT op) as totalOperations,
    count(DISTINCT inf) as totalInferences;

// Q8.2: Find bottleneck operations
// Purpose: Performance optimization
// Use case: System tuning, capacity planning
MATCH (op:Operation {tenantId: $tenantId})
WHERE op.status = 'completed' AND op.timestamp >= datetime() - duration({days: 7})
WITH op.type as operationType, 
     avg(op.durationMs) as avgDuration,
     percentileDisc(op.durationMs, 0.95) as p95Duration,
     count(op) as operationCount
WHERE avgDuration > 1000 OR p95Duration > 5000
RETURN 
    operationType,
    operationCount,
    avgDuration,
    p95Duration
ORDER BY p95Duration DESC;

// Q8.3: Data quality score by atom type
// Purpose: Quality monitoring
// Use case: Data governance, improvement initiatives
MATCH (atom:Atom {tenantId: $tenantId})
OPTIONAL MATCH (atom)-[:FAILED_VALIDATION]->(v:Validation)
WITH atom.atomType as atomType,
     count(DISTINCT atom) as totalAtoms,
     count(DISTINCT v) as failedValidations
RETURN 
    atomType,
    totalAtoms,
    failedValidations,
    toFloat(totalAtoms - failedValidations) / totalAtoms as qualityScore
ORDER BY qualityScore ASC;

// Q8.4: Find most accessed atoms
// Purpose: Usage analysis
// Use case: Caching optimization, popularity tracking
MATCH (atom:Atom {tenantId: $tenantId})<-[r:ACCESSED_BY]-()
WHERE r.timestamp >= datetime() - duration({days: 30})
WITH atom, count(r) as accessCount
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.content[0..100] as contentPreview,
    accessCount
ORDER BY accessCount DESC
LIMIT 50;

// Q8.5: Get transformation graph statistics
// Purpose: System complexity analysis
// Use case: Architecture review, optimization
MATCH (atom:Atom {tenantId: $tenantId})
OPTIONAL MATCH (atom)-[:DERIVED_FROM]->(parent)
OPTIONAL MATCH (atom)<-[:DERIVED_FROM]-(child)
RETURN 
    atom.atomType as atomType,
    count(DISTINCT atom) as totalAtoms,
    avg(size((atom)-[:DERIVED_FROM]->())) as avgParents,
    avg(size((atom)<-[:DERIVED_FROM]-())) as avgChildren,
    max(size((atom)-[:DERIVED_FROM]->())) as maxParents
ORDER BY totalAtoms DESC;

// Q8.6: Find orphaned atoms (no relationships)
// Purpose: Data cleanup
// Use case: Storage optimization, integrity check
MATCH (atom:Atom {tenantId: $tenantId})
WHERE NOT (atom)-[:DERIVED_FROM]->()
  AND NOT (atom)<-[:DERIVED_FROM]-()
  AND NOT (atom)-[:BELONGS_TO_DOCUMENT]->()
  AND NOT ()-[:ACCESSED_BY]->(atom)
RETURN 
    atom.id as atomId,
    atom.atomType as atomType,
    atom.createdAt as createdAt,
    atom.syncStatus as syncStatus
ORDER BY atom.createdAt ASC
LIMIT 100;

// Q8.7: Cross-tenant comparison (admin query)
// Purpose: Multi-tenant analytics
// Use case: Billing, resource allocation
MATCH (t:Tenant)
OPTIONAL MATCH (t)<-[:BELONGS_TO_TENANT]-(atom:Atom)
OPTIONAL MATCH (op:Operation {tenantId: t.id})
WHERE op.timestamp >= datetime() - duration({days: 30})
RETURN 
    t.id as tenantId,
    t.name as tenantName,
    t.tier as tier,
    count(DISTINCT atom) as atomCount,
    count(DISTINCT op) as operationCount,
    sum(op.durationMs) / 1000.0 as totalComputeSeconds
ORDER BY atomCount DESC;

// Q8.8: Inference success rate by model
// Purpose: Model performance tracking
// Use case: Model selection, A/B testing
MATCH (model:Model)
OPTIONAL MATCH (inf:Inference)-[:USED_MODEL]->(model)
WHERE inf.timestamp >= datetime() - duration({days: 30})
WITH model, 
     count(inf) as totalInferences,
     sum(CASE WHEN inf.confidence > 0.8 THEN 1 ELSE 0 END) as highConfidenceInferences
WHERE totalInferences > 10
RETURN 
    model.name as modelName,
    totalInferences,
    highConfidenceInferences,
    toFloat(highConfidenceInferences) / totalInferences as successRate
ORDER BY successRate DESC;

// =============================================
// END OF QUERY LIBRARY
// =============================================
