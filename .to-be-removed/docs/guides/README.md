# User Guides

Tutorials, examples, and how-to guides for using Hartonomous.

---

## Getting Started

- **[Quickstart Guide](../../QUICKSTART.md)** - Get running in 5 minutes
- **First Steps** - Your first spatial query and generation (coming soon)
- **Understanding Spatial Navigation** - How O(log N) queries work (coming soon)

---

## Reasoning Frameworks

**Using Chain of Thought**:
- When to use linear step-by-step reasoning
- Example: Solving math problems step by step
- Analyzing coherence scores
- **See**: [20-Reasoning-Frameworks-Guide.md](../rewrite-guide/20-Reasoning-Frameworks-Guide.md)

**Using Tree of Thought**:
- When to explore multiple reasoning paths
- Example: Creative problem solving with parallel approaches
- Evaluating and selecting best paths
- **See**: [20-Reasoning-Frameworks-Guide.md](../rewrite-guide/20-Reasoning-Frameworks-Guide.md)

**Using Reflexion**:
- When to use consensus-based reasoning
- Example: Verifying factual answers via self-consistency
- Interpreting agreement ratios
- **See**: [20-Reasoning-Frameworks-Guide.md](../rewrite-guide/20-Reasoning-Frameworks-Guide.md)

---

## Cross-Modal Synthesis

**Complete Examples** (see [22-Cross-Modal-Generation-Examples.md](../rewrite-guide/22-Cross-Modal-Generation-Examples.md)):

1. **"Generate audio that sounds like this image"**
   - Image → find nearby audio atoms → synthesize harmonics
   - Implementation: Hybrid retrieval + synthesis

2. **"Write poem about this video"**
   - Video frames → visual centroid → find text atoms → Chain of Thought
   - Implementation: Cross-modal + reasoning frameworks

3. **"Create image representing this code"**
   - Code AST → find image atoms → synthesize guided patches
   - Implementation: Code embedding + geometric diffusion

4. **"What does this silent film sound like?"**
   - Video motion → find audio atoms → synthesize soundtrack
   - Implementation: Visual semantics guide audio synthesis

---

## Agent Tools Framework

**Using Agent Tools**:
- How the AgentTools registry works
- Semantic tool selection via spatial proximity
- Dynamic execution with JSON parameters
- **See**: [21-Agent-Framework-Guide.md](../rewrite-guide/21-Agent-Framework-Guide.md)

**Adding Custom Tools**:
- Creating new stored procedures
- Registering in AgentTools table
- Generating tool embeddings
- Example: Custom diagnostics tool

---

## Working with Spatial Queries

**Basic Spatial Query**:
```sql
-- Find atoms near a query point
DECLARE @Query GEOMETRY = geometry::Point(10, 20, 5, 0);

SELECT TOP 10 *
FROM dbo.AtomEmbeddings
WHERE SpatialGeometry.STIntersects(@Query.STBuffer(10)) = 1
ORDER BY SpatialGeometry.STDistance(@Query);
```

**Cross-Modal Query**:
```sql
-- Find images related to text query
EXEC dbo.sp_CrossModalQuery
    @QueryText = 'beautiful sunset',
    @Modalities = 'image',
    @TopK = 20,
    @SessionId = @SessionId;
```

**Geometric Range Query**:
```sql
-- Find all atoms in a bounding box
DECLARE @BoundingBox GEOMETRY = geometry::STGeomFromText('POLYGON((0 0, 100 0, 100 100, 0 100, 0 0))', 0);

SELECT *
FROM dbo.AtomEmbeddings
WHERE SpatialGeometry.STIntersects(@BoundingBox) = 1;
```

---

## Understanding the OODA Loop

**Monitoring Self-Improvement**:
```sql
-- Check OODA loop activity
SELECT Phase, COUNT(*) AS Executions, AVG(DurationMs) AS AvgDuration
FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY Phase;

-- Check generated hypotheses
SELECT HypothesisType, COUNT(*) AS Count
FROM dbo.PendingActions
WHERE CreatedAt >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY HypothesisType;
```

