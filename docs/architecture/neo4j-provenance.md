# Neo4j Dual-Ledger Provenance Architecture

**Date:** November 12, 2025  
**System:** SQL Server temporal tables + Neo4j graph database for AI explainability  
**Compliance:** GDPR Article 22 (Right to Explanation), auditability, counterfactual analysis

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Dual-Ledger Philosophy](#dual-ledger-philosophy)
3. [Complete Sync Protocol](#complete-sync-protocol)
4. [GDPR Article 22 Compliance](#gdpr-article-22-compliance)
5. [Explainability Query Patterns](#explainability-query-patterns)
6. [Performance Characteristics](#performance-characteristics)
7. [Conflict Resolution](#conflict-resolution)
8. [Failure Scenarios](#failure-scenarios)
9. [Implementation Reference](#implementation-reference)
10. [Dual-Instance Strategy](#dual-instance-strategy)

---

## Executive Summary

### The Problem: SQL Alone Isn't Enough

**SQL Server is excellent for:**
- ✅ ACID transactions
- ✅ Temporal tables (row-level history with FOR SYSTEM_TIME)
- ✅ Complex joins and aggregations
- ✅ Strict schema enforcement

**But SQL Server is poor for:**
- ❌ Relationship traversal (6+ hops = slow joins)
- ❌ Graph algorithms (shortest path, centrality, community detection)
- ❌ Exploratory queries ("show me all influences on decision X")
- ❌ Counterfactual analysis ("what if factor Y was different?")

### The Solution: Dual-Ledger Architecture

**Core Concept:**
Maintain **two complementary data stores**:

1. **SQL Server (Primary Ledger):**
   - Source of truth for current state
   - Temporal tables for complete history
   - Transactional guarantees
   - Optimized for row lookups, aggregations

2. **Neo4j (Graph Ledger):**
   - Mirror of SQL data as graph
   - Relationship-first model
   - Optimized for traversal, pattern matching
   - Explainability queries (GDPR compliance)

**Sync Protocol:**
```
SQL INSERT/UPDATE → Temporal Table History → Service Broker Message → Neo4jSync Worker → Cypher Query → Neo4j Commit
```

**Current Deployment:**
- **Windows (HART-DESKTOP):** Neo4j Desktop (development/testing)
- **Ubuntu (hart-server):** Neo4j Community Edition (production provenance)

**Benefits:**
1. **GDPR Article 22 Compliance:** Can explain any AI decision with graph traversal
2. **Auditability:** Full provenance from input → embeddings → concepts → decision
3. **Performance:** SQL for OLTP, Neo4j for graph queries (best tool for each job)
4. **Eventual Consistency:** Acceptable for explainability (doesn't need to be real-time)

---

## Dual-Ledger Philosophy

### Why Two Databases?

**Analogy: Financial Accounting**
- **General Ledger (SQL):** Record every transaction, balance sheet, income statement
- **Audit Trail (Neo4j):** Trace any transaction back to source documents, find fraud patterns

**In Hartonomous:**
- **Operational Ledger (SQL):** Store atoms, embeddings, conversations, billing
- **Provenance Graph (Neo4j):** Trace how an AI decision was influenced, find bias patterns

### Historical Context

**Append-Only Ledgers:**
- Blockchain: Immutable chain of transactions (but slow, no complex queries)
- Git: Directed acyclic graph (DAG) of commits (but file-based, not queryable)
- SQL Temporal Tables: System-versioned history (but relational, not graph)

**Hartonomous Combines:**
- SQL temporal tables (structured, ACID, queryable with FOR SYSTEM_TIME)
- Neo4j graph (relationship traversal, pattern matching, graph algorithms)
- Service Broker (asynchronous sync, transactional messaging)

### Design Principles

**1. SQL is Source of Truth**
- All writes go to SQL Server first
- Neo4j is read-only mirror (never write to Neo4j directly)
- If conflict, SQL wins

**2. Eventual Consistency**
- Neo4j may lag behind SQL by seconds/minutes
- Acceptable for explainability (not real-time requirement)
- Reconciliation procedures detect and fix drift

**3. Idempotent Sync**
- Same SQL change replayed → same Neo4j state
- Use versioning (system_version_id) to prevent duplicate processing
- Cypher MERGE ensures no duplicate nodes

**4. Transactional Messaging**
- Service Broker ensures exactly-once delivery
- Message processing wrapped in transaction (commit or rollback together)

---

## Complete Sync Protocol

### Phase 1: SQL Change Capture

**Temporal Table Setup:**

```sql
-- Enable system versioning (if not already enabled)
ALTER TABLE Atoms SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.AtomsHistory));
ALTER TABLE ConceptMappings SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ConceptMappingsHistory));
ALTER TABLE ConversationLogs SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ConversationLogsHistory));
```

**Change Tracking via Triggers:**

```sql
-- Trigger on Atoms table
CREATE TRIGGER trg_Atoms_AfterInsertUpdate
ON dbo.Atoms
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get affected rows
    DECLARE @Changes TABLE (
        AtomId UNIQUEIDENTIFIER,
        Modality NVARCHAR(50),
        SourceUri NVARCHAR(512),
        ContentHash NVARCHAR(128),
        CreatedAtUtc DATETIME2,
        SystemVersionId BIGINT,
        ChangeType NVARCHAR(10)
    );
    
    INSERT INTO @Changes
    SELECT 
        i.AtomId,
        i.Modality,
        i.SourceUri,
        i.ContentHash,
        i.CreatedAtUtc,
        i.system_version_id,  -- Temporal table version
        CASE WHEN EXISTS (SELECT 1 FROM deleted d WHERE d.AtomId = i.AtomId) THEN 'UPDATE' ELSE 'INSERT' END
    FROM inserted i;
    
    -- Send to Service Broker queue
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    DECLARE @MessageBody NVARCHAR(MAX);
    
    BEGIN CONVERSATION @ConversationHandle
        FROM SERVICE Neo4jSyncService
        TO SERVICE 'Neo4jSyncService'
        ON CONTRACT [//Hartonomous/Neo4jSync/SyncContract]
        WITH ENCRYPTION = OFF;
    
    SET @MessageBody = (
        SELECT 
            'Atoms' AS entityType,
            (SELECT * FROM @Changes FOR JSON PATH) AS changes
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [//Hartonomous/Neo4jSync/ChangeMessage]
        (@MessageBody);
    
    -- No COMMIT needed (part of outer transaction)
END;
GO
```

**Alternative: Change Data Capture (CDC)**

```sql
-- Enable CDC (more efficient for high-volume tables)
EXEC sys.sp_cdc_enable_db;

EXEC sys.sp_cdc_enable_table
    @source_schema = 'dbo',
    @source_name = 'Atoms',
    @role_name = NULL,
    @supports_net_changes = 1;

-- Poll CDC table periodically
SELECT 
    __$operation AS operation,  -- 1=DELETE, 2=INSERT, 3=UPDATE_BEFORE, 4=UPDATE_AFTER
    AtomId,
    Modality,
    SourceUri,
    ContentHash,
    sys.fn_cdc_map_lsn_to_time(__$start_lsn) AS change_time
FROM cdc.dbo_Atoms_CT
WHERE __$start_lsn > @LastProcessedLSN
ORDER BY __$start_lsn;
```

### Phase 2: Service Broker Queuing

**Message Types & Contracts:**

```sql
CREATE MESSAGE TYPE [//Hartonomous/Neo4jSync/ChangeMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [//Hartonomous/Neo4jSync/ReconcileMessage]
VALIDATION = WELL_FORMED_XML;

CREATE CONTRACT [//Hartonomous/Neo4jSync/SyncContract]
    ([//Hartonomous/Neo4jSync/ChangeMessage] SENT BY INITIATOR,
     [//Hartonomous/Neo4jSync/ReconcileMessage] SENT BY INITIATOR);

CREATE QUEUE Neo4jSyncQueue
WITH STATUS = ON,
ACTIVATION (
    STATUS = OFF,  -- Manual activation (handled by worker process)
    PROCEDURE_NAME = NULL,
    MAX_QUEUE_READERS = 0,
    EXECUTE AS OWNER
);

CREATE SERVICE Neo4jSyncService
ON QUEUE Neo4jSyncQueue
([//Hartonomous/Neo4jSync/SyncContract]);
```

**Why Manual Activation?**
- Service Broker activation runs stored procedures (T-SQL only)
- Neo4j Cypher execution requires .NET driver
- Worker process polls queue, executes Cypher

### Phase 3: Neo4jSync Worker

**Background Worker (.NET 9):**

```csharp
// Hartonomous.Neo4jSync/Neo4jSyncWorker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System.Data.SqlClient;

namespace Hartonomous.Neo4jSync
{
    public class Neo4jSyncWorker : BackgroundService
    {
        private readonly ILogger<Neo4jSyncWorker> _logger;
        private readonly string _sqlConnectionString;
        private readonly IDriver _neo4jDriver;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
        
        public Neo4jSyncWorker(
            ILogger<Neo4jSyncWorker> logger,
            IConfiguration config)
        {
            _logger = logger;
            _sqlConnectionString = config.GetConnectionString("Hartonomous");
            
            var neo4jUri = config["Neo4j:Uri"] ?? "bolt://localhost:7687";
            var neo4jUser = config["Neo4j:User"] ?? "neo4j";
            var neo4jPassword = config["Neo4j:Password"];
            
            _neo4jDriver = GraphDatabase.Driver(neo4jUri, 
                AuthTokens.Basic(neo4jUser, neo4jPassword));
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Neo4jSync worker starting...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueueAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Neo4jSync queue");
                }
                
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
        
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_sqlConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Receive message from queue
            var command = new SqlCommand(@"
                DECLARE @ConversationHandle UNIQUEIDENTIFIER;
                DECLARE @MessageType NVARCHAR(256);
                DECLARE @MessageBody NVARCHAR(MAX);
                
                WAITFOR (
                    RECEIVE TOP (1)
                        @ConversationHandle = conversation_handle,
                        @MessageType = message_type_name,
                        @MessageBody = CAST(message_body AS NVARCHAR(MAX))
                    FROM Neo4jSyncQueue
                ), TIMEOUT 1000;  -- 1 second timeout
                
                SELECT 
                    @ConversationHandle AS ConversationHandle,
                    @MessageType AS MessageType,
                    @MessageBody AS MessageBody;
            ", connection);
            
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (await reader.ReadAsync(cancellationToken))
            {
                var conversationHandle = reader.GetGuid(0);
                var messageType = reader.GetString(1);
                var messageBody = reader.IsDBNull(2) ? null : reader.GetString(2);
                
                if (messageBody != null)
                {
                    await ProcessChangeAsync(messageBody, cancellationToken);
                }
            }
        }
        
        private async Task ProcessChangeAsync(string messageJson, CancellationToken cancellationToken)
        {
            var change = JsonSerializer.Deserialize<ChangeMessage>(messageJson);
            
            _logger.LogInformation($"Processing {change.EntityType} changes: {change.Changes.Count} rows");
            
            var session = _neo4jDriver.AsyncSession();
            try
            {
                foreach (var item in change.Changes)
                {
                    var cypher = GenerateCypher(change.EntityType, item);
                    
                    await session.ExecuteWriteAsync(async tx =>
                    {
                        await tx.RunAsync(cypher.Query, cypher.Parameters);
                    });
                    
                    _logger.LogDebug($"Synced {change.EntityType} {item.Id}");
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }
        
        private (string Query, object Parameters) GenerateCypher(string entityType, dynamic changeItem)
        {
            return entityType switch
            {
                "Atoms" => (
                    @"MERGE (a:Atom {atomId: $atomId})
                      ON CREATE SET 
                          a.modality = $modality,
                          a.sourceUri = $sourceUri,
                          a.contentHash = $contentHash,
                          a.createdAtUtc = datetime($createdAtUtc),
                          a.systemVersionId = $systemVersionId
                      ON MATCH SET
                          a.modality = $modality,
                          a.sourceUri = $sourceUri,
                          a.systemVersionId = $systemVersionId",
                    new
                    {
                        atomId = (string)changeItem.AtomId,
                        modality = (string)changeItem.Modality,
                        sourceUri = (string)changeItem.SourceUri,
                        contentHash = (string)changeItem.ContentHash,
                        createdAtUtc = (DateTime)changeItem.CreatedAtUtc,
                        systemVersionId = (long)changeItem.SystemVersionId
                    }
                ),
                
                "ConceptMappings" => (
                    @"MATCH (a:Atom {atomId: $atomId})
                      MERGE (c:Concept {conceptId: $conceptId})
                      ON CREATE SET c.conceptName = $conceptName
                      MERGE (a)-[r:HAS_CONCEPT {weight: $weight}]->(c)
                      ON CREATE SET r.systemVersionId = $systemVersionId
                      ON MATCH SET r.weight = $weight, r.systemVersionId = $systemVersionId",
                    new
                    {
                        atomId = (string)changeItem.AtomId,
                        conceptId = (string)changeItem.ConceptId,
                        conceptName = (string)changeItem.ConceptName,
                        weight = (double)changeItem.Weight,
                        systemVersionId = (long)changeItem.SystemVersionId
                    }
                ),
                
                "ConversationLogs" => (
                    @"MERGE (conv:Conversation {conversationId: $conversationId})
                      MERGE (a:Atom {atomId: $atomId})
                      MERGE (conv)-[r:USED_ATOM {
                          weight: $weight,
                          position: $position,
                          systemVersionId: $systemVersionId
                      }]->(a)",
                    new
                    {
                        conversationId = (string)changeItem.ConversationId,
                        atomId = (string)changeItem.AtomId,
                        weight = (double)changeItem.Weight,
                        position = (int)changeItem.Position,
                        systemVersionId = (long)changeItem.SystemVersionId
                    }
                ),
                
                _ => throw new NotSupportedException($"Entity type {entityType} not supported")
            };
        }
    }
}
```

**Configuration (appsettings.json):**

```json
{
  "ConnectionStrings": {
    "Hartonomous": "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=true;"
  },
  "Neo4j": {
    "Uri": "bolt://hart-server:7687",
    "User": "neo4j",
    "Password": "secure-password-from-keyvault"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Hartonomous.Neo4jSync": "Debug"
    }
  }
}
```

### Phase 4: Neo4j Commit

**Transactional Write:**

```cypher
// Inside session.ExecuteWriteAsync
BEGIN
  MERGE (a:Atom {atomId: $atomId})
  ON CREATE SET 
      a.modality = $modality,
      a.sourceUri = $sourceUri,
      a.contentHash = $contentHash,
      a.createdAtUtc = datetime($createdAtUtc),
      a.systemVersionId = $systemVersionId
  ON MATCH SET
      a.modality = $modality,
      a.sourceUri = $sourceUri,
      a.systemVersionId = $systemVersionId
COMMIT
```

**Idempotency via systemVersionId:**
- If systemVersionId already exists in Neo4j, skip update
- Prevents duplicate processing if message replayed

**Index for Performance:**

```cypher
// Create indexes for fast lookups
CREATE INDEX atom_id IF NOT EXISTS FOR (a:Atom) ON (a.atomId);
CREATE INDEX concept_id IF NOT EXISTS FOR (c:Concept) ON (c.conceptId);
CREATE INDEX conversation_id IF NOT EXISTS FOR (c:Conversation) ON (c.conversationId);
CREATE INDEX system_version IF NOT EXISTS FOR (a:Atom) ON (a.systemVersionId);
```

---

## GDPR Article 22 Compliance

### The Regulation

**GDPR Article 22: Right to Explanation**

> The data subject shall have the right not to be subject to a decision based solely on automated processing, including profiling, which produces legal effects concerning him or her or similarly significantly affects him or her.

**Practical Meaning:**
If an AI system makes a decision about a person (loan approval, job application, content moderation), the person has the **right to demand an explanation** of:
1. What factors influenced the decision
2. How those factors were weighted
3. What would change the decision (counterfactual)

### Hartonomous Implementation

**Provenance Graph Structure:**

```cypher
// User input → Atoms → Embeddings → Concepts → Decision
(:User)-[:SENT]->(:Message)-[:CONTAINS]->(:Atom)
(:Atom)-[:HAS_EMBEDDING]->(:Embedding)
(:Embedding)-[:SIMILAR_TO]->(:Atom)  // Vector search results
(:Atom)-[:HAS_CONCEPT]->(:Concept)   // Semantic labels
(:Concept)-[:INFLUENCED]->(:Decision) // Decision factors
(:Decision)-[:GENERATED]->(:Response)
```

**Example Scenario:**

```
User: "What's the best restaurant in Paris?"
AI Response: "Le Bernardin has excellent seafood."

User: "Why did you recommend that?"  [GDPR Article 22 request]
```

**Explainability Query:**

```cypher
// Find all factors that influenced the decision
MATCH path = (user:User {userId: $userId})-[:SENT]->(msg:Message)
             -[:CONTAINS]->(atom:Atom)
             -[:HAS_EMBEDDING]->(emb:Embedding)
             -[:SIMILAR_TO*1..3]->(contextAtom:Atom)
             -[:HAS_CONCEPT]->(concept:Concept)
             -[:INFLUENCED]->(decision:Decision {decisionId: $decisionId})
WHERE msg.timestamp > datetime() - duration({days: 30})
RETURN 
    path,
    [node in nodes(path) | {
        type: labels(node)[0],
        id: node.atomId,
        data: node.content,
        weight: CASE WHEN 'INFLUENCED' IN [type(r) IN relationships(path) | type(r)] 
                     THEN [r IN relationships(path) WHERE type(r) = 'INFLUENCED'][0].weight 
                     ELSE NULL END
    }] AS factorChain,
    reduce(totalWeight = 0.0, r IN relationships(path) | totalWeight + coalesce(r.weight, 0.0)) AS influenceScore
ORDER BY influenceScore DESC
LIMIT 10;
```

**Human-Readable Explanation:**

```
Your recommendation was influenced by:

1. Your previous query "seafood restaurants Paris" (weight: 0.85)
   → Matched concept: "French cuisine" 
   → Similar atoms: [Le Bernardin review, Michelin stars seafood]

2. Your location history: Paris, France (weight: 0.72)
   → Matched concept: "Geographic preference"
   → Similar atoms: [Paris restaurant reviews, local favorites]

3. Your rating history: 4.5★ average for seafood (weight: 0.68)
   → Matched concept: "User preference seafood"
   → Similar atoms: [Seafood restaurant ratings, user taste profile]

Counterfactual: If you had rated seafood restaurants lower (<3★), 
the recommendation would have been "Septime" (modern French, no seafood focus).
```

### Counterfactual Analysis

**"What If" Queries:**

```cypher
// What if the user had different preferences?
MATCH (decision:Decision {decisionId: $decisionId})
MATCH (decision)-[r:INFLUENCED_BY]->(factor:Concept)
WITH decision, 
     collect({concept: factor, weight: r.weight}) AS originalFactors

// Simulate changing one factor
UNWIND originalFactors AS f
WITH decision, originalFactors,
     CASE WHEN f.concept.conceptName = 'User preference seafood'
          THEN {concept: f.concept, weight: 0.1}  // Reduce weight
          ELSE f
     END AS modifiedFactor

// Recalculate decision with modified factors
WITH decision, collect(modifiedFactor) AS modifiedFactors
CALL {
    WITH modifiedFactors
    // Re-run decision algorithm with modified weights
    UNWIND modifiedFactors AS mf
    MATCH (mf.concept)-[:SUGGESTS]->(altDecision:Decision)
    RETURN altDecision, sum(mf.weight) AS totalWeight
    ORDER BY totalWeight DESC
    LIMIT 1
}

RETURN 
    decision.outcome AS originalDecision,
    altDecision.outcome AS counterfactualDecision,
    'If user rated seafood lower, recommendation would change to: ' + altDecision.outcome AS explanation;
```

---

## Explainability Query Patterns

### Pattern 1: Direct Influence Chain

**Find atoms that directly influenced a decision:**

```cypher
MATCH (decision:Decision {decisionId: $decisionId})
     -[r:INFLUENCED_BY]->(concept:Concept)
     <-[:HAS_CONCEPT]-(atom:Atom)
RETURN 
    atom.atomId,
    atom.content,
    concept.conceptName,
    r.weight AS influence,
    atom.createdAtUtc
ORDER BY r.weight DESC
LIMIT 10;
```

### Pattern 2: Similarity Cascade

**Trace vector search similarity chains:**

```cypher
MATCH path = (queryAtom:Atom {atomId: $queryAtomId})
            -[:HAS_EMBEDDING]->(queryEmb:Embedding)
            -[:SIMILAR_TO {similarity: >0.8}]->(resultAtom:Atom)
            -[:HAS_CONCEPT]->(concept:Concept)
WHERE queryAtom.createdAtUtc > datetime() - duration({hours: 1})
RETURN 
    resultAtom.content,
    concept.conceptName,
    [r IN relationships(path) WHERE type(r) = 'SIMILAR_TO'][0].similarity AS cosineSimilarity
ORDER BY cosineSimilarity DESC;
```

### Pattern 3: Temporal Provenance

**Show how decision factors evolved over time:**

```cypher
MATCH (user:User {userId: $userId})-[:SENT]->(msg:Message)
     -[:CONTAINS]->(atom:Atom)
     -[:HAS_CONCEPT]->(concept:Concept {conceptName: $conceptName})
WHERE msg.timestamp >= datetime($startDate) AND msg.timestamp <= datetime($endDate)
WITH concept, 
     atom.createdAtUtc AS timestamp,
     count(*) AS occurrences
RETURN 
    concept.conceptName,
    collect({timestamp: timestamp, count: occurrences}) AS timeline
ORDER BY timestamp ASC;
```

### Pattern 4: Concept Co-occurrence

**Find concepts that frequently appear together:**

```cypher
MATCH (atom1:Atom)-[:HAS_CONCEPT]->(concept1:Concept),
      (atom1)-[:HAS_CONCEPT]->(concept2:Concept)
WHERE concept1 <> concept2
WITH concept1, concept2, count(*) AS cooccurrences
WHERE cooccurrences > 5
RETURN 
    concept1.conceptName,
    concept2.conceptName,
    cooccurrences,
    cooccurrences / (
        (SELECT count(*) FROM (MATCH (:Atom)-[:HAS_CONCEPT]->(concept1))) +
        (SELECT count(*) FROM (MATCH (:Atom)-[:HAS_CONCEPT]->(concept2)))
    ) AS jaccardSimilarity
ORDER BY cooccurrences DESC;
```

### Pattern 5: Student Model Provenance

**Trace which parent model layers influenced student:**

```cypher
MATCH (student:StudentModel {studentModelId: $studentModelId})
     -[:DISTILLED_FROM {layerRange: $layerRange}]->(parent:ParentModel)
     -[:TRAINED_ON]->(trainingAtom:Atom)
     -[:HAS_CONCEPT]->(capability:Concept)
WHERE capability.conceptName IN student.capabilities
RETURN 
    parent.parentModelName,
    layerRange,
    collect(DISTINCT capability.conceptName) AS inheritedCapabilities,
    count(DISTINCT trainingAtom) AS trainingDataSize;
```

### Pattern 6: Bias Detection

**Find if certain demographics are over-represented in decision factors:**

```cypher
MATCH (decision:Decision)-[:INFLUENCED_BY]->(concept:Concept)
     <-[:HAS_CONCEPT]-(atom:Atom)
     -[:TAGGED_WITH]->(demographic:Tag)
WHERE decision.decisionType = 'LoanApproval'
WITH demographic.tagName AS demo, 
     count(DISTINCT decision) AS decisionCount,
     avg([r IN [(decision)-[r:INFLUENCED_BY]->() | r.weight]][0]) AS avgInfluence
RETURN 
    demo,
    decisionCount,
    avgInfluence,
    CASE WHEN avgInfluence > 0.7 THEN 'HIGH BIAS RISK' ELSE 'OK' END AS biasFlag
ORDER BY avgInfluence DESC;
```

### Pattern 7: OODA Loop Impact

**Show how autonomous improvements affected outcomes:**

```cypher
MATCH (hypothesis:Hypothesis {hypothesisType: 'IndexOptimization'})
     -[:EXECUTED_AS]->(action:Action)
     -[:IMPROVED]->(metric:Metric {metricName: 'QueryLatency'})
WHERE action.executedAtUtc > datetime() - duration({days: 7})
RETURN 
    hypothesis.description,
    action.executedAtUtc,
    metric.beforeValue,
    metric.afterValue,
    (metric.beforeValue - metric.afterValue) / metric.beforeValue AS improvementPercent
ORDER BY action.executedAtUtc DESC;
```

### Pattern 8: Cache Hit Analysis

**Explain why certain atoms were cached:**

```cypher
MATCH (atom:Atom {atomId: $atomId})-[:ACCESSED_BY]->(request:Request)
WITH atom, count(request) AS accessCount, max(request.timestamp) AS lastAccess
WHERE accessCount > 10 AND lastAccess > datetime() - duration({hours: 1})
MATCH (atom)-[:CACHED_IN]->(cache:Cache)
RETURN 
    atom.content,
    accessCount,
    lastAccess,
    cache.tier AS cacheTier,
    'Cached due to high access frequency (' + accessCount + ' requests in last hour)' AS explanation;
```

### Pattern 9: Student Model Selection

**Explain why a specific student model was chosen:**

```cypher
MATCH (request:Request {requestId: $requestId})
     -[:ROUTED_TO]->(student:StudentModel)
     -[:DISTILLED_FROM]->(parent:ParentModel)
MATCH (request)-[:CONTAINS]->(atom:Atom)
     -[:HAS_CONCEPT]->(concept:Concept)
WHERE concept.conceptName IN student.capabilities
RETURN 
    student.studentModelName,
    parent.parentModelName,
    collect(concept.conceptName) AS matchedCapabilities,
    student.latencyMs AS latency,
    student.costPerToken AS cost,
    'Student selected because request matched capabilities: ' + 
        [c IN collect(concept.conceptName) | c][0..3] AS explanation;
```

### Pattern 10: Billing Attribution

**Trace token consumption back to specific features:**

```cypher
MATCH (user:User {userId: $userId})-[:INITIATED]->(conversation:Conversation)
     -[:USED_MODEL]->(model:Model)
     -[:CONSUMED {tokens: tokens}]->(resource:Resource)
WHERE conversation.timestamp >= datetime($billingPeriodStart)
WITH user, 
     model.modelName AS modelName,
     sum(tokens) AS totalTokens,
     count(DISTINCT conversation) AS conversationCount
RETURN 
    modelName,
    totalTokens,
    conversationCount,
    totalTokens / conversationCount AS avgTokensPerConversation,
    totalTokens * model.costPerToken AS totalCost
ORDER BY totalCost DESC;
```

### Pattern 11: Anomaly Detection

**Find unusual decision patterns:**

```cypher
MATCH (decision:Decision)-[:INFLUENCED_BY {weight: weight}]->(concept:Concept)
WITH concept, 
     avg(weight) AS avgWeight,
     stdev(weight) AS stddevWeight
MATCH (outlierDecision:Decision)-[r:INFLUENCED_BY]->(concept)
WHERE r.weight > avgWeight + (2 * stddevWeight)  // 2 sigma outlier
RETURN 
    outlierDecision.decisionId,
    concept.conceptName,
    r.weight AS outlierWeight,
    avgWeight,
    stddevWeight,
    'Unusual influence (2σ above average)' AS anomalyFlag;
```

### Pattern 12: Regulatory Audit Trail

**Generate complete audit log for regulator:**

```cypher
MATCH path = (user:User {userId: $userId})-[:SENT]->(msg:Message)
            -[:CONTAINS]->(atom:Atom)
            -[*1..5]->(decision:Decision {decisionId: $decisionId})
WHERE msg.timestamp >= datetime($auditStartDate) 
  AND msg.timestamp <= datetime($auditEndDate)
WITH path, 
     [node IN nodes(path) | {
         type: labels(node)[0],
         id: CASE labels(node)[0]
                 WHEN 'User' THEN node.userId
                 WHEN 'Message' THEN node.messageId
                 WHEN 'Atom' THEN node.atomId
                 WHEN 'Decision' THEN node.decisionId
                 ELSE node.id
             END,
         timestamp: CASE labels(node)[0]
                        WHEN 'Message' THEN node.timestamp
                        WHEN 'Atom' THEN node.createdAtUtc
                        WHEN 'Decision' THEN node.decidedAtUtc
                        ELSE NULL
                    END,
         data: node
     }] AS auditTrail
RETURN 
    $userId AS userId,
    $decisionId AS decisionId,
    auditTrail,
    length(path) AS provenanceDepth,
    datetime() AS auditGeneratedAt;
```

---

## Performance Characteristics

### When to Query SQL vs Neo4j

**SQL Server Strengths:**

| Query Type | Example | Use SQL |
|------------|---------|---------|
| Row lookup by ID | `SELECT * FROM Atoms WHERE AtomId = @id` | ✅ |
| Aggregations | `SELECT COUNT(*), AVG(TokensConsumed) FROM TenantUsageTracking` | ✅ |
| Range scans | `SELECT * FROM ConversationLogs WHERE LoggedAtUtc > @start` | ✅ |
| Joins (2-3 tables) | `SELECT a.*, e.* FROM Atoms a JOIN AtomEmbeddings e ON a.AtomId = e.AtomId` | ✅ |
| Temporal queries | `SELECT * FROM Atoms FOR SYSTEM_TIME AS OF @timestamp` | ✅ (Neo4j can't do this) |

**Neo4j Strengths:**

| Query Type | Example | Use Neo4j |
|------------|---------|-----------|
| Relationship traversal | `MATCH (a:Atom)-[:SIMILAR_TO*1..5]->(b:Atom)` | ✅ |
| Pattern matching | `MATCH (user)-[:SENT]->(msg)-[:CONTAINS]->(atom)` | ✅ |
| Graph algorithms | Shortest path, PageRank, community detection | ✅ |
| Variable-depth joins | "Find all atoms influenced by X, regardless of depth" | ✅ |
| Exploratory queries | "Show me all relationships involving this concept" | ✅ |

### Benchmarks

**SQL Server (Windows HART-DESKTOP):**
- Row lookup by PK: **<1ms**
- 2-table join (Atoms + Embeddings): **5-10ms**
- Vector search (TOP 10 from 1M): **200-500ms** (depends on index)
- Temporal query (FOR SYSTEM_TIME): **10-50ms**

**Neo4j Community (Ubuntu hart-server):**
- Node lookup by atomId: **<1ms** (indexed)
- 1-hop relationship traversal: **1-5ms**
- 3-hop traversal (SIMILAR_TO chain): **10-30ms**
- Variable-depth traversal (1..10 hops): **50-200ms**
- PageRank (10K nodes): **500ms-2s**

**Cross-Server Linked Query (Windows → Ubuntu SQL):**
- Simple SELECT: **20-50ms** (network latency)
- Complex join: **100-500ms**

### Caching Strategy

**Neo4j Query Cache:**

```cypher
// Frequently used queries cached in application
var cachedExplanation = _cache.GetOrAddAsync($"explanation:{decisionId}", async () =>
{
    var session = _neo4jDriver.AsyncSession();
    try
    {
        return await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(explanationQuery, new { decisionId });
            return await result.ToListAsync();
        });
    }
    finally
    {
        await session.CloseAsync();
    }
}, TimeSpan.FromMinutes(15));
```

**Warm-Up Queries:**

```cypher
// Pre-load hot data into Neo4j cache on startup
MATCH (a:Atom)
WHERE a.createdAtUtc > datetime() - duration({days: 7})
WITH a LIMIT 1000
MATCH (a)-[r]->(n)
RETURN count(r);
```

---

## Conflict Resolution

### Eventual Consistency Model

**Guarantee:**
Neo4j will **eventually** match SQL Server, but may lag by seconds to minutes.

**Acceptable Lag:**
- Explainability queries (GDPR): **5-10 minutes acceptable** (legal requirement is "reasonable timeframe")
- Audit trails: **1 hour acceptable** (used for forensics, not real-time)
- Graph analytics: **Daily batch sync acceptable** (used for trend analysis)

### Detecting Drift

**Reconciliation Query (SQL):**

```sql
-- Find Atoms in SQL but not in Neo4j
SELECT 
    a.AtomId,
    a.Modality,
    a.CreatedAtUtc,
    a.system_version_id AS LatestVersion
FROM Atoms a
WHERE NOT EXISTS (
    SELECT 1 
    FROM OPENQUERY([hart-server], 
        'SELECT atomId FROM Atoms WHERE systemVersionId = ' + CAST(a.system_version_id AS NVARCHAR(20))
    ) neo
    WHERE neo.atomId = CAST(a.AtomId AS NVARCHAR(36))
);
```

**Reconciliation Query (Neo4j):**

```cypher
// Find nodes in Neo4j missing recent updates
MATCH (a:Atom)
WHERE a.systemVersionId < $latestVersionInSQL
RETURN a.atomId, a.systemVersionId
LIMIT 100;
```

### Rebuild from SQL History

**Full Reconciliation Procedure:**

```sql
CREATE PROCEDURE dbo.sp_RebuildNeo4jFromSQL
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    -- Get all Atoms created/modified in date range
    DECLARE @Changes TABLE (
        AtomId UNIQUEIDENTIFIER,
        Modality NVARCHAR(50),
        SourceUri NVARCHAR(512),
        ContentHash NVARCHAR(128),
        CreatedAtUtc DATETIME2,
        SystemVersionId BIGINT
    );
    
    INSERT INTO @Changes
    SELECT 
        AtomId,
        Modality,
        SourceUri,
        ContentHash,
        CreatedAtUtc,
        system_version_id
    FROM Atoms
    FOR SYSTEM_TIME BETWEEN @StartDate AND @EndDate;
    
    -- Send all changes to Neo4jSyncQueue
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    
    BEGIN CONVERSATION @ConversationHandle
        FROM SERVICE Neo4jSyncService
        TO SERVICE 'Neo4jSyncService'
        ON CONTRACT [//Hartonomous/Neo4jSync/SyncContract]
        WITH ENCRYPTION = OFF;
    
    DECLARE @MessageBody NVARCHAR(MAX) = (
        SELECT 
            'Atoms' AS entityType,
            'Rebuild' AS syncType,
            (SELECT * FROM @Changes FOR JSON PATH) AS changes
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [//Hartonomous/Neo4jSync/ReconcileMessage]
        (@MessageBody);
    
    PRINT 'Rebuild initiated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' changes queued';
END;
GO
```

---

## Failure Scenarios

### Scenario 1: Neo4j Down

**Symptoms:**
- Neo4jSync worker logs connection errors
- Service Broker queue depth increases
- Explainability queries fail

**Mitigation:**
1. Service Broker retains messages (durable queue)
2. Worker retries with exponential backoff
3. Application shows cached explanations (stale but better than nothing)

**Recovery:**
```powershell
# Restart Neo4j service
sudo systemctl restart neo4j

# Worker will auto-resume processing queue
# No data loss (messages persisted in SQL Server)
```

### Scenario 2: Network Partition (Windows ↔ Ubuntu)

**Symptoms:**
- Neo4jSync worker can't connect to Neo4j on Ubuntu
- Messages pile up in queue
- Linked server queries from Windows to Ubuntu SQL fail

**Mitigation:**
1. Service Broker queues messages locally (no data loss)
2. API gracefully degrades (uses SQL-only queries, no explanations)
3. Auto-retry when network recovers

**Manual Intervention:**
```powershell
# Verify network connectivity
Test-NetConnection -ComputerName hart-server -Port 7687

# Check firewall rules
Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*Neo4j*" }

# Restart Neo4jSync worker after network restored
Restart-Service Hartonomous.Neo4jSync
```

### Scenario 3: Cypher Execution Error

**Symptoms:**
- Worker logs "Cypher query failed"
- Message moved to poison queue after 5 retries
- Neo4j graph incomplete (missing nodes/relationships)

**Root Causes:**
- Schema mismatch (Neo4j expects property that doesn't exist)
- Constraint violation (duplicate atomId)
- Syntax error in generated Cypher

**Mitigation:**
```csharp
// Wrap Cypher execution in try-catch
try
{
    await session.ExecuteWriteAsync(async tx =>
    {
        await tx.RunAsync(cypher.Query, cypher.Parameters);
    });
}
catch (Neo4jException ex)
{
    _logger.LogError(ex, $"Cypher execution failed: {cypher.Query}");
    
    // Log to SQL for manual review
    await LogFailedSyncAsync(messageJson, ex.Message);
    
    // Don't rethrow (prevents poison queue)
    // Manual intervention required
}
```

**Recovery:**
```sql
-- Query failed sync log
SELECT TOP 100 
    FailedAtUtc,
    EntityType,
    ErrorMessage,
    MessageJson
FROM Neo4jSyncFailures
ORDER BY FailedAtUtc DESC;

-- Fix issue (e.g., add missing constraint)
-- Then replay message
EXEC dbo.sp_ReplayFailedSync @FailureId = 'F7B4C8D2-...';
```

### Scenario 4: SQL Server Crash Mid-Sync

**Symptoms:**
- Service Broker conversation interrupted
- Neo4j has partial update (some nodes created, relationships missing)
- Transaction rolled back in SQL, but Neo4j committed

**Mitigation:**
- **Idempotent Cypher:** MERGE ensures re-running same update is safe
- **systemVersionId:** Track which SQL version was synced to Neo4j
- **Reconciliation:** Periodic sp_RebuildNeo4jFromSQL detects drift

**Example:**
```
1. SQL INSERT Atom (version 1001) - COMMITTED
2. Trigger sends message to queue - COMMITTED
3. SQL Server crashes before COMMIT
4. Neo4jSync worker processes message - Neo4j has version 1001
5. SQL Server restarts, Atom has version 1002 (re-executed)
6. Reconciliation detects drift, replays version 1002
7. Neo4j MERGE updates to version 1002 (no duplicate)
```

---

## Implementation Reference

### Service Broker Schema (Complete)

```sql
-- Enable Service Broker
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Message types
CREATE MESSAGE TYPE [//Hartonomous/Neo4jSync/ChangeMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [//Hartonomous/Neo4jSync/ReconcileMessage]
VALIDATION = WELL_FORMED_XML;

-- Contract
CREATE CONTRACT [//Hartonomous/Neo4jSync/SyncContract]
(
    [//Hartonomous/Neo4jSync/ChangeMessage] SENT BY INITIATOR,
    [//Hartonomous/Neo4jSync/ReconcileMessage] SENT BY INITIATOR
);

-- Queue
CREATE QUEUE Neo4jSyncQueue
WITH STATUS = ON,
ACTIVATION (
    STATUS = OFF,  -- Manual (handled by .NET worker)
    PROCEDURE_NAME = NULL,
    MAX_QUEUE_READERS = 0,
    EXECUTE AS OWNER
),
POISON_MESSAGE_HANDLING (STATUS = ON);

-- Service
CREATE SERVICE Neo4jSyncService
ON QUEUE Neo4jSyncQueue
([//Hartonomous/Neo4jSync/SyncContract]);
```

### Neo4j Schema (Complete)

```cypher
// Node constraints
CREATE CONSTRAINT atom_id_unique IF NOT EXISTS FOR (a:Atom) REQUIRE a.atomId IS UNIQUE;
CREATE CONSTRAINT concept_id_unique IF NOT EXISTS FOR (c:Concept) REQUIRE c.conceptId IS UNIQUE;
CREATE CONSTRAINT conversation_id_unique IF NOT EXISTS FOR (c:Conversation) REQUIRE c.conversationId IS UNIQUE;
CREATE CONSTRAINT decision_id_unique IF NOT EXISTS FOR (d:Decision) REQUIRE d.decisionId IS UNIQUE;

// Indexes
CREATE INDEX atom_version IF NOT EXISTS FOR (a:Atom) ON (a.systemVersionId);
CREATE INDEX atom_created IF NOT EXISTS FOR (a:Atom) ON (a.createdAtUtc);
CREATE INDEX concept_name IF NOT EXISTS FOR (c:Concept) ON (c.conceptName);

// Full-text search
CREATE FULLTEXT INDEX atom_content IF NOT EXISTS FOR (a:Atom) ON EACH [a.content];
```

---

## Dual-Instance Strategy

### Neo4j Desktop (Windows HART-DESKTOP)

**Purpose:** Development and testing

**Configuration:**
- Port: 7687 (Bolt), 7474 (HTTP)
- Database: hartonomous-dev
- Authentication: neo4j / local-dev-password
- Memory: 8GB heap, 4GB page cache

**Use Cases:**
- Test Cypher queries before production
- Sandbox for graph algorithm experiments
- Local debugging of Neo4jSync worker

### Neo4j Community (Ubuntu hart-server)

**Purpose:** Production provenance graph

**Configuration:**
- Port: 7687 (Bolt), 7474 (HTTP)
- Database: hartonomous-prod
- Authentication: neo4j / secure-password-from-keyvault
- Memory: 32GB heap, 16GB page cache

**Use Cases:**
- GDPR Article 22 explainability queries
- Regulatory audit trail generation
- Graph analytics (trend analysis, bias detection)

### Cross-Instance Sync

**Not Currently Implemented (Future):**

Could sync Desktop ↔ Community for:
- Backup (Community → Desktop for disaster recovery)
- Testing (prod data snapshot → Desktop for debugging)

**Approach:**
```bash
# Dump from Community
neo4j-admin database dump --database=hartonomous-prod --to-path=/backup

# Copy to Windows
scp hart-server:/backup/hartonomous-prod.dump C:\Neo4j\backups\

# Restore to Desktop
neo4j-admin database load --from-path=C:\Neo4j\backups\hartonomous-prod.dump --database=hartonomous-dev
```

---

## References

**Neo4j Documentation:**
- [Neo4j Graph Database](https://neo4j.com/docs/)
- [Cypher Query Language](https://neo4j.com/docs/cypher-manual/current/)
- [Neo4j .NET Driver](https://neo4j.com/docs/dotnet-manual/current/)

**GDPR Compliance:**
- [GDPR Article 22](https://gdpr-info.eu/art-22-gdpr/)
- [Explainable AI (XAI)](https://en.wikipedia.org/wiki/Explainable_artificial_intelligence)
- [LIME: Local Interpretable Model-Agnostic Explanations](https://arxiv.org/abs/1602.04938)

**Dual-Ledger Architectures:**
- [Lambda Architecture](https://en.wikipedia.org/wiki/Lambda_architecture)
- [Kappa Architecture](https://en.wikipedia.org/wiki/Kappa_architecture)
- [Polyglot Persistence](https://martinfowler.com/bliki/PolyglotPersistence.html)

**Service Broker:**
- [SQL Server Service Broker](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-service-broker)
- [Asynchronous Processing with Service Broker](https://learn.microsoft.com/sql/relational-databases/service-broker/service-broker-programming)
