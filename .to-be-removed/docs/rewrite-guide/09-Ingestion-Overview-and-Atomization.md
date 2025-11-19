# 09 - Ingestion Overview and Atomization

Getting data into the Hartonomous platform is the first step in any workflow. The ingestion pipeline is designed to be reliable, scalable, and extensible to support a wide variety of data sources. The architecture follows a robust, three-phase pattern based on Change Data Capture (CDC) and a message broker.

## The Three-Phase Ingestion Architecture

The entire ingestion process is decoupled to ensure stability and scalability. The three phases are:

1.  **Capture:** A dedicated service polls a data source and captures any changes.
2.  **Decouple:** The captured changes are published as standardized events to a central message bus.
3.  **Process:** Downstream services consume these events, perform the actual atomization, and insert the data into the core database.

This separation means that a failure or slowdown in the processing phase will not impact the capture phase. The message broker acts as a buffer, ensuring no data is lost.

---

### Phase 1: Capture (The `CesConsumer` Worker)

The primary ingestion entry point is the `Hartonomous.Workers.CesConsumer` service.

-   **Mechanism:** This service uses a **Change Data Capture (CDC)** pattern. It continuously polls a source database's transaction logs by calling an `ICdcRepository`.
-   **Checkpointing:** To ensure it never misses data, it keeps track of the last Log Sequence Number (LSN) it has processed. On startup, it queries the database for all changes that have occurred since the last checkpoint.
-   **Responsibility:** The *only* responsibility of the `CesConsumer` is to capture the raw change events, map them to a standard format, and hand them off to the next phase. It does no heavy processing itself.

### Phase 2: Decouple (The SQL Service Broker)

Once the `CesConsumer` has a batch of change events, it does not process them. Instead, it publishes them to the **in-process SQL Service Broker**.

-   **Mechanism:** SQL Service Broker is a native messaging and queuing technology built directly into SQL Server. It allows for asynchronous, reliable message delivery between different components within the same SQL Server instance or across instances.
-   **Purpose:** SQL Service Broker acts as a durable, transactional message queue. It decouples the data capture process from the data processors.
-   **Benefits:**
    -   **Transactional Integration:** Messages can be published within the same database transaction as other data operations, ensuring atomicity.
    -   **Reliability:** Messages are durably stored within the database, guaranteeing delivery even if consumers are offline.
    -   **Scalability:** We can add more processing workers to consume messages in parallel, allowing the system to handle massive bursts of incoming data.
    -   **Simplicity:** No external message broker infrastructure is required, simplifying deployment and management.
    -   **Flexibility:** We can add new types of consumers that listen to the same event stream for different purposes (e.g., real-time analytics, logging) without changing the original pipeline.

### Phase 3: Process (The Atomizer Workers)

This phase is handled by one or more downstream worker services (to be detailed in a subsequent document) that subscribe to the message broker.

-   **Consumption:** These workers listen for new messages on the bus.
-   **Atomization:** When a message is received, the worker initiates the atomization process. It uses the `AtomIngestionPipeline` and its various `Atomizers` (e.g., `TextAtomizer`, `ImageAtomizer`) from the `Hartonomous.Core` project to deconstruct the content into its fundamental atoms.
-   **Dual-Database Insertion:** The worker then performs a dual insertion:
    1.  It calls the appropriate stored procedures in `Hartonomous.Database` (e.g., `dbo.sp_AtomizeText_Governed`) to insert the new atoms, embeddings, and relations into the SQL Server database.
    2.  Crucially, it also interacts with **Neo4j** (often via the `Hartonomous.Workers.Neo4jSync` service or directly) to build the **provenance graph**. This involves creating nodes and relationships in Neo4j that link the newly created atoms to their original sources, the ingestion job, the user, and the specific algorithms used to create them. This ensures a complete, auditable, and explainable history.
-   **Idempotency:** A critical best practice for these workers is to be **idempotent**. Because message brokers can sometimes deliver a message more than once, the worker must be designed so that processing the same event multiple times does not create duplicate data or cause errors. This is naturally handled by the content-addressable nature of Atoms; attempting to insert the same atom twice will simply result in a reference to the existing one.

This robust, decoupled architecture ensures that data can be ingested into the Hartonomous platform reliably and at scale. The next document will dive deeper into the specific atomization pipelines and the workers that execute them.
