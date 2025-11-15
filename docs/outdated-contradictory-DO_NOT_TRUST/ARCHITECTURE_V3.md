# Hartonomous Architecture V3: The Definitive Master Plan

**Document Status: Authoritative**
**Version:** 3.0
**Date:** November 14, 2025

## 1. Introduction & Mandate

This document defines the official V3 architecture for the Hartonomous project. It is the culmination of a comprehensive historical analysis of the repository and a synthesis of the best architectural ideas from all previous versions. **This document supersedes all previous master plans and architectural documentation.**

The project's history has been marked by architectural churn and incomplete, undocumented refactoring efforts. This has resulted in a broken, inconsistent codebase. The mandate of the V3 architecture is to correct this by establishing a single, stable, and well-documented foundation for all future development.

This is not a report on the past; it is a clear, justified, and actionable plan for the future.

---

## 2. The V3 Architecture: A "Best of All Worlds" Synthesis

The V3 architecture is a hybrid model, pragmatically selecting the most robust and performant features from previous designs while correcting their flaws.

### 2.1. Multi-Tenancy: The Hybrid Model

**Decision:** A hybrid model combining direct ownership with a junction table for sharing.

*   **`dbo.Atoms` will have a `TenantId INT NOT NULL` column.** This column denotes the "owner" of the atom. A default value of `0` will represent a "system" or "public" tenant.
*   **The `dbo.TenantAtoms` junction table will be used for sharing.** It creates a many-to-many relationship, allowing an atom owned by one tenant to be explicitly shared with others.

**Justification:**

This model provides the "best of all worlds":

*   **Security:** Restores strong, database-level security. The `TenantId` on `dbo.Atoms` allows for simple and effective Row-Level Security (RLS) policies, preventing accidental data leakage at the lowest level.
*   **Performance:** The most common query—a tenant fetching their own data—is fast and efficient, requiring a simple `WHERE` clause on an indexed column, not a costly `JOIN`.
*   **Flexibility:** The junction table is retained for the advanced use case of sharing data between tenants, which was the only valid goal of the flawed V5 refactor.
*   **Clarity:** The ownership model is now unambiguous. `Atoms.TenantId` is the owner. `TenantAtoms` is for sharing.

**Example Query:**

To get all atoms accessible by a tenant (both owned and shared):

```sql
DECLARE @CurrentTenantId INT = 123;

SELECT a.*
FROM dbo.Atoms AS a
WHERE
    -- The atom is owned by the current tenant
    a.TenantId = @CurrentTenantId
    OR
    -- The atom has been explicitly shared with the current tenant
    EXISTS (
        SELECT 1
        FROM dbo.TenantAtoms AS ta
        WHERE ta.AtomId = a.AtomId AND ta.TenantId = @CurrentTenantId
    );
```

### 2.2. Embedding & Search: Flexible and Fast

**Decision:** Restore `EmbeddingType` for flexibility while retaining `SpatialKey` for performance.

*   **`dbo.AtomEmbeddings` will have an `EmbeddingType NVARCHAR(50) NOT NULL` column.** This restores the ability to categorize embeddings based on their semantic meaning (e.g., "semantic", "syntactic", "image_rgb") rather than being rigidly tied to a `ModelId`.
*   **`dbo.AtomEmbeddings` will keep the `SpatialKey GEOMETRY NOT NULL` column.** The performance gains from spatial indexing are non-negotiable for a scalable system.
*   **The monolithic `EmbeddingVector` column will be removed.** The V5 plan was correct to eliminate the storage of large, raw vectors in the main table. The long-term vision of "atomic decomposition" of vectors is valid but will be deferred to focus on stabilizing the system.

**Justification:**

*   **Flexibility:** Restoring `EmbeddingType` is critical. It decouples the "what" (the meaning of the embedding) from the "how" (the model that created it), allowing for a much richer and more adaptable data model.
*   **Performance:** Keeping `SpatialKey` ensures that semantic search remains highly performant and scalable, a key feature of the platform.
*   **Pragmatism:** This approach delivers the most important features (flexible categorization and fast search) without the immense complexity of implementing full vector decomposition immediately.

### 2.3. Data Ingestion: Governed and Chunked

**Decision:** Adopt the "Governed, Chunked Ingestion" model from the `LATEST_MASTER_PLAN.md`.

*   **`IngestionJobs` Table:** All large-object ingestion will be managed via a record in the `dbo.IngestionJobs` table.
*   **CLR Streamer & T-SQL Governor:** The ingestion process will use a SQL CLR function to stream chunks of the source file to a "governor" stored procedure. This procedure will process the data in small, fast, and resumable transactions.

**Justification:**

This is the only model presented in the repository's history that is suitable for an enterprise-grade system.

*   **Scalability & Reliability:** It is the only design that can handle multi-gigabyte source files without overwhelming the database. Its resumable nature is essential for robust error handling.
*   **Governance:** It provides a necessary control plane for managing resource consumption and preventing abuse.
*   **Architectural Consistency:** It is the only ingestion model that correctly implements the project's core philosophy of "atomic decomposition."

---

## 3. Architectural Violations to Be Corrected

The Phase 1 audit revealed several critical violations of Clean Architecture principles in the C# codebase. The V3 plan includes correcting these:

1.  **Decouple `Core` from Data Layers:** `Hartonomous.Core` will be refactored to remove its dependencies on `Hartonomous.Data.Entities` and Entity Framework Core.
2.  **Introduce a True Domain Layer:** The `Core` project will contain pure domain models and business logic, with no knowledge of persistence.
3.  **Strengthen the `Infrastructure` Layer:** All data persistence logic, EF Core configurations, and repository implementations will reside solely in the `Infrastructure` project.

---

## 4. High-Level Migration Plan

This section outlines the major steps required to migrate the repository from its current broken state to the V3 architecture.

**Phase 1: Database Schema Correction (SQL)**

1.  **Add `TenantId` to `dbo.Atoms`:** Modify the table definition to include the `TenantId INT NOT NULL DEFAULT 0` column.
2.  **Add `EmbeddingType` to `dbo.AtomEmbeddings`:** Modify the table definition to include the `EmbeddingType NVARCHAR(50) NOT NULL` column.
3.  **Remove `TenantAtoms` Junction Table (Temporarily):** To simplify the initial migration, we will drop the `TenantAtoms` table. It can be added back later once the core tenancy model is stable.
4.  **Audit and Correct Stored Procedures:** Systematically review and fix all stored procedures (`sp_SemanticSearch`, etc.) that were broken by the V5 changes, re-introducing correct `TenantId` and `EmbeddingType` logic.

**Phase 2: Application Layer Refactoring (C#)**

1.  **Fix Architectural Violations:** Refactor the `Core`, `Infrastructure`, and `Data` projects to enforce Clean Architecture principles.
2.  **Update Data Services:** Rewrite `AtomIngestionService`, `SemanticSearchService`, and other services to work with the corrected V3 database schema.
3.  **Implement Governed Ingestion:** Implement the C# components required to interact with the new `IngestionJobs` system.

**Phase 3: Documentation & Finalization**

1.  **Archive Old Documents:** Move all outdated architectural documents to an `archive` folder.
2.  **Update `README.md`:** The primary `README.md` files will be updated to point to this `ARCHITECTURE_V3.md` as the single source of truth.
3.  **Full Build & Test:** Perform a full build and run all tests to ensure the migrated system is stable and functional.

This plan provides a clear path to a stable, well-architected, and well-documented system.
