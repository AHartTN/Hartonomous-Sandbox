# Hartonomous System Design

## System Overview

Hartonomous is an autonomous geometric reasoning system built on five core layers. It is designed to be a highly-performant, scalable, and self-optimizing intelligence engine with a strong emphasis on provenance and determinism.

```
┌─────────────────────────────────────────────────────────┐
│                   User Applications                     │
│             (Web, Mobile, Desktop, APIs)                │
└───────────────────────┬─────────────────────────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Application Layer       │ ← .NET 10 Workers + APIs
          │   (Thin orchestration)    │
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Reasoning Layer         │ ← CoT, ToT, Reflexion
          │   (Stored procedures)     │    Agent Tools
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Database Layer          │ ← SQL Server 2019+
          │   (Spatial R-Tree)        │    CLR (.NET Fmwk 4.8.1)
          └─────────────┬─────────────┘
                        │
          ┌─────────────▼─────────────┐
          │   Provenance Layer        │ ← Neo4j Merkle DAG
          │   (Graph tracking)        │
          └───────────────────────────┘
```

The system's architecture is based on the principle of "semantic-first" design, where all data, regardless of its original modality, is represented in a common geometric space. This allows for powerful cross-modal reasoning and analysis. It's important to note the deliberate choice to leverage both the latest .NET 10 for application and worker services, while retaining .NET Framework 4.8.1 for SQL CLR components due to SQL Server's specific requirements. This dual-framework approach is a cornerstone of our high-performance, in-database intelligence strategy.

> For a deep dive into the "semantic-first" approach, see **[Semantic-First Architecture](./semantic-first.md)**.

---

## Layer 1: Database Layer (SQL Server)

The database layer is the heart of the Hartonomous system. It is responsible for storing all data as "atoms" in a content-addressable format and providing the core O(log N) + O(K) query pattern for efficient retrieval. It leverages SQL Server's spatial capabilities and .NET CLR integration for high-performance geometric operations. This layer's reliance on SQL Server's CLR functionality necessitates the continued use of .NET Framework 4.8.1, even as other parts of the system migrate to .NET 10.

> For more details on the database layer, including its unique capabilities, see **[Database Architecture (coming soon)]()**.

---

## Layer 2: Reasoning Layer

The reasoning layer provides a suite of autonomous reasoning frameworks implemented as stored procedures. These frameworks, including Chain of Thought, Tree of Thought, and Reflexion, enable the system to perform complex reasoning tasks directly within the database, benefiting from the close proximity to data and computational power of SQL Server.

> For more details on the reasoning frameworks, see **[Reasoning Frameworks (coming soon)]()**.

---

## Layer 3: Application Layer (.NET 10)

The application layer consists of a set of thin, stateless worker services and minimal APIs, built on .NET 10. This layer is responsible for orchestrating the ingestion of data and exposing the system's reasoning capabilities to user applications. It is designed to be highly scalable and resilient, providing a modern interface to the powerful database core.

> For more details on the application layer, see **[Application Layer Architecture (coming soon)]()**.

---

## Layer 4: OODA Loop (Autonomous Self-Improvement)

The OODA (Observe, Orient, Decide, Act) loop is a key feature of the Hartonomous system. It provides a mechanism for continuous self-optimization and self-improvement, allowing the system to learn from its own performance and adapt its behavior over time, largely orchestrated through SQL Server's Service Broker.

> For a deep dive into the OODA loop, see **[OODA Loop Architecture](./ooda-loop.md)**.

---

## Layer 5: Provenance Layer (Neo4j)

The provenance layer provides a complete, cryptographic audit trail for all data and reasoning processes in the system. It uses a Neo4j graph database to store a Merkle DAG of all operations, ensuring that the system's behavior is fully transparent and reproducible, offering tamper-evident audit trails and deep historical context.

> For more details on the provenance layer, see **[Neo4j Provenance Architecture](./neo4j-provenance.md)**.
