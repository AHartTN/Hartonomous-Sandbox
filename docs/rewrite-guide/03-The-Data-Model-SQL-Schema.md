# 03 - The Data Model & SQL Schema

The `Hartonomous.Database` project is the heart of the AI engine. Its schema is not a traditional relational model but a highly specialized architecture designed to implement the principles of Atomization, Spatio-Semantic Representation, and Verifiable Provenance. This document provides an overview of its key components.

## 1. Core Philosophy

The schema is designed around a "separation of concerns" model for data:

-   **Unique Data (`Atoms`)**: Stores the unique, content-addressed raw data. This is the "what."
-   **Contextual Data (`AtomRelations`, `AtomSources`)**: Describes the relationships between atoms and their origins. This is the "where" and "how."
-   **Derived Data (`AtomEmbeddings`, `TensorAtoms`)**: Stores the results of AI processes, such as embeddings or inference results, linked to the source atoms. This is the "meaning."
-   **Provenance Data (`provenance.*`)**: A dedicated schema for immutable, append-only audit trails. This is the "who" and "when."

## 2. Key Tables and Their Purpose

Below are the most critical tables in the main `dbo` schema.

### `dbo.Atoms`
This is the master table for all unique, deduplicated data in the system.

-   `AtomId` (BIGINT): An internal, unique identifier for the atom.
-   `AtomHash` (VARBINARY(32)): The SHA-256 hash of the `Content`. This is the public, content-addressable key used for lookups. It is the true identifier of the atom.
-   `Content` (VARBINARY(MAX)): The raw, immutable byte data of the atom.
-   `ContentTypeId` (INT): A foreign key to a `ContentType` table, classifying the atom's data (e.g., `text/utf-8`, `image/png`, `float32`).

### `dbo.AtomRelations`
This table constructs the relationships between atoms, forming complex data structures from simple parts. It is the foundation of the system's ability to represent context.

-   `ParentAtomId` (BIGINT): The atom that is the "container" or "subject."
-   `ChildAtomId` (BIGINT): The atom that is "contained within" or the "object."
-   `RelationTypeId` (INT): A foreign key describing the nature of the relationship (e.g., "is-a-child-of", "is-a-key-for", "is-a-member-of").
-   `Ordinal` (INT): Defines the order of child atoms within a parent (e.g., the sequence of words in a sentence).

### `dbo.AtomEmbeddings`
This table stores the semantic representation of an atom.

-   `AtomId` (BIGINT): The atom that has been embedded.
-   `EmbeddingTypeId` (INT): The embedding model used (e.g., "Text-Embedding-Ada-002", "Internal-Model-v3").
-   `EmbeddingVector` (`VECTOR`): The high-dimensional vector, stored natively using the `VECTOR` data type (SQL Server 2025+). This is the primary data used for semantic search.
-   `SpatialGeometry` (`GEOMETRY`): An optional, lower-dimensional (2D or 3D) projection of the `EmbeddingVector`, used for visualization and specialized geometric queries.

### `dbo.TensorAtoms`
A specialized table for storing and manipulating tensor data, critical for ML model weights and other numerical structures.

-   `TensorAtomId` (BIGINT): The identifier for this specific tensor atom.
-   `OwningAtomId` (BIGINT): The atom that this tensor is a component of (e.g., a specific layer in a neural network model file).
-   `Dimensions` (VARCHAR(100)): The shape of the tensor (e.g., "1536", "768,1024").
-   `Value` (FLOAT): The numerical value at a specific coordinate within the tensor.

## 3. Specialized SQL Server Technologies

The schema makes extensive use of advanced SQL Server features to achieve its performance goals. The selection of these technologies is critical for the performance of the `O(log N)` database lookups.

-   **Vector Indexes:** Created on the `EmbeddingVector` column in `dbo.AtomEmbeddings`. These indexes are the core mechanism for performing rapid Approximate Nearest Neighbor (ANN) searches on high-dimensional vector data.
    -   **Best Practice:** SQL Server 2025 uses the state-of-the-art **DiskANN algorithm** from Microsoft Research, which is optimized for large datasets on disk. This is the primary method for the semantic search part of AI inference.

-   **Columnstore Indexes:** Applied to large analytical and historical tables, particularly those involved in tracking atom relationships and provenance metadata.
    -   **Best Practice:** A **Clustered Columnstore Index** should be used for very large, data-warehouse-style tables that are read-intensive. A **Non-Clustered Columnstore Index** is ideal for enabling real-time analytics directly on transactional tables without impacting performance.

-   **Temporal Tables:** Used extensively, especially in the `provenance` schema, to provide a complete, immutable history of all changes to critical data.
    -   **Best Practice:** This is the optimal solution for **data auditing, point-in-time analysis** (e.g., "what did this record look like last Tuesday?"), and meeting compliance requirements. The history table should be indexed on its `SysStartTime` and `SysEndTime` columns.

-   **Hekaton (In-Memory OLTP):** Certain "hot" tables that require extreme low-latency and high throughput are configured as memory-optimized tables.
    -   **Best Practice:** This is ideal for **caching, session state, or high-volume ingestion scenarios** (like receiving data from a real-time event stream). For maximum performance, logic that operates on these tables should be encapsulated in **natively compiled stored procedures**.

This schema is the bedrock of the Hartonomous engine. The next document will cover the **Orchestration Layer**, explaining how T-SQL Stored Procedures operate on this data model to execute complex AI pipelines.
