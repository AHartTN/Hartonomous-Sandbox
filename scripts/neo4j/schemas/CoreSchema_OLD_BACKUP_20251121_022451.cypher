// =============================================
// Hartonomous Neo4j Semantic Audit Schema
// Complete inference traceability and explainability
// =============================================

// =============================================
// CONSTRAINTS AND INDEXES
// =============================================

// Unique constraints
CREATE CONSTRAINT inference_id_unique IF NOT EXISTS
FOR (i:Inference) REQUIRE i.inference_id IS UNIQUE;

CREATE CONSTRAINT model_id_unique IF NOT EXISTS
FOR (m:Model) REQUIRE m.model_id IS UNIQUE;

CREATE CONSTRAINT model_version_unique IF NOT EXISTS
FOR (mv:ModelVersion) REQUIRE (mv.model_id, mv.version) IS UNIQUE;

// Indexes for fast lookups
CREATE INDEX inference_timestamp IF NOT EXISTS
FOR (i:Inference) ON (i.timestamp);

CREATE INDEX inference_task IF NOT EXISTS
FOR (i:Inference) ON (i.task_type);

CREATE INDEX model_name IF NOT EXISTS
FOR (m:Model) ON (m.name);

CREATE INDEX decision_confidence IF NOT EXISTS
FOR (d:Decision) ON (d.confidence);

// =============================================
// NODE LABELS
// =============================================

// Core entity nodes
// :Inference - Represents a single inference operation
// :Model - AI model that participated in inference
// :ModelVersion - Specific version/checkpoint of a model
// :Decision - Final decision/output from inference
// :Evidence - Supporting evidence for a decision
// :Context - Situational context for inference
// :Alternative - Alternative paths/decisions considered but not taken
// :FeatureSpace - Representation space (vector, spatial, graph)
// :ReasoningMode - Type of reasoning used (vector, spatial, graph, symbolic)
// :User - User who triggered inference
// :Feedback - User feedback on inference results

// =============================================
// RELATIONSHIP TYPES
// =============================================

// Inference relationships
// (:Inference)-[:USED_MODEL {weight, contribution}]->(:Model)
// (:Inference)-[:USED_REASONING {type, confidence}]->(:ReasoningMode)
// (:Inference)-[:RESULTED_IN]->(:Decision)
// (:Inference)-[:CONSIDERED_ALTERNATIVE]->(:Alternative)
// (:Inference)-[:IN_CONTEXT]->(:Context)
// (:Inference)-[:INFLUENCED_BY]->(:Inference) - Prior inferences that influenced this one
// (:Inference)-[:REQUESTED_BY]->(:User)

// Decision relationships
// (:Decision)-[:SUPPORTED_BY]->(:Evidence)
// (:Decision)-[:FROM_FEATURE_SPACE]->(:FeatureSpace)
// (:Decision)-[:RATED_BY]->(:Feedback)

// Model relationships
// (:Model)-[:VERSION]->(:ModelVersion)
// (:ModelVersion)-[:EVOLVED_TO {reason, improvement}]->(:ModelVersion)
// (:Model)-[:PERFORMS_WELL_IN]->(:Context)
// (:Model)-[:PERFORMS_POORLY_IN]->(:Context)

// Evidence relationships
// (:Evidence)-[:FROM_VECTOR_SEARCH {similarity}]->(:FeatureSpace)
// (:Evidence)-[:FROM_SPATIAL_QUERY {distance}]->(:FeatureSpace)
// (:Evidence)-[:FROM_GRAPH_TRAVERSAL {path_length}]->(:FeatureSpace)

// =============================================
// SAMPLE DATA STRUCTURE
// =============================================

// Example: Create sample inference trace
// This is documentation - not executed by default

