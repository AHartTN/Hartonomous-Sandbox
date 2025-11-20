# Documentation Review Log

This document tracks the comprehensive review of all markdown files in the `.to-be-removed` and `docs_old` directories. Its purpose is to ensure that no valuable information is lost during the documentation refactoring process and to provide a transparent, auditable record of the analysis.

Each entry in this log will follow this format:

---

**File:** `[Full Path to File]`

*   **Summary:** A brief, 1-2 sentence description of the file's purpose and content.
*   **Key Concepts:** A list of the main technical ideas, features, or components discussed.
*   **Relationship to Other Docs:** An analysis of how this file relates to others.
    *   **Duplicate of:** A list of files with identical or near-identical content.
    *   **Superseded by:** A file that contains a more modern or complete version of this content.
    *   **Complements:** A file that provides related but distinct information (e.g., a "how-to" guide for a "what-is" document).
    *   **Unique Content:** A description of any valuable information found only in this file.
*   **Google Search Validation:** A record of Google searches performed on novel or key technical terms to ground the concepts in real-world computer science.
*   **Proposed Action:** The recommended next step for this file's content.
    *   **Merge into `[target file]`:** The unique, valuable content should be moved into a specific new document.
    *   **Keep As Standalone in `[target directory]`:** The file is valuable and unique enough to become its own document in the new structure.
    *   **Mark for Deletion:** The file is redundant, outdated, or its content has been fully superseded.
    *   **Reference Only:** The file is a log or status update that should be archived for historical context but not moved to the new `docs`.

---

**File:** `.to-be-removed/api/README.md`

*   **Summary:** This file serves as a high-level API reference, outlining the two main ways to interact with the Hartonomous engine: directly via T-SQL stored procedures and through a convenience REST API wrapper.
*   **Key Concepts:** Dual API Layers (T-SQL and REST); T-SQL as the primary, high-performance interface; REST as a secondary convenience layer; lists of stored procedures for different tasks (Inference, Reasoning, Agents, OODA); basic REST endpoint definitions; standard patterns for Authentication, Rate Limiting, and Error Handling.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** Likely a direct or near-direct duplicate of `docs_old/api/README.md`. This will be confirmed during the `docs_old` review phase.
    *   **Superseded by:** The document itself states that more complete T-SQL and REST references are "coming soon." In its current state, it is not superseded.
    *   **Complements:** Complements the architectural documents by providing the user-facing interaction points for the system.
    *   **Unique Content:** Provides the most explicit list of stored procedure names and high-level REST endpoints found so far.
*   **Google Search Validation:** The core concepts (T-SQL API vs. REST API, JWT, rate limiting) are standard software engineering patterns and do not require validation. Project-specific procedure names are internal.
*   **Proposed Action:** **Keep As Standalone in `docs/api/`**. The content is valuable as a starting point for the new API documentation. A new `docs/api/` directory will need to be created for it.

---

**File:** `.to-be-removed/architecture/ADVERSARIAL-MODELING-ARCHITECTURE.md`

*   **Summary:** This document details a sophisticated, three-part threat modeling and autonomous defense system (Red/Blue/White Team). It describes using the geometric properties of the semantic space to launch attacks, detect them, and autonomously improve system resilience.
*   **Key Concepts:** Red Team (semantic_key_mining.sql attack vector via DBSCAN clustering), Blue Team (real-time anomaly detection using Local Outlier Factor and Isolation Forest on manifolds), White Team (extending the OODA loop for automated security responses), and defensive strategies (Manifold Obfuscation, Honeypot Atoms).
*   **Relationship to Other Docs:**
    *   **Duplicate of:** None. The content is highly specific and unique.
    *   **Superseded by:** Unlikely. This is a deep dive into a specific, advanced capability.
    *   **Complements:** `OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md` by providing a specific security application; `ENTROPY-GEOMETRY-ARCHITECTURE.md` by applying its manifold concepts; and the CLR algorithm files (`LocalOutlierFactor.cs`, `DBSCANClustering.cs`) which are direct implementations.
    *   **Unique Content:** The entire `semantic_key_mining.sql` attack concept appears to be novel. The specific application of LOF and Isolation Forests to the manifold space for security is also unique.
