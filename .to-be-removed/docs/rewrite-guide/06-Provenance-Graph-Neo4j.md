# 06 - Provenance & Explainability: The Neo4j Graph

While the SQL Server engine provides the power for computation and storage, it is not optimized for deep, multi-hop relationship analysis. To solve the "black box" problem and provide a complete, verifiable audit trail for every piece of data and every AI inference, the Hartonomous platform integrates a second database: **Neo4j**.

## 1. Dual-Database Strategy: System of Record

The decision to use both SQL Server and Neo4j is a deliberate architectural choice that leverages the strengths of each database by defining a clear **System of Record (SoR)** for different types of data.

-   **SQL Server (The Engine & SoR for Atomic Data):** SQL Server is the master repository for all core atomic data. It is optimized for storing the raw `Content` of atoms and their high-dimensional `VECTOR` embeddings. It is the transactional source of truth for "what an atom is".
-   **Neo4j (The Ledger & SoR for Provenance):** Neo4j is the master repository for the relationships *between* atoms and the processes that create them. It is optimized for graph traversal and pathfinding, making it the source of truth for "how an atom came to be" and "what it's connected to".

## 2. Data Synchronization and Consistency

The two databases are kept in sync using an **asynchronous, event-driven** model, which is orchestrated by the ingestion pipeline.

-   **Mechanism:** As described in the ingestion guide, when new atoms are created, events are published to the **SQL Service Broker**. The `Hartonomous.Workers.Neo4jSync` service acts as a consumer for these events.
-   **Eventual Consistency:** This asynchronous pattern means the system operates on an **eventual consistency** model. There will be a short delay between when an atom is created in SQL Server and when its corresponding node and relationships appear in the Neo4j provenance graph.
-   **Appropriate Trade-off:** This is an acceptable and standard trade-off for this architecture. The provenance graph is an analytical and auditing system, not a real-time transactional one. A slight delay in the visibility of the audit trail does not impact the core functionality of the AI engine.

## 3. The Provenance Graph Data Model

The Neo4j database stores a graph model of the system's complete history. This is not a copy of the data in SQL Server, but a graph of the *metadata and relationships* between data. The model consists of several key node types:

-   **`Atom`**: Represents a specific atom from the SQL database, identified by its `AtomId` (hash). This is the bridge between the two databases.
-   **`Source`**: Represents the origin of data (e.g., a file path, a URL, a user input).
-   **`IngestionJob`**: Represents a specific run of the ingestion service that brought a `Source` into the system.
-   **`User`**: The user or service principal who initiated an action.
-   **`Pipeline`**: A specific version of an AI pipeline or procedure (e.g., `dbo.sp_TransformerStyleInference_v1.2`).
-   **`Inference`**: An instance of a pipeline execution, which consumes input `Atom` nodes and produces output `Atom` nodes.

## 4. How the Graph Provides Explainability

These nodes are connected by descriptive relationships (e.g., `INGESTED_FROM`, `GENERATED_BY`, `HAD_INPUT`), forming a complete, unbroken chain of provenance. This is a Merkle DAG (Directed Acyclic Graph) where the `AtomId` hashes provide cryptographic verifiability.

This structure allows for powerful explainability queries that are difficult or impossible in a relational database:

-   **Root Cause Analysis:** For any given piece of generated data (an `Atom`), you can traverse the graph backwards to find the exact `Inference` that created it, the `Pipeline` version used, the `User` who triggered it, all the input `Atom`s that influenced it, and the original `Source`s of those inputs.
-   **Impact Analysis:** For any given `Source` or input `Atom`, you can traverse the graph forwards to find every single piece of derived data or AI inference that was influenced by it. This is critical for data correction, retraction, and compliance.
-   **Bias Detection:** By analyzing paths in the graph, you can identify if certain `Source`s or `User`s are disproportionately influencing the outcomes of AI `Inference` nodes, helping to uncover potential biases in the training data or the models.
-   **"Why did the AI do that?":** When a user asks why a particular result was generated, the graph can provide a definitive answer by showing the exact inputs and context that led to that specific output.

The Neo4j provenance graph is not an optional component; it is the foundational solution to ensuring the Hartonomous platform is transparent, auditable, and trustworthy.