**Understanding Hypothesis Types**:
1. **IndexOptimization**: Creates/rebuilds spatial indexes automatically
2. **QueryRegression**: Fixes slow queries by forcing good plans
3. **CacheWarming**: Preloads frequently accessed atoms
4. **ConceptDiscovery**: Identifies new semantic clusters
5. **PruneModel**: Removes low-importance model weights
6. **RefactorCode**: Detects duplicate code patterns
7. **FixUX**: Analyzes user behavior geometrically for UX issues

---

## Working with Provenance

**Tracing Inference Results**:
```cypher
// Neo4j query: Trace how output was generated
MATCH (inference:Inference {inferenceId: $id})
      -[:HAD_INPUT]->(inputs:Atom)
MATCH (inference)-[:GENERATED]->(output:Atom)
MATCH (inference)-[:USED_REASONING]->(reasoning:ReasoningChain)
RETURN inference, inputs, output, reasoning;
```

**Root Cause Analysis**:
```cypher
// What source created this atom?
MATCH path = (source:Source)<-[:INGESTED_FROM*]-(atom:Atom {atomId: $id})
RETURN path;
```

**Impact Analysis**:
```cypher
// What depends on this atom?
MATCH path = (atom:Atom {atomId: $id})-[:HAD_INPUT*]->(dependent)
RETURN path;
```

---

## Tutorials (Coming Soon)

**Beginner**:
- Tutorial 1: Your first spatial query
- Tutorial 2: Using Chain of Thought reasoning
- Tutorial 3: Cross-modal search (text → images)
- Tutorial 4: Understanding provenance

**Intermediate**:
- Tutorial 5: Creating custom agent tools
- Tutorial 6: Cross-modal synthesis (image → audio)
- Tutorial 7: Analyzing user behavior geometrically
- Tutorial 8: Monitoring OODA loop performance

**Advanced**:
- Tutorial 9: Implementing custom reasoning frameworks
- Tutorial 10: Multi-model ensemble queries
- Tutorial 11: Student model extraction
- Tutorial 12: Gödel engine for long-running computation

---

## Best Practices

**Query Performance**:
- Always use spatial index hints: `WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))`
- Buffer size affects candidate count: smaller buffer = fewer candidates = faster
- Two-stage pattern: spatial pre-filter → exact vector refinement

**Reasoning Frameworks**:
- Use Chain of Thought for step-by-step problems (math, logic)
- Use Tree of Thought for creative/exploratory problems
- Use Reflexion for factual verification

**Cross-Modal Synthesis**:
- Retrieval for finding existing content
- Synthesis for generating new content
- Hybrid (retrieval + synthesis) for guided generation

**OODA Loop**:
- Let it run automatically (don't micromanage)
- Monitor via OODALoopMetrics table
- Review PendingActions for dangerous operations before approval

---

## Example Projects (Coming Soon)

- **Semantic Search Engine**: Build search across text, images, and audio
- **Creative Assistant**: Use reasoning frameworks for story generation
- **Code Analysis Tool**: Analyze codebases with AST embeddings
- **UX Analytics Platform**: Geometric behavioral analysis
- **Multi-Modal Chatbot**: Cross-modal question answering

---

## Additional Resources

**Technical Details**:
- [Complete Rewrite Guide](../rewrite-guide/) - Full technical specification
- [Architecture](../../ARCHITECTURE.md) - System architecture overview
- [API Reference](../api/) - Complete API documentation

**Operations**:
- [Operations Runbooks](../operations/) - Monitoring and troubleshooting
- [Setup Guides](../setup/) - Installation and configuration

**Community** (Coming Soon):
- Discord server
- GitHub discussions
- Example repository
- Video tutorials