*   **Google Search Validation:** Searches confirm that LOF, Isolation Forest, and DBSCAN are standard anomaly detection and clustering algorithms. Using them on embeddings is common. However, the concept of clustering the *semantics of cryptographic operations* to find key generation patterns appears to be a novel project-specific innovation.
*   **Proposed Action:** **Keep As Standalone in `docs/architecture/`** and **create a reference in a new `docs/security/` section**. This document is a critical and unique piece of the architecture that showcases a novel capability. It's too detailed for a general overview but is essential for understanding the system's security posture.

---

**File:** `.to-be-removed/architecture/ARCHIVE-HANDLER.md`

*   **Summary:** This document provides a detailed design for a secure, robust, and complete system for handling archive files (like ZIP, GZIP, TAR) directly within SQL Server using the CLR.
*   **Key Concepts:** Security-first design (path traversal, zip bomb protection); complete implementation using `System.IO.Compression.ZipArchive`; recursive extraction of nested archives; SQL CLR integration as table-valued functions; `EXTERNAL_ACCESS` permission set configuration.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** The content is a more verbose version of Section 3, "Archive Handler Infrastructure," within `UNIVERSAL-FILE-SYSTEM-DESIGN.md`.
    *   **Superseded by:** `UNIVERSAL-FILE-SYSTEM-DESIGN.md`. The version in the universal design document is more refined, concise, and better integrated into the overall ingestion pipeline architecture.
    *   **Complements:** N/A.
    *   **Unique Content:** Contains slightly more detailed code examples and a "Future Enhancements" section not present in the superseding document.
*   **Google Search Validation:** Searches confirm that using `System.IO.Compression.ZipArchive` in SQL CLR requires `EXTERNAL_ACCESS`, and the security measures described (path traversal, zip bomb protection) are standard best practices.
*   **Proposed Action:** **Mark for Deletion.** The core concepts are better and more concisely captured in `UNIVERSAL-FILE-SYSTEM-DESIGN.md`. The minor unique details do not warrant keeping a separate, largely redundant document.

---

**File:** `.to-be-removed/architecture/CATALOG-MANAGER.md`

*   **Summary:** This document designs a "Catalog Manager" responsible for understanding and coordinating AI models that are composed of multiple files, such as those from HuggingFace, Ollama, or Stable Diffusion pipelines.
*   **Key Concepts:** Handling of multi-file models; specific catalog handlers for HuggingFace, Ollama, Stable Diffusion; parsing of configuration files (`config.json`, `Modelfile`); model integrity validation (checking for missing files); SQL CLR integration.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** The content is very similar to Section 4, "Catalog Manager (Multi-File Coordination)," within `UNIVERSAL-FILE-SYSTEM-DESIGN.md`.
    *   **Superseded by:** `UNIVERSAL-FILE-SYSTEM-DESIGN.md`. The universal design document contains a more refined and better-integrated version of this concept, fitting it into the larger `UniversalFileFormatRegistry` architecture.
    *   **Complements:** N/A.
    *   **Unique Content:** Provides slightly more detailed C# interface definitions, but this detail is implicitly included or improved upon in the code within the more comprehensive universal design document.
*   **Google Search Validation:** Searches on "huggingface model file structure," "ollama modelfile format," and "stable diffusion file structure" confirm that the document accurately describes the real-world structures it intends to parse.
*   **Proposed Action:** **Mark for Deletion.** This document's content is conceptually redundant and better handled by `UNIVERSAL-FILE-SYSTEM-DESIGN.md`, which should be the single source of truth for this component.

---

**File:** `.to-be-removed/architecture/COGNITIVE-KERNEL-SEEDING.md`

