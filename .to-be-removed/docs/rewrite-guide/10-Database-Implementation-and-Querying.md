# 10 - Database Implementation and Querying

This document details the specific, optimal implementation strategies for the `Hartonomous.Database` project. It moves from the high-level data model to the physical implementation required to ensure performance, scalability, and maintainability. These are not suggestions; they are the prescribed patterns for the rewrite.

## 1. Comprehensive Indexing Strategy

A multi-faceted indexing strategy is required to support the system's mixed read/write workloads.

-   **Referential Integrity:** All relationships between tables (e.g., `AtomRelations` to `Atoms`) **must** be enforced with Foreign Key constraints. The foreign key columns themselves must be indexed (non-clustered B-Tree) to ensure the performance of joins.

-   **B-Tree Indexes (Rowstore):**
    -   **Clustered Index:** The primary key on most tables will be a standard clustered index on an `Id` column.
    -   **Non-Clustered Indexes:** A unique non-clustered index **must** be placed on `dbo.Atoms(AtomHash)` for fast content-addressable lookups. Other frequently queried columns should have non-clustered indexes as needed.

-   **Spatial (R-Tree) Indexes:** A spatial index **must** be created on the `dbo.AtomEmbeddings.SpatialGeometry` column. This index is critical for the "Stage 1 Geometric Pre-Filtering" of the multi-stage navigational query.

-   **Vector Indexes:** A vector index **must** be created on the `dbo.AtomEmbeddings.EmbeddingVector` column. This index, using the DiskANN algorithm, is used for the "Stage 2 Vector Refinement" part of the navigational query.

-   **Columnstore Indexes:** To enable real-time operational analytics without impacting transactional performance, non-clustered columnstore indexes **must** be created on large, analytical-heavy tables like `dbo.AtomRelations` and tables within the `provenance` schema.

## 2. Table Partitioning Strategy

The `dbo.Atoms` table, as the central repository for all data, will grow to be massive. To ensure its manageability and performance, it **must** be partitioned.

-   **Partition Key:** The table will be partitioned by `ContentTypeId`.
-   **Benefits:** This allows for index maintenance to be performed on a per-partition basis. It also allows the query optimizer to perform partition elimination, scanning only the relevant partitions for queries that filter by content type, which significantly improves performance.

## 3. Hekaton (In-Memory OLTP) for Caching

Hekaton's role is strictly for ephemeral, high-speed data, not primary storage.

-   **Use Case:** Hekaton will be used for specific caching and session state tables, as evidenced by existing tables like `InferenceCache_InMemory`. This provides microsecond-level access for performance-critical internal operations.
-   **Implementation:** Logic operating on these tables should be encapsulated in **natively compiled stored procedures** for maximum performance.

## 4. The Canonical Navigational Query

All semantic search operations **must** use the prescribed multi-stage navigational query pattern. This ensures that the efficient spatial index is always used to pre-filter candidates before the more expensive vector search is performed.

The query will be structured with Common Table Expressions (CTEs) for readability and maintainability.

**Required T-SQL Template:**
```sql
-- All semantic searches MUST follow this two-stage pattern.
WITH GeometricCandidates AS (
    -- Stage 1: Use the fast spatial R-Tree index to get a small candidate pool.
    -- This query should be tailored (trilateration, radius search) to the use case.
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

Adherence to these implementation patterns is not optional; it is critical to achieving the performance and scalability goals of the Hartonomous platform.
