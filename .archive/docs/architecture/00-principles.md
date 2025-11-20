# 00 - Architectural Principles

This document defines the foundational architectural principles for the Hartonomous rewrite. These principles are law and will govern all subsequent design and implementation decisions.

## 1. The Engine is the Database

The core of the Hartonomous platform is not in the C# application code; it is the **Spatio-Semantic AI Engine** implemented directly within the `Hartonomous.Database` project. This embodies a **Database-First Architecture**, where the database is not merely a passive persistence layer, but the active, central component of the system's logic and computation.

-   **Core Logic:** All primary AI logic—including data atomization, semantic projection to geometric space, and inference—is executed via T-SQL stored procedures, functions, and SQL CLR components.
-   **Specialized Technologies:** The engine is a sophisticated orchestration of specialized SQL Server technologies:
    -   **Spatial `GEOMETRY` Types:** The foundation for representing and querying semantic data.
    -   **Spatial Indexes:** The primary mechanism for AI inference and similarity search.
    -   **Columnstore Indexes:** Used for large-scale analytics on atom metadata.
    -   **Hekaton (In-Memory OLTP):** Used for extreme low-latency data access where applicable.
    -   **SQL CLR:** The high-performance execution engine for complex, procedural, or row-by-row algorithms that are inefficient in set-based T-SQL.
-   **C# Role:** The C# application layers serve as orchestrators and access layers to the database engine, not as the engine itself.

## 2. Atomic Granularity & Content-Addressable Storage (CAS)

The system is built on the concept of deconstructing all information into its smallest, meaningful, and verifiable components. This approach aligns perfectly with the principles of **Content-Addressable Storage (CAS)**, ensuring data integrity and inherent deduplication.

-   **Atomization:** All content (files, models, data streams) is broken down into its fundamental components, or "atoms." The granularity is variable and defined by the logical structure of the content (e.g., a file header, a single floating-point number from a model weight).
-   **Content-Addressing:** Every unique atom is identified by a cryptographic hash of its content (e.g., SHA-256). This is its primary key.
-   **Deduplication:** An atom's raw data is stored only once. All subsequent references to that atom point to the existing, content-addressed entry.

## 3. Spatio-Semantic Representation

The core innovation is the representation of semantic meaning as geometric position. This is a form of **Spatio-Semantic Modeling**, where the system explicitly combines spatial information (geometry) with semantic meaning to enable novel forms of querying and inference.

-   **Dimensionality Reduction is Solved:** The "curse of dimensionality" is sidestepped by not indexing high-dimensional vectors directly. Instead, we atomize the data and project these low-dimensional atoms into a 2D/3D `GEOMETRY` space.
-   **Semantic Proximity is Spatial Proximity:** The semantic similarity between two concepts is a direct function of the geometric distance (`STDistance()`) between their corresponding atoms in the spatial index.
-   **Inference as Navigation:** AI queries are transformed from vector similarity searches into geometric navigation and pathfinding problems on a semantic map.

## 4. Verifiable Provenance (The "Black Box" Solution)

The system must be transparent, auditable, and deterministic. Neo4j is the primary tool for achieving this by creating a **Verifiable Provenance Graph**. This aligns with modern data governance patterns where graph databases are used to model and query the lineage of data.

-   **Merkle DAG:** The relationships between atoms, sources, and transformations form a cryptographically verifiable Merkle DAG.
-   **Immutable History:** Neo4j stores the immutable graph of provenance, linking every atom to its source, the user who ingested it, and the specific version of the algorithm that created it.
-   **Auditability:** Any piece of data can be traced back through its entire lifecycle, providing a complete, tamper-evident audit trail.

## 5. Strict Vertical Separation & Dependency Rule

The solution is organized into clean, vertically separated layers with an inviolable dependency rule. This is a direct implementation of well-established patterns like **Hexagonal Architecture (Ports and Adapters)** and **Clean Architecture**.

-   **Layers:**
    -   `Hartonomous.Core`: Defines contracts (interfaces, DTOs, CLR definitions). Contains no logic. This is the central "Application Core".
    -   `Hartonomous.Infrastructure`: Implements contracts. A thin, high-performance Data Access Layer (DAL) and wrapper for external services. This is an "Adapter".
    -   `Hartonomous.Workers.*`: Background services that orchestrate complex, long-running tasks. These are "Adapters" that drive the application.
    -   `Hartonomous.Api`: A thin, stateless HTTP access layer. This is another "Adapter".
-   **Dependency Rule:** Dependencies point inwards.
    -   `Api` -> `Core`
    -   `Workers` -> `Core`
    -   `Infrastructure` -> `Core`
    -   `Api` and `Workers` depend on `Infrastructure` via the interfaces defined in `Core`.
    -   `Infrastructure` is the only layer that directly communicates with the database.