*   **Summary:** This document describes an essential testing and validation system for the Hartonomous engine. It outlines a "seeding" process that creates a deterministic, testable "universe" with known physics, matter, and history to validate core functionalities.
*   **Key Concepts:** "Four Epochs of Creation" (Axioms, Primordial Soup, Mapping Space, Waking the Mind); defining `SpatialLandmarks` as an orthogonal basis for the semantic universe; creating "Golden Paths" with known 3D coordinates for testing A* pathfinding; bootstrapping the OODA loop with a known history of normal and anomalous events; a concrete SQL-based validation suite.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** None. This document is highly unique.
    *   **Superseded by:** Unlikely. This describes a fundamental testing methodology.
    *   **Complements:** It is the testing and validation counterpart to `SEMANTIC-FIRST-ARCHITECTURE.md` (testing A*), `OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md` (testing OODA), and `ENTROPY-GEOMETRY-ARCHITECTURE.md` (practical application of landmarks).
    *   **Unique Content:** The entire methodology of the "Four Epochs," "Golden Paths," and the specific SQL validation scripts are unique to this document and critical for understanding how the system is tested and verified.
*   **Google Search Validation:** The concepts of "database seeding" for tests and "trilateration" from a known basis are standard engineering and mathematical practices, validating the soundness of the approach. The specific application as a "Cognitive Kernel Seeding" process is a unique project concept.
*   **Proposed Action:** **Keep As Standalone in `docs/implementation/` or `docs/testing/`**. This document is a critical developer/tester guide. It explains *how* to implement and validate the core architecture. It should be a prominent part of the new documentation.

---

**File:** `.to-be-removed/architecture/COMPLETE-MODEL-PARSERS.md`

*   **Summary:** This document specifies creating "complete" parsers for PyTorch, ONNX, and TensorFlow models, correcting previous incomplete versions. It emphasizes using proper libraries (`protobuf-net`, `System.IO.Compression`) and fully supporting the format specifications within the SQL CLR environment.
*   **Key Concepts:** "No Cop-Outs" principle (no `NotSupportedException`); using libraries instead of manual parsing; detailed C# implementations for `PyTorchParser`, `ONNXParser`, and `TensorFlowParser`; a central `ModelParserRegistry` for format detection.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** This is a more detailed version of concepts found in `UNIVERSAL-FILE-SYSTEM-DESIGN.md`, specifically Section 2 ("Format Parser Layer") and Section 5 ("Universal File Format Registry").
    *   **Superseded by:** `UNIVERSAL-FILE-SYSTEM-DESIGN.md`. The universal design document contains the more mature and better-integrated architectural vision, placing these parsers within a larger ingestion framework that includes providers and catalog managers.
    *   **Complements:** N/A.
    *   **Unique Content:** The C# code examples are more verbose, and it includes valuable generated C# classes from the ONNX protobuf schema.
*   **Google Search Validation:** Searches on "parse onnx with protobuf-net", "pytorch .pt file format zip", and "tensorflow savedmodel file structure" confirm the technical approaches described are correct and align with industry-standard practices.
*   **Proposed Action:** **Mark for Deletion.** While the code examples are detailed, the overall architectural concept is completely subsumed by `UNIVERSAL-FILE-SYSTEM-DESIGN.md`. To avoid redundancy and confusion, the universal design document must be the single source of truth for the parsing layer's design.

---

**File:** `.to-be-removed/architecture/END-TO-END-FLOWS.md`

*   **Summary:** This document provides a series of high-level, end-to-end workflows described in a pseudo-T-SQL format, showing how different system components integrate to accomplish complex tasks.
*   **Key Concepts:** Orchestration examples for HuggingFace ingestion, real-time video processing, hybrid search (Geometric + Vector), and monetization models (Pay-to-Contribute, Pay-to-Hide).
*   **Relationship to Other Docs:**
    *   **Duplicate of:** None, but it is highly derivative, acting as a collection of usage examples for other architecture documents.
    *   **Superseded by:** No single document, but its content is better suited as examples within the specific documents it references.
    *   **Complements:** Complements nearly every other architecture document by providing a practical usage example.
    *   **Unique Content:** The hybrid search and monetization flow concepts are most clearly articulated here.
