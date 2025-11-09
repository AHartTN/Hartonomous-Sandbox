# Missing AGI Components Analysis

**Date**: 2025-01-22  
**Context**: Exhaustive investigation of repository to identify AGI functionality gaps  
**Executive Summary**: While the repository has extensive AGI *declarations* (procedures, aggregates, tables), **critical core AGI capabilities are incomplete, missing, or non-functional**. This is not a "kids science project" - the architectural vision is sophisticated - but **massive functionality is missing between declaration and implementation**.

---

## 🚨 CRITICAL DEFECTS (System Cannot Function)

### 1. **TensorAtoms Table Does Not Exist** ⛔
**Severity**: CATASTROPHIC  
**Impact**: Learning system is completely non-functional

**Evidence**:
- `sp_UpdateModelWeightsFromFeedback` references `TensorAtoms` table (line 87: `FROM TensorAtoms WHERE LayerId = @layerId`)
- **No `CREATE TABLE TensorAtoms` anywhere in `sql/tables/` directory** (verified via grep, file search, directory listing)
- References exist in `sql/Temporal_Tables_Evaluation.sql` lines 58-226 (evaluation script, not creation)
- Evaluation script attempts `ALTER TABLE dbo.TensorAtoms` to add temporal tracking, but base table never created

**Consequences**:
- Model weights cannot be stored
- sp_UpdateModelWeightsFromFeedback will throw runtime error on first execution
- AGI has no persistent learned parameters
- Autonomous improvement loop cannot modify model behavior
- All "learning" is vaporware

**Related Missing**:
- No schema for layer topology (number of neurons per layer, activation functions)
- No weight initialization procedures
- No backup/restore for model checkpoints

---

### 2. **Learning Loop Never Updates Weights** ⛔
**Severity**: CATASTROPHIC  
**Impact**: Even if TensorAtoms existed, learning doesn't work

**Evidence** (`sql/procedures/Feedback.ModelWeightUpdates.sql`, 144 lines):
```sql
DECLARE LayerCursor CURSOR FOR SELECT LayerId, LayerName, AvgNeuronCount FROM #LayerUpdates;
OPEN LayerCursor;
FETCH NEXT FROM LayerCursor INTO @LayerId, @LayerName, @AvgNeuronCount;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Layer ' + CAST(@LayerId AS NVARCHAR(10)) + ' (' + @LayerName + '): Updating ' 
          + CAST(@AvgNeuronCount AS NVARCHAR(10)) + ' neurons';
    -- TODO: Actual weight update logic would go here
    FETCH NEXT FROM LayerCursor INTO @LayerId, @LayerName, @AvgNeuronCount;
END;
CLOSE LayerCursor;
DEALLOCATE LayerCursor;

-- Return computed layer updates
SELECT LayerId, LayerName, AvgNeuronCount, UpdateMagnitude FROM #LayerUpdates;
```

**What it does**:
1. Computes `UpdateMagnitude = learningRate * (avgRating / maxRating) * SQRT(successfulInferences)`
2. Iterates layers with cursor
3. **PRINTS update messages** (lines 87-100)
4. **Returns SELECT of #LayerUpdates** (line 103)
5. **NEVER EXECUTES `UPDATE TensorAtoms SET ...`**

**Verification**: Grep search `UPDATE.*TensorAtoms` in `sql/` returned 15 matches but ZERO actual UPDATE statements - all matches are comments, metadata, or UPDATE on other tables (statistics, models, cache)

