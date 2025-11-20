# 04 - Orchestration Layer: T-SQL Pipelines

The application code (C#, Python, etc.) does not contain the core AI logic. Instead, it makes calls to a well-defined API exposed by the database in the form of T-SQL stored procedures. These procedures are the orchestration layer, responsible for executing complex, multi-step AI workflows—or "pipelines"—that operate on the atomic data model.

## 1. The Stored Procedure as an API Endpoint

Every primary operation in the Hartonomous engine is initiated by executing a stored procedure. This approach has several key advantages:

-   **Encapsulation:** The complexity of an AI task (e.g., running inference) is hidden behind a simple `EXEC` command. The calling application only needs to provide the inputs, not understand the internal mechanics.
-   **Performance:** It minimizes data transfer between the application and the database. The entire workflow runs "close to the data," avoiding the latency of pulling large datasets into application memory.
-   **Security:** Permissions can be managed at the procedure level, granting applications the ability to execute specific tasks without giving them broad access to the underlying tables.
-   **Atomicity:** A stored procedure can wrap an entire pipeline in a single transaction, ensuring that complex operations either succeed or fail as a single, atomic unit.

## 2. Anatomy of a Modern AI Pipeline: The O(log N) + O(K) Model

The Hartonomous pipeline avoids the `O(N^2)` complexity of traditional attention mechanisms by splitting the work between an efficient database lookup and subsequent processing on a small subset of data. Each step in a process is a `O(log N) + O(K)` operation.

### The `O(log N)` Step: The Multi-Stage Navigational Query (T-SQL)
The first and most critical part of the process is a **multi-stage navigational query** executed in T-SQL. This query uses a cascade of increasingly precise filters to efficiently find a small set of `K` relevant candidates from the entire dataset of `N` atoms. This is the architectural breakthrough that makes the system scalable.

#### Stage 1: Geometric Pre-Filtering (Coarse Grained)
Instead of immediately performing a vector search across the entire table, the query first executes a very fast spatial search on the indexed `SpatialGeometry` column.

-   **Technique:** This can use techniques like **trilateration** (finding a location based on distances to known landmark atoms) or a simple radius search (`STIntersects` with a buffered geometry).
-   **Benefit:** This query uses a highly efficient R-Tree spatial index to instantly discard millions or billions of irrelevant atoms, identifying a small "candidate zone" with a few thousand potential matches.

#### Stage 2: Vector Refinement (Fine Grained)
The more computationally expensive `VECTOR_DISTANCE` function is only executed on the small set of candidates that passed the initial geometric filter.

-   **Technique:** The query takes the candidates from Stage 1 and calculates their precise semantic similarity using their full-dimensional vectors.
-   **Benefit:** This reserves the most expensive part of the query for a tiny subset of the data, making the overall operation extremely fast.

```sql
-- This multi-stage query is the O(log N) operation.
WITH GeometricCandidates AS (
    -- Stage 1: Use the fast spatial index to get a small candidate pool.
    SELECT TOP (1000) AtomId, EmbeddingVector
    FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry.STIntersects(@search_area_geometry) = 1
),
VectorCandidates AS (
    -- Stage 2: Run the more expensive vector search ONLY on the small pool.
    SELECT
        g.AtomId,
        VECTOR_DISTANCE('cosine', g.EmbeddingVector, @context_vector) AS Score
    FROM GeometricCandidates g
)
-- Select the final, most relevant candidates.
SELECT TOP (@K) AtomId, Score
FROM VectorCandidates
ORDER BY Score;
```

The result of this multi-stage query is a small, highly-relevant set of `K` candidate `AtomId`s.

### The `O(K)` Step: The Refinement (CLR)
This small set of `K` candidates is then passed to the SQL CLR for more complex, procedural processing. This can include the sophisticated filtering, trilateration, A* pathfinding, or iterative generation logic you have designed. Because the CLR is only operating on a small set `K`, its work is fast and efficient. This is the `O(K)` part of the model.

This division of labor is key: the database does what it does best (indexed lookups on massive datasets), and the CLR does what it does best (complex procedural logic on small datasets).

## 3. Key Procedures & Functions

-   `dbo.sp_FindNearestAtoms`: A procedure that implements the `O(log N)` search, taking a context vector and returning a set of `K` candidate atoms.
-   `dbo.fn_GenerateFromCandidates`: A CLR function that takes the `K` candidates and performs the `O(K)` processing, such as iterative generation or pathfinding, to produce the final result.

## 4. T-SQL Best Practices

To ensure the T-SQL orchestration layer is performant, secure, and maintainable, the following modern design patterns should be strictly followed.

### Use Common Table Expressions (CTEs) for Readability
The `O(log N)` navigational queries can become complex, involving multiple joins, sub-queries, and filtering stages. To manage this complexity, queries should be structured using **Common Table Expressions (CTEs)**.

-   **Benefit:** CTEs break down a monolithic query into a series of logical, readable steps. This makes the query easier to understand, debug, and maintain. For example, one CTE can gather initial candidates, a second can perform filtering, and a third can calculate final scores.

```sql
-- Example of a structured query using CTEs
WITH CandidateAtoms AS (
    -- Step 1: Get initial candidates using an index (O(log N))
    SELECT TOP (100) AtomId, EmbeddingVector
    FROM dbo.AtomEmbeddings
    WHERE ...
),
ScoredCandidates AS (
    -- Step 2: Score the candidates
    SELECT
        c.AtomId,
        VECTOR_DISTANCE('cosine', c.EmbeddingVector, @context_vector) AS Score
    FROM CandidateAtoms c
)
-- Step 3: Select the final set
SELECT TOP (@K) AtomId, Score
FROM ScoredCandidates
ORDER BY Score;
```

### Use `sp_executesql` for Dynamic Queries
In scenarios where parts of the query must be constructed at runtime (e.g., searching across a dynamic set of tables or columns), it is critical to do so safely and efficiently.

-   **Best Practice:** Always use the `sp_executesql` system stored procedure with strongly-typed parameters.
-   **Security:** This is the **only** safe way to execute dynamic SQL. It prevents SQL injection attacks by properly parameterizing user input.
-   **Performance:** Using `sp_executesql` allows SQL Server to cache and reuse the execution plan for the dynamic query, which significantly improves performance for frequently called procedures.
-   **Anti-Pattern:** **Never** build a query by directly concatenating strings from user input and executing it with `EXEC()`. This is a major security vulnerability and hurts performance.