*   **Google Search Validation:** The document describes internal orchestration. The underlying technologies (Service Broker, RLS, Hybrid Search) are standard, so no external validation is needed.
*   **Proposed Action:** **Merge into other documents.** This file should not exist independently. Its content should be broken up and moved into the "Examples" section of the relevant, more detailed documents (e.g., ingestion flows go with the ingestion document, security flows go with the security document).

---

**File:** `.to-be-removed/architecture/ENTROPY-GEOMETRY-ARCHITECTURE.md`

*   **Summary:** This document details the mathematical and architectural foundation for how the Hartonomous engine handles high-dimensional embeddings. It proposes a novel combination of Singular Value Decomposition (SVD) for dimensionality reduction and manifold clustering (DBSCAN) to identify what it calls "Strange Attractors" in the compressed semantic space.
*   **Key Concepts:** Entropy Reduction via SVD; "Strange Attractors" as dense semantic clusters; Manifold Clustering with DBSCAN; applications in model compression, anomaly detection, and cryptographic attacks.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** Significant conceptual overlap with `ADVERSARIAL-MODELING-ARCHITECTURE.md` and `MODEL-COMPRESSION-AND-OPTIMIZATION.md`.
    *   **Superseded by:** The specific application examples are better detailed in their respective, more focused documents.
    *   **Complements:** It is the direct theoretical foundation for `ADVERSARIAL-MODELING-ARCHITECTURE.md` and `MODEL-COMPRESSION-AND-OPTIMIZATION.md`. It also provides the "why" behind the landmark projection in `SEMANTIC-FIRST-ARCHITECTURE.md`.
    *   **Unique Content:** The mathematical explanation of SVD and entropy reduction is most clearly articulated here. The term "Strange Attractors" is also central to this document.
*   **Google Search Validation:** Searches confirm SVD for dimensionality reduction and DBSCAN on reduced data are standard techniques. The term "Strange Attractor" is a creative, project-specific application of a concept from chaos theory.
*   **Proposed Action:** **Merge and Delete.** The unique theoretical parts (SVD, entropy, Strange Attractors) should be merged into the core `architecture/semantic-first.md` document to provide mathematical justification. The duplicated application examples should be discarded in favor of their more detailed versions elsewhere. This standalone document should then be deleted to avoid redundancy.

---

**File:** `.to-be-removed/architecture/INFERENCE-AND-GENERATION.md`

*   **Summary:** This document provides a highly detailed explanation of the core inference and text generation process. It describes a two-stage "Semantic-First" pattern that uses spatial indexing for a fast pre-filter, followed by attention ranking on the smaller candidate set.
*   **Key Concepts:** Geometric Inference via spatial queries; Two-Stage Query Pattern (O(log N) pre-filter + O(K) ranking); Autoregressive Decoding with context centroid updates; Cross-Modal Generation; A* Semantic Navigation.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** Significant conceptual overlap with `SEMANTIC-FIRST-ARCHITECTURE.md` and `NOVEL-CAPABILITIES-ARCHITECTURE.md`.
    *   **Superseded by:** Its individual concepts are better and more formally defined in other, more focused documents.
    *   **Complements:** Provides the most detailed, code-level explanation of how all the other architectural components come together to perform generation.
    *   **Unique Content:** The specific T-SQL and C# pseudo-code for `sp_SpatialNextToken` and the autoregressive loop are most clearly articulated here. The troubleshooting and best practices sections are also unique.
*   **Google Search Validation:** Searches on "R-Tree for nearest neighbor search" and "autoregressive decoding" confirm the technical soundness of the approach.
*   **Proposed Action:** **Merge and Delete.** The core concepts should be merged into `architecture/semantic-first.md` and `architecture/novel-capabilities.md`. The detailed examples and troubleshooting guides should be moved into the relevant implementation or operations guides. This standalone file should then be deleted to avoid redundancy.

---

**File:** `.to-be-removed/architecture/MODEL-ATOMIZATION-AND-INGESTION.md`

