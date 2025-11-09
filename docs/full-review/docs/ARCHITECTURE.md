# docs/ARCHITECTURE.md

## Purpose and Context

- Comprehensive architectural overview describing Hartonomous as a SQL Server–centric AI platform leveraging spatial data, graph traversal, CLR extensions, and Service Broker automation.
- Documents rationale for treating SQL Server as inference runtime and outlines layer responsibilities from storage through autonomous optimization.

## Key Concepts

- Storage layer stores tensors using `GEOMETRY`, FILESTREAM, temporal tables, and graph nodes/edges; introduces trilateration projection to overcome `VECTOR` dimension limits.
- Computation layer relies on SQL CLR 4.8.1 assemblies plus a .NET Standard bridge for modern features (tokenization, JSON, TSNE, Mahalanobis distance) and SIMD optimizations.
- Intelligence layer implements multi-head attention, graph neural network operations, and reasoning engines for hypothesis generation/validation; Autonomous layer details OODA loop realized via Service Broker procedures and queues.
- Provides data flow diagrams for embedding generation, semantic search, and autonomous optimization, emphasizing end-to-end SQL orchestration.
- Highlights security (row-level security, masking, Always Encrypted), observability (Query Store, Extended Events, custom metrics), and deployment practices (HA, DR, scalability).

## Potential Risks / Follow-ups

- Many claims (GPU FILESTREAM mapping, advanced CLR functions) should be validated against actual implementation status; some sections may be aspirational.
- Aggressive reliance on SQL CLR and Service Broker demands rigorous operational monitoring and security review before production rollout.
- Ensure documentation stays synchronized with evolving schema or architectural decisions to avoid misleading contributors.
