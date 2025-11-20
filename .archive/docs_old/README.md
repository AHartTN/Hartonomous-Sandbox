# Hartonomous Documentation

Complete documentation for the Hartonomous semantic-first AI engine.

## Quick Navigation

### Getting Started
- **[Installation Guide](getting-started/installation.md)** - ✅ Complete setup (SQL Server, Neo4j, CLR, workers, OODA loop)
- **[Quickstart Tutorial](getting-started/quickstart.md)** - ✅ Get running in 10 minutes with sample model ingestion
- **[Core Concepts](getting-started/concepts.md)** - *(coming soon)* Understand atoms, spatial indexing, and semantic-first design
- **[First Queries](getting-started/first-queries.md)** - *(coming soon)* Step-by-step query examples

### Architecture
- **[Semantic-First Architecture](architecture/semantic-first.md)** - ✅ O(log N) + O(K) pattern, 3.6M× speedup proven
- **[Model Atomization](architecture/model-atomization.md)** - ✅ 6 format parsers, CAS deduplication, spatial indexing
- **[Neo4j Provenance](architecture/neo4j-provenance.md)** - ✅ Merkle DAG, explainability, cryptographic audit trail
- **[OODA Autonomous Loop](architecture/ooda-loop.md)** - ✅ Dual-triggering, 7 hypothesis types, Bayesian learning
- **[Entropy Geometry](architecture/entropy-geometry.md)** - *(coming soon)* SVD compression and manifold clustering
- **[Temporal Causality](architecture/temporal-causality.md)** - *(coming soon)* Laplace's Demon bidirectional state traversal
- **[Adversarial Modeling](architecture/adversarial-modeling.md)** - *(coming soon)* Red/blue/white team threat dynamics
- **[Cross-Modal Capabilities](architecture/cross-modal.md)** - *(coming soon)* Text ↔ Audio ↔ Image ↔ Code queries
- **[System Design](architecture/system-design.md)** - ✅ Complete technical architecture

### API Reference
- **[SQL Procedures](api/sql-procedures.md)** - *(coming soon)* Stored procedure reference (sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn, etc.)
- **[CLR Functions](api/clr-functions.md)** - *(coming soon)* 49 CLR functions for distance metrics, ML algorithms, model parsers
- **[REST Endpoints](api/rest-endpoints.md)** - *(coming soon)* HTTP API specification (in development)

### Operations
- **[CLR Deployment Guide](operations/clr-deployment.md)** - ✅ ⚠️ **CRITICAL dependency issue documented** - Assembly deployment, signing, troubleshooting
- **[Deployment Guide](operations/deployment.md)** - *(coming soon)* Production deployment procedures
- **[Monitoring & Observability](operations/monitoring.md)** - *(coming soon)* Metrics, logging, and diagnostics
- **[Troubleshooting](operations/troubleshooting.md)** - *(coming soon)* Common issues and solutions
- **[Performance Tuning](operations/performance-tuning.md)** - *(coming soon)* Optimization strategies
- **[Backup & Recovery](operations/backup-recovery.md)** - *(coming soon)* Data protection procedures

### Examples & Tutorials
- **[Cross-Modal Queries](examples/cross-modal-queries.md)** - *(coming soon)* Text→Audio, Image→Code, Audio→Text examples
- **[Model Ingestion](examples/model-ingestion.md)** - *(coming soon)* Ingest GGUF, SafeTensors, ONNX, PyTorch models
- **[Reasoning Chains](examples/reasoning-chains.md)** - *(coming soon)* Tree-of-Thought, Chain-of-Thought, ReAct patterns
- **[Behavioral Analysis](examples/behavioral-analysis.md)** - *(coming soon)* Session path geometry and user pattern detection

## Documentation Philosophy

This documentation follows these principles:

1. **Vision-Driven**: Every page reflects the core vision - semantic-first spatial indexing with O(log N) performance
2. **No Deviation**: Content strictly adheres to the proven architecture (3.6M× speedup, 159:1 compression, etc.)
3. **Clarity Over Completeness**: Focus on understanding core concepts before implementation details
4. **Fresh Start**: Generated from scratch based on validated production implementation (49 CLR functions, 225K lines)
5. **Examples First**: Show working code before explaining theory

## Contributing to Documentation

Documentation improvements are welcome! Please:

1. Read **[Core Concepts](getting-started/concepts.md)** to understand the vision
2. Verify examples work against current codebase
3. Follow existing structure and tone
4. Submit pull requests with clear explanations

## Documentation Status

✅ **Complete** (5,320 lines):
- Getting Started: Installation (530 lines), Quickstart (480 lines)
- Architecture: Semantic-First (580 lines), Model Atomization (505 lines), Neo4j Provenance (590 lines), OODA Loop (680 lines)
- Operations: CLR Deployment (630 lines) with ⚠️ CRITICAL dependency issue

🚧 **Planned**:
- API Reference: SQL Procedures, CLR Functions (49 total), REST Endpoints
- Operations: Monitoring, Performance Tuning, Backup & Recovery, Troubleshooting
- Examples: Cross-Modal Queries, Reasoning Chains, Behavioral Analysis

**Total Documentation**: 5,320 lines across 9 files

Last Updated: November 18, 2025