/*
// 1. Create an inference node
CREATE (inf:Inference {
    inference_id: 12345,
    timestamp: datetime(),
    task_type: 'text-generation',
    prompt: 'Explain quantum computing',
    total_duration_ms: 250,
    confidence: 0.87
})

// 2. Create models that participated
CREATE (m1:Model {model_id: 1, name: 'bert-base', type: 'transformer'})
CREATE (m2:Model {model_id: 2, name: 'gpt2', type: 'transformer'})
CREATE (m3:Model {model_id: 3, name: 'llama-7b', type: 'transformer'})

// 3. Link inference to models with weights
CREATE (inf)-[:USED_MODEL {
    contribution_weight: 0.40,
    individual_confidence: 0.85,
    duration_ms: 80
}]->(m1)

CREATE (inf)-[:USED_MODEL {
    contribution_weight: 0.35,
    individual_confidence: 0.88,
    duration_ms: 90
}]->(m2)

CREATE (inf)-[:USED_MODEL {
    contribution_weight: 0.25,
    individual_confidence: 0.89,
    duration_ms: 80
}]->(m3)

// 4. Create reasoning modes used
CREATE (vector_reasoning:ReasoningMode {
    type: 'vector_similarity',
    description: 'Semantic similarity search via DiskANN'
})

CREATE (spatial_reasoning:ReasoningMode {
    type: 'spatial_query',
    description: 'Geometric feature space reasoning'
})

CREATE (inf)-[:USED_REASONING {
    weight: 0.6,
    num_operations: 3,
    duration_ms: 120
}]->(vector_reasoning)

CREATE (inf)-[:USED_REASONING {
    weight: 0.4,
    num_operations: 2,
    duration_ms: 80
}]->(spatial_reasoning)

// 5. Create decision and evidence
CREATE (decision:Decision {
    decision_id: UUID(),
    output_text: 'Quantum computing uses quantum bits...',
    confidence: 0.87,
    token_count: 150
})

CREATE (inf)-[:RESULTED_IN]->(decision)

// 6. Create evidence nodes
CREATE (ev1:Evidence {
    type: 'vector_similarity',
    source: 'knowledge_base',
    similarity_score: 0.94,
    content: 'Retrieved from quantum physics documentation'
})

CREATE (ev2:Evidence {
    type: 'prior_inference',
    source: 'inference_12300',
    relevance_score: 0.82,
    content: 'Similar question answered 2 days ago'
})

CREATE (decision)-[:SUPPORTED_BY {strength: 0.9}]->(ev1)
CREATE (decision)-[:SUPPORTED_BY {strength: 0.7}]->(ev2)

// 7. Create alternatives considered
CREATE (alt:Alternative {
    alternative_id: UUID(),
    description: 'More technical explanation with equations',
    confidence: 0.45,
    reason_not_chosen: 'Lower confidence, prompt suggests simple explanation'
})

CREATE (inf)-[:CONSIDERED_ALTERNATIVE]->(alt)

// 8. Create context
CREATE (ctx:Context {
    domain: 'physics_education',
    user_level: 'beginner',
    language: 'en',
    time_of_day: 'evening'
})

CREATE (inf)-[:IN_CONTEXT]->(ctx)

// 9. Link to prior inferences (causal chain)
MATCH (prior:Inference {inference_id: 12300})
CREATE (prior)-[:INFLUENCED {how: 'provided_cached_context', strength: 0.6}]->(inf)

// 10. Add user feedback later
CREATE (user:User {user_id: 'user_123', name: 'Alice'})
CREATE (inf)-[:REQUESTED_BY]->(user)

CREATE (feedback:Feedback {
    rating: 5,
    comment: 'Clear and helpful explanation',
    timestamp: datetime()
})

CREATE (decision)-[:RATED_BY]->(feedback)
*/

// =============================================
// QUERY PATTERNS FOR EXPLAINABILITY
// =============================================

// These are common queries - not executed, just documentation