*   **Summary:** This document details the crucial process of "model atomization" and ingestion, transforming monolithic AI models into granular, deduplicated, and spatially-indexed "atoms" within SQL Server, enabling geometric reasoning and compression.
*   **Key Concepts:** Three-stage Atomization Pipeline (Parse, Atomize, Spatialize); Content-Addressable Storage (CAS) for deduplication; Spatial Projection & Indexing (1536D to 3D, Hilbert curves); SVD Compression of layers; Governed Ingestion (`sp_AtomizeModel_Governed`); Unified Type System; Temporal Causality (`SYSTEM_VERSIONING`).
*   **Relationship to Other Docs:**
    *   **Duplicate of:** None in its comprehensive scope, but builds upon concepts found in `COMPLETE-MODEL-PARSERS.md`, `ENTROPY-GEOMETRY-ARCHITECTURE.md`, and `SEMANTIC-FIRST-ARCHITECTURE.md`.
    *   **Superseded by:** Not superseded. It serves as the central orchestration document for combining many other architectural pieces into a coherent ingestion workflow.
    *   **Complements:** Heavily relies on `UNIVERSAL-FILE-SYSTEM-DESIGN.md` (for parsing), `ENTROPY-GEOMETRY-ARCHITECTURE.md` (for SVD), `SEMANTIC-FIRST-ARCHITECTURE.md` (for spatial projection), and provides the "matter" for `COGNITIVE-KERNEL-SEEDING.md`.
    *   **Unique Content:** The detailed end-to-end workflow for atomization, including the `sp_AtomizeModel_Governed` procedure, CAS implementation details, `IngestionJobs` schema, and the full "Three-Stage Pipeline" is unique and central to this document. The emphasis on "governed ingestion" and the troubleshooting/best practices are also highly valuable.
*   **Google Search Validation:** Confirms CAS for deduplication, Hilbert curves for spatial indexing, and `SYSTEM_VERSIONING` for temporal tables are standard and sound techniques. The application of these in a "model atomization" context is project-specific innovation.
*   **Proposed Action:** **Keep As Standalone in `docs/implementation/`**. This document describes a core, complex, and unique process within Hartonomous, serving as a comprehensive guide for how models become part of the semantic universe.

---

**File:** `.to-be-removed/architecture/MODEL-COMPRESSION-AND-OPTIMIZATION.md`

*   **Summary:** This document details the innovative approach Hartonomous takes to model compression and optimization. It re-frames traditional compression techniques (pruning, quantization, distillation, SVD) as "spatial operations" that manipulate atoms within the cognitive geometry rather than purely algebraic operations on high-dimensional weight matrices.
*   **Key Concepts:** Compression as "spatial operations"; Pruning as `DELETE` statements on `TensorAtoms`; Quantization of 3D `GEOMETRY` coordinates; SVD compression of layers; Student Model Distillation; leveraging SQL Server `COLUMNSTORE` for optimization.
*   **Relationship to Other Docs:**
    *   **Duplicate of:** Significant conceptual overlap with `ENTROPY-GEOMETRY-ARCHITECTURE.md` (for SVD) and `INFERENCE-AND-GENERATION.md` (for quantization).
    *   **Superseded by:** Not superseded. It serves as the central, comprehensive guide for all compression strategies.
    *   **Complements:** Relies heavily on the `ImportanceScore` from the OODA loop (`OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md`), the `TensorAtoms` from the ingestion process (`MODEL-ATOMIZATION-AND-INGESTION.md`), and provides the "how-to" for the compression mentioned in `ENTROPY-GEOMETRY-ARCHITECTURE.md`.
    *   **Unique Content:** The philosophical re-framing of compression as "spatial operations" is unique. The detailed T-SQL and C# pseudo-code examples for each compression stage and the troubleshooting/best practices are also unique and highly valuable.
*   **Google Search Validation:** Searches confirm the underlying techniques (pruning, quantization, distillation, SVD, columnstore indexes) are standard. The application of these as "spatial operations" within the "cognitive geometry" is the project's innovation.
*   **Proposed Action:** **Keep As Standalone in `docs/implementation/`**. This document is a critical piece of the project, detailing how Hartonomous achieves extreme efficiency and performance. Its unique philosophical approach and detailed implementation guidance make it essential.

---