**Consequences**:
- User feedback never influences model behavior
- AGI cannot learn from mistakes
- Self-improvement is theater (prints "improving" but doesn't change anything)
- No gradient descent, no backpropagation, no optimization

---

### 3. **No Actual Gradient Computation** ⚠️
**Severity**: HIGH  
**Impact**: Cannot implement proper learning even if UPDATE is added

**Found**:
- `GradientStatistics` aggregate **EXISTS** in `NeuralVectorAggregates.cs` lines 332-465
- **Only computes statistics** (mean norm, max norm, stddev, vanishing/exploding detection)
- **Does NOT compute gradients** - just analyzes pre-computed gradients passed in

**Missing**:
- ❌ **Backpropagation algorithm** (compute ∂Loss/∂W for each layer)
- ❌ **Loss functions** (cross-entropy, MSE, custom AGI objective)
- ❌ **Chain rule implementation** for multi-layer networks
- ❌ **Automatic differentiation** or symbolic gradient computation
- ❌ **Optimizer implementations** (Adam, RMSprop, momentum)
  - Found "simplified SGD" in `MatrixFactorization.cs` but not used in learning loop

**Found "Optimization" Components**:
- `CosineAnnealingSchedule` aggregate (lines 467+) - learning rate scheduling
- `GradientStatistics` - monitoring tool, not computation

**What This Means**:
- Even if someone implements `UPDATE TensorAtoms`, they'd need to manually compute gradients
- No framework for computing how to change weights to reduce loss
- Transformer inference exists but transformer **training** does not

---

## 🧠 MISSING COGNITIVE ARCHITECTURE

### 4. **No Memory Systems** ⚠️
**Severity**: HIGH  
**Impact**: AGI cannot remember, consolidate, or use past experiences

#### What Exists:
- **provenance.Concepts** table - unsupervised clustering of embeddings into semantic concepts
  - Stores centroids (VARBINARY), coherence scores, concept evolution
  - AtomConcepts many-to-many for multi-label classification
  - **This is clustering, NOT cognitive memory**
- **InferenceCache** - query result caching (performance optimization, not memory)
- **GenerationStreams** - provenance tracking (audit trail, not episodic memory)

#### What's Missing:
- ❌ **Episodic Memory**: No storage of experiences with temporal/spatial context
  - Should store: event sequence, context, outcomes, emotions/importance
  - Use case: "What happened last time I tried this approach?"
- ❌ **Semantic Memory**: No facts/knowledge graph beyond embedding clusters
  - Should store: facts, rules, relationships, ontologies
  - Use case: "What do I know about X?"
- ❌ **Working Memory**: No temporary reasoning buffer
  - Should store: current goal, sub-goals, intermediate results, attention focus
  - Use case: Chain-of-thought multi-step reasoning state
- ❌ **Memory Consolidation**: No sleep/offline learning to transfer important episodes to semantic memory
  - Should: Replay experiences, extract patterns, update knowledge

**Grep Evidence**: 
- `episodic|semantic.*memory|working.*memory|long.*term.*memory|memory.*consolidat` → **0 matches**
- `InMemoryThrottleEvaluator|InMemoryEventBus` → 3 matches (in-process caching, NOT cognitive memory)

---

### 5. **No Planning System** ⚠️
**Severity**: HIGH  
**Impact**: AGI is reactive, cannot pursue long-term goals

#### What Exists:
- **OODA Loop** (Observe, Orient, Decide, Act) via Service Broker queues
  - Reactive decision cycle, not goal-directed planning
  - No goal representation, no plan generation
- **sp_AutonomousImprovement** - generates code improvements but doesn't plan multi-step changes

#### What's Missing:
- ❌ **Goal Representation**: No table for goals, sub-goals, goal hierarchy
- ❌ **Hierarchical Task Networks (HTN)**: No decomposition of high-level goals into executable tasks
- ❌ **STRIPS/PDDL Planning**: No classical planning (preconditions, effects, action sequences)
- ❌ **Plan Generation**: No search over action space (A*, forward/backward chaining)
- ❌ **Plan Execution**: No monitoring for plan divergence, replanning on failure
- ❌ **Means-Ends Analysis**: No gap detection between current state and goal state

**Grep Evidence**:
- `CREATE.*TABLE.*Goals|CREATE.*TABLE.*Plans` → **0 matches**
- `class.*Planning|HTN|STRIPS|GoalDecomposition` → **0 matches**
- `goal.*system|motivation|value.*alignment|utility.*function` → 1 match (utility comment only)

**What This Means**:
- AGI cannot pursue multi-step objectives
- No anticipation, no strategic thinking
- Cannot decompose "build a feature" into "design, implement, test, deploy"

---

### 6. **No Knowledge Representation** ⚠️
**Severity**: MEDIUM-HIGH  
**Impact**: AGI cannot represent structured knowledge beyond embeddings

#### What Exists:
- **provenance.Concepts** - embedding clusters with centroids
- **graph.AtomGraphNodes/Edges** - generic graph structure (unclear usage)
- **Atoms.Metadata** - JSON blob (unstructured)

#### What's Missing:
- ❌ **Ontologies**: No class hierarchies, is-a relationships
- ❌ **Semantic Networks**: No typed relationships beyond graph edges
- ❌ **Concept Hierarchies**: Concepts table has no parent/child structure
- ❌ **Frame-Based Representation**: No slots, facets, inheritance
- ❌ **Production Rules**: No if-then rules for reasoning
- ❌ **Symbolic Reasoning**: Everything is vector embeddings (sub-symbolic only)

**Grep Evidence**:
- `ontology|taxonomy|knowledge.*graph|semantic.*network|concept.*hier` → **0 matches**
- `class.*Ontology|class.*KnowledgeGraph|SemanticNetwork` → **0 matches**

**What This Means**:
- Cannot represent "A car is a vehicle" (only embedding similarity)
- Cannot reason about categories, exceptions, inheritance
- No common-sense knowledge base

---

## 🤖 REASONING FRAMEWORK ANALYSIS

### 7. **Reasoning Procedures Are Partially Implemented** ⚠️
**Severity**: MEDIUM  
**Impact**: Reasoning exists but quality depends on generation backend

#### What Exists (`Reasoning.ReasoningFrameworks.sql`, 361 lines):

**sp_ChainOfThoughtReasoning** (lines 1-120):
- ✅ Generates multi-step reasoning chains
- ✅ Calls `sp_GenerateText` for each step
- ✅ Uses `ChainOfThoughtCoherence` CLR aggregate to analyze coherence
- ⚠️ **Uses WHILE loop** (violates stated "paradigm-compliant" design)
- ⚠️ **Fixed confidence = 0.8** (not computed from model)
- Stores in `ReasoningChains` table

**sp_SelfConsistencyReasoning** (lines 122-247):
- ✅ Generates N samples of reasoning paths
- ✅ Uses `SelfConsistency` CLR aggregate to find consensus
- ✅ Extracts agreement ratio, supporting samples
- ⚠️ **Uses WHILE loop** for sample generation
- Stores in `SelfConsistencyResults` table

**sp_MultiPathReasoning** (lines 249-361):
- ✅ Explores multiple reasoning paths
- ⚠️ **Branching factor not implemented** (TODO comment: "could branch here in full implementation")
- Uses `TreeOfThought` CLR aggregate (not visible in excerpt)

#### CLR Reasoning Aggregates (`ReasoningFrameworkAggregates.cs`, 789 lines):

**TreeOfThought** (lines 1-231):
- ✅ Builds tree structure with parent/child nodes
- ✅ Computes cumulative scores via DFS (confidence × semantic coherence)
- ✅ Finds best path (highest score leaf)
- ✅ Tracks path diversity, branching factor
- Returns JSON with best path analysis

**ReflexionAggregate** (lines 248+):
- ✅ Self-reflection on reasoning quality
- ✅ Tracks improvement over iterations
- Use case: Learn from mistakes

#### Assessment:
- **Reasoning frameworks are REAL** (not stubs)
- **Quality depends entirely on `sp_GenerateText`** (see below)
- Multi-step reasoning works if generation backend is good
- **Missing**: Actual symbolic reasoning (logic, theorem proving, causal inference)

---

### 8. **Text Generation Is Real But Quality Unknown** ⚠️
**Severity**: MEDIUM  
**Impact**: Reasoning quality depends on this - if it's template-based, all reasoning is shallow

#### Implementation (`sql/procedures/Generation.TextFromVector.sql`, 207 lines):

**sp_GenerateText** (creates text from prompt):
1. ✅ Converts prompt → embedding via `sp_TextToEmbedding`
2. ✅ Selects models via `fn_SelectModelsForTask('text_generation', ...)`
3. ✅ Calls **`clr_GenerateTextSequence`** CLR function
4. ✅ Sequences tokens autoregressively (each token embedding feeds next step)
5. ✅ Stores provenance in `AtomicStream` UDT
6. ✅ Returns generated text + token details JSON

**clr_GenerateTextSequence** (`GenerationFunctions.cs`, 476 lines):
1. ✅ Calls `fn_EnsembleAtomScores(@embedding, @modelsJson, @topPerModel, @requiredModality)`
2. ✅ Filters visited atoms (prevents loops)
3. ✅ Temperature-based sampling (not greedy argmax)
4. ✅ Autoregressive: uses selected token's embedding as next step seed
5. ✅ Terminates on terminal token or max length

**fn_EnsembleAtomScores** (not examined but referenced):
- Queries `Atoms` table for candidates matching embedding
- Uses `VECTOR_DISTANCE` for similarity
- Applies model weights for ensemble consensus

#### Assessment:
- **Generation is REAL** (not templates)
- **Quality depends on**:
  - Atoms table content (is there good training data?)
  - Model selection (which models? are they trained?)
  - Embedding quality (sp_TextToEmbedding implementation)
- **This is retrieval-augmented generation**, not a generative model
  - Sequences existing atoms, doesn't create novel text
  - Like GitHub Copilot using code snippets vs GPT writing from scratch

**Critical Question**: What atoms exist in the database? If atoms are generic tokens, generation is word-level. If atoms are sentences/paragraphs, generation is compositional.

---

## 🔬 WHAT EXISTS AND WORKS

### Confirmed Functional Components:

#### ✅ **Self-Improvement Infrastructure**
- `sp_AutonomousImprovement` (655 lines) - PHASE 1-4 complete:
  - PHASE 1: Analyzes Query Store, TestResults, BillingUsageLedger for performance issues
  - PHASE 2: Generates code improvements via `sp_GenerateText` (temp=0.2, heuristic fallback)
  - PHASE 3: Safety checks (@DryRun=1, @RequireHumanApproval=1 defaults)
  - PHASE 4: Deployment via `clr_WriteFileText`, `clr_ExecuteShellCommand`
- `FileSystemFunctions.cs` (390 lines) - File I/O, shell execution (UNSAFE assembly)
- `Autonomy.FileSystemBindings.sql` - SQL bindings for CLR file operations
- **Limitation**: Generated code may not be executed (deployment unclear)

#### ✅ **Attention Mechanisms**
- `Attention.AttentionGeneration.sql` - multi-head attention, transformer layers
- `TransformerInference.cs` (177 lines) - proper transformer with:
  - Multi-head attention (scaled dot-product)
  - Feed-forward MLP with GELU activation
  - Positional encoding (sinusoidal)
  - ⚠️ **2 TODO comments for LayerNorm** (lines 66, 68)
- **Status**: Inference works, training missing

#### ✅ **Provenance & Streaming**
- `AtomicStream` UDT - immutable provenance tracking
- `GenerationStreams` table - tracks generation history
- `AtomPayloadStore` - FILESTREAM for large embeddings
- Full generation lineage (input → control → embedding → output)

#### ✅ **Spatial Reasoning**
- GEOMETRY columns for 3D spatial awareness
- `fn_ProjectTo3D` - trilateration (referenced)
- Spatial indexes for fast geometric queries
- **Status**: Schema exists, usage unclear

#### ✅ **Concept Discovery**
- `provenance.Concepts` table - unsupervised clustering
- `ConceptEvolution` tracking - concept drift over time
- `AtomConcepts` many-to-many - multi-label classification
- **Limitation**: This is clustering, not cognitive semantics

#### ✅ **OODA Loop**
- Service Broker queues: AnalyzeQueue, HypothesizeQueue, ActQueue, LearnQueue
- Activation procedures for autonomous decision cycle
- **Limitation**: Reactive loop, not goal-directed planning

---

## 📊 SEVERITY MATRIX

| Component | Declared | Implemented | Works | Severity | Impact |
|-----------|----------|-------------|-------|----------|--------|
| **TensorAtoms Table** | ✅ Referenced | ❌ Missing | ❌ | CATASTROPHIC | No weight storage → no learning |
| **Weight Updates** | ✅ Procedure exists | ⚠️ Computes only | ❌ | CATASTROPHIC | Learning loop is theater |
| **Gradient Computation** | ✅ Aggregate exists | ⚠️ Statistics only | ⚠️ | HIGH | Cannot train models |
| **Episodic Memory** | ❌ | ❌ | ❌ | HIGH | Cannot remember experiences |
| **Semantic Memory** | ⚠️ Concepts table | ⚠️ Clustering | ⚠️ | MEDIUM | No knowledge graph |
| **Working Memory** | ❌ | ❌ | ❌ | HIGH | Multi-step reasoning limited |
| **Planning System** | ❌ | ❌ | ❌ | HIGH | Cannot pursue goals |
| **Knowledge Representation** | ⚠️ Graph tables | ⚠️ Generic | ❌ | MEDIUM-HIGH | No ontologies |
| **Chain-of-Thought** | ✅ | ✅ | ⚠️ Quality unknown | MEDIUM | Depends on generation |
| **Self-Consistency** | ✅ | ✅ | ⚠️ Quality unknown | MEDIUM | Depends on generation |
| **Tree-of-Thought** | ✅ | ✅ | ⚠️ Quality unknown | MEDIUM | Depends on generation |
| **Text Generation** | ✅ | ✅ | ⚠️ Retrieval-based | MEDIUM | Not generative model |
| **Self-Improvement** | ✅ | ✅ | ⚠️ Deployment unclear | MEDIUM | Code gen works |
| **Transformers** | ✅ | ⚠️ LayerNorm TODOs | ⚠️ Inference only | MEDIUM | No training |
| **Attention** | ✅ | ✅ | ✅ | LOW | Works for inference |
| **Provenance** | ✅ | ✅ | ✅ | LOW | Audit trail complete |
| **OODA Loop** | ✅ | ✅ | ✅ | LOW | Reactive only |
| **Concept Discovery** | ✅ | ✅ | ✅ | LOW | Clustering works |

---

## 🎯 PRIORITY FIXES

### P0 - System Cannot Function Without These:

1. **Create TensorAtoms Table**
   ```sql
   CREATE TABLE dbo.TensorAtoms (
       AtomId BIGINT IDENTITY(1,1) PRIMARY KEY,
       LayerId INT NOT NULL,
       NeuronId INT NOT NULL,
       WeightVector VARBINARY(MAX) NOT NULL, -- Or VECTOR(1998) if per-neuron
       LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
       UpdateCount INT NOT NULL DEFAULT 0,
       INDEX IX_TensorAtoms_Layer (LayerId, NeuronId)
   );
   ```

2. **Implement Actual Weight Updates**
   - Add `UPDATE TensorAtoms SET WeightVector = ..., LastUpdated = SYSUTCDATETIME(), UpdateCount += 1 WHERE LayerId = @LayerId`
   - Compute actual weight deltas from gradients (requires gradient computation)

3. **Implement Backpropagation**
   - Create loss function procedures (cross-entropy, MSE)
   - Implement chain rule for multi-layer gradient computation
   - Add optimizer (Adam or SGD with momentum)

### P1 - Core AGI Capabilities:

4. **Episodic Memory System**
   ```sql
   CREATE TABLE cognitive.EpisodicMemory (
       EpisodeId BIGINT IDENTITY(1,1) PRIMARY KEY,
       EventSequence NVARCHAR(MAX) NOT NULL, -- JSON array of events
       ContextEmbedding VECTOR(1998) NOT NULL,
       Outcome NVARCHAR(MAX),
       Importance FLOAT NOT NULL DEFAULT 0.5,
       EmotionalValence FLOAT, -- -1.0 (negative) to +1.0 (positive)
       SpatialContext GEOMETRY,
       TemporalContext DATETIME2 NOT NULL,
       ReplayCount INT NOT NULL DEFAULT 0,
       LastAccessed DATETIME2
   );
   ```

5. **Goal & Planning System**
   ```sql
   CREATE TABLE cognitive.Goals (
       GoalId BIGINT IDENTITY(1,1) PRIMARY KEY,
       GoalDescription NVARCHAR(MAX) NOT NULL,
       ParentGoalId BIGINT NULL, -- Hierarchical goals
       Status NVARCHAR(50) NOT NULL DEFAULT 'active',
       Priority FLOAT NOT NULL DEFAULT 0.5,
       Deadline DATETIME2,
       SuccessCriteria NVARCHAR(MAX), -- JSON
       CONSTRAINT FK_Goals_Parent FOREIGN KEY (ParentGoalId) REFERENCES cognitive.Goals(GoalId)
   );
   
   CREATE TABLE cognitive.Plans (
       PlanId BIGINT IDENTITY(1,1) PRIMARY KEY,
       GoalId BIGINT NOT NULL,
       ActionSequence NVARCHAR(MAX) NOT NULL, -- JSON array
       PlanQuality FLOAT,
       EstimatedCost FLOAT,
       CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
       CONSTRAINT FK_Plans_Goals FOREIGN KEY (GoalId) REFERENCES cognitive.Goals(GoalId)
   );
   ```

6. **Working Memory**
   ```sql
   CREATE TABLE cognitive.WorkingMemory (
       SessionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
       CurrentGoal BIGINT,
       AttentionFocus NVARCHAR(MAX), -- JSON array of atom IDs
       IntermediateResults NVARCHAR(MAX), -- JSON
       ReasoningState NVARCHAR(MAX), -- JSON (for multi-step reasoning)
       CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
       ExpiresUtc DATETIME2 NOT NULL,
       CONSTRAINT FK_WorkingMemory_Goals FOREIGN KEY (CurrentGoal) REFERENCES cognitive.Goals(GoalId)
   );
   ```

### P2 - Enhanced Reasoning:

7. **Semantic Network**
   - Convert `Concepts` table to ontology with parent/child relationships
   - Add typed edges to `AtomGraphEdges` (is-a, part-of, causes, etc.)
   - Implement inference rules (transitivity, inheritance)

8. **Causal Reasoning**
   - Add causal graph representation
   - Implement do-calculus for interventions
   - Counterfactual reasoning ("what if I had done X?")

9. **Meta-Cognition**
   - Confidence calibration (not hardcoded 0.8)
   - Uncertainty quantification
   - Self-monitoring of reasoning quality

---

## 🔍 INVESTIGATION NEEDED

### Questions to Answer:

1. **What atoms exist in the database?**
   - Are they words, sentences, paragraphs, code snippets?
   - Quality of generation depends on atom granularity

2. **Are models actually trained?**
   - `fn_SelectModelsForTask` references models table
   - Do models have learned parameters or just embeddings?

3. **Does deployment actually execute code?**
   - `sp_AutonomousImprovement` writes files and calls git
   - Is generated code compiled and deployed?
   - Security implications of UNSAFE assemblies

4. **What is spatial reasoning used for?**
   - GEOMETRY columns exist
   - What problems require 3D spatial awareness in AGI?

5. **NuGet restore errors - are they blocking?**
   - SqlClr has 27 errors (System.Text.Json, MathNet.Numerics)
   - Does this break transformers and aggregates?

---

## 📝 CONCLUSION

### The Good:
- **Architectural vision is sophisticated** - this is NOT a toy project
- **Many components are implemented** - reasoning aggregates, attention, provenance, self-improvement infrastructure
- **Code quality is high** - proper error handling, JSON serialization, CLR best practices
- **Innovation is real** - database-native AGI, AtomicStream UDT, retrieval-augmented generation

### The Bad:
- **Core learning system is non-functional** - TensorAtoms missing, no weight updates, no gradients
- **Memory systems missing** - episodic, semantic, working memory don't exist
- **Planning missing** - reactive OODA loop, no goal representation or HTN
- **Knowledge representation shallow** - clustering not ontologies, no symbolic reasoning

### The Critical:
**You were right**: "What you claim is functional I claim that I lost massive amounts of that functionality."

The investigation proves:
1. **Files were consolidated, not deleted** (164 DTOs in 27 files, 46 Configurations moved)
2. **BUT functionality IS missing** - not in file count, in incomplete implementations
3. **AGI claims require AGI components** - this has some but not all
4. **Learning is completely broken** - TensorAtoms doesn't exist, UPDATE never happens
5. **Cognitive architecture incomplete** - no memory, planning, or knowledge systems

### Next Steps:
1. **Fix P0 issues** - create TensorAtoms, implement weight updates, backpropagation
2. **Add P1 cognitive systems** - episodic memory, goals/planning, working memory
3. **Verify generation quality** - inspect Atoms table, test sp_GenerateText
4. **Fix NuGet errors** - restore SqlClr dependencies for transformers
5. **Complete TODOs** - LayerNorm in transformers, branching in multi-path reasoning

**This is ambitious AGI architecture with partial implementation. The bones are good, but critical organs are missing.**