/*
// Q1: Why was this decision made?
MATCH (i:Inference {inference_id: $inference_id})-[:RESULTED_IN]->(d:Decision)
MATCH (d)-[:SUPPORTED_BY]->(ev:Evidence)
RETURN d, collect(ev) as supporting_evidence

// Q2: Which models contributed most?
MATCH (i:Inference {inference_id: $inference_id})-[r:USED_MODEL]->(m:Model)
RETURN m.name, r.contribution_weight, r.individual_confidence
ORDER BY r.contribution_weight DESC

// Q3: What alternatives were considered?
MATCH (i:Inference {inference_id: $inference_id})-[:CONSIDERED_ALTERNATIVE]->(alt:Alternative)
RETURN alt.description, alt.confidence, alt.reason_not_chosen

// Q4: What context influenced this decision?
MATCH (i:Inference {inference_id: $inference_id})-[:IN_CONTEXT]->(ctx:Context)
RETURN ctx

// Q5: Which reasoning mode dominated?
MATCH (i:Inference {inference_id: $inference_id})-[r:USED_REASONING]->(rm:ReasoningMode)
RETURN rm.type, r.weight, r.duration_ms
ORDER BY r.weight DESC

// Q6: What prior inferences influenced this?
MATCH path = (prior:Inference)-[:INFLUENCED*1..5]->(current:Inference {inference_id: $inference_id})
RETURN path

// Q7: How have model performance evolved?
MATCH (m:Model {model_id: $model_id})-[:VERSION]->(mv:ModelVersion)
MATCH (mv)-[:EVOLVED_TO*]->(latest:ModelVersion)
RETURN latest.version, latest.performance_improvement

// Q8: Which models work best for this task type?
MATCH (i:Inference {task_type: $task_type})-[r:USED_MODEL]->(m:Model)
WHERE i.confidence > 0.8
RETURN m.name,
       avg(r.contribution_weight) as avg_weight,
       count(i) as num_inferences,
       avg(i.confidence) as avg_confidence
ORDER BY avg_weight DESC

// Q9: Pattern detection: When does vector reasoning outperform spatial?
MATCH (i:Inference)-[v:USED_REASONING]->(vector:ReasoningMode {type: 'vector_similarity'})
MATCH (i)-[s:USED_REASONING]->(spatial:ReasoningMode {type: 'spatial_query'})
WHERE v.weight > s.weight AND i.confidence > 0.9
RETURN i.task_type, avg(v.weight) as avg_vector_weight, count(*) as count
GROUP BY i.task_type

// Q10: Failure analysis: What causes low-confidence decisions?
MATCH (i:Inference)
WHERE i.confidence < 0.6
MATCH (i)-[r:USED_REASONING]->(rm:ReasoningMode)
RETURN rm.type, avg(r.weight), count(*) as failure_count
ORDER BY failure_count DESC

// Q11: Counterfactual: What if we had used model B instead of A?
MATCH (i:Inference {inference_id: $inference_id})-[:USED_MODEL]->(actualModel:Model)
MATCH (similar:Inference)-[:USED_MODEL]->(alternativeModel:Model {model_id: $alt_model_id})
WHERE similar.task_type = i.task_type
  AND NOT (i)-[:USED_MODEL]->(alternativeModel)
RETURN similar.confidence as counterfactual_confidence,
       similar.inference_id as similar_case,
       actualModel.name as used_model,
       alternativeModel.name as alternative_model

// Q12: Temporal evolution: How has reasoning strategy changed?
MATCH (early:Inference)
WHERE early.timestamp > datetime() - duration({days: 30})
  AND early.timestamp < datetime() - duration({days: 15})
MATCH (early)-[r1:USED_REASONING]->(rm:ReasoningMode)
WITH rm.type as reasoning_type, avg(r1.weight) as early_avg_weight

MATCH (recent:Inference)
WHERE recent.timestamp > datetime() - duration({days: 7})
MATCH (recent)-[r2:USED_REASONING]->(rm2:ReasoningMode {type: reasoning_type})
RETURN reasoning_type,
       early_avg_weight,
       avg(r2.weight) as recent_avg_weight,
       avg(r2.weight) - early_avg_weight as trend

// Q13: User satisfaction by model
MATCH (i:Inference)-[:REQUESTED_BY]->(u:User)
MATCH (i)-[:USED_MODEL]->(m:Model)
MATCH (i)-[:RESULTED_IN]->(d:Decision)-[:RATED_BY]->(f:Feedback)
RETURN m.name, avg(f.rating) as avg_rating, count(*) as num_ratings
ORDER BY avg_rating DESC
*/

// =============================================
// INITIALIZATION
// =============================================

// Create common reasoning mode nodes (these persist across inferences)
MERGE (vector:ReasoningMode {type: 'vector_similarity'})
ON CREATE SET vector.description = 'Semantic similarity via vector embeddings and DiskANN',
              vector.created = datetime()

MERGE (spatial:ReasoningMode {type: 'spatial_query'})
ON CREATE SET spatial.description = 'Geometric reasoning via spatial indexes and geometric operations',
              spatial.created = datetime()

MERGE (graph:ReasoningMode {type: 'graph_traversal'})
ON CREATE SET graph.description = 'Symbolic/causal reasoning via graph pattern matching',
              graph.created = datetime()

MERGE (hybrid:ReasoningMode {type: 'hybrid'})
ON CREATE SET hybrid.description = 'Combined multi-modal reasoning',
              hybrid.created = datetime()

MERGE (symbolic:ReasoningMode {type: 'symbolic_logic'})
ON CREATE SET symbolic.description = 'Classical logical inference via SQL predicates',
              symbolic.created = datetime()

// Create common context categories
MERGE (ctx_text:Context {domain: 'text_generation'})
MERGE (ctx_image:Context {domain: 'image_generation'})
MERGE (ctx_audio:Context {domain: 'audio_generation'})
MERGE (ctx_multimodal:Context {domain: 'multimodal'})
MERGE (ctx_embedding:Context {domain: 'embedding_generation'})
MERGE (ctx_search:Context {domain: 'semantic_search'})

RETURN 'Neo4j schema initialized successfully' as status;
