# Hartonomous Documentation

Welcome to the comprehensive documentation for **Hartonomous** - a revolutionary database-centric AI platform that enables semantic search, geometric inference, and autonomous optimization through spatial reasoning.

## 📖 Documentation Structure

This documentation is organized into seven main sections to serve different audiences and use cases:

### 🚀 Getting Started

New to Hartonomous? Start here to get up and running quickly.

- **[Quickstart Guide](getting-started/quickstart.md)** - Get Hartonomous running in 5 minutes
- **[Installation](getting-started/installation.md)** - Detailed installation instructions for all components
- **[Configuration](getting-started/configuration.md)** - Configure Azure services, Entra ID, and multi-tenancy
- **[First Ingestion](getting-started/first-ingestion.md)** - Ingest your first model, document, or image

**Estimated Time**: 30 minutes from zero to first query

---

### 🏗️ Architecture

Deep dive into the core architectural innovations that make Hartonomous unique.

- **[Semantic-First Architecture](architecture/semantic-first.md)** - O(log N) + O(K) pattern for queryable AI
- **[OODA Autonomous Loop](architecture/ooda-loop.md)** - Self-healing database optimization
- **[Spatial Geometry](architecture/spatial-geometry.md)** - 1536D → 3D landmark projection
- **[Model Atomization](architecture/model-atomization.md)** - Content-addressable storage and deduplication
- **[Catalog Management](architecture/catalog-management.md)** - Multi-file model coordination
- **[Model Parsers](architecture/model-parsers.md)** - GGUF, SafeTensors, ONNX, PyTorch, TensorFlow
- **[Inference & Generation](architecture/inference.md)** - Spatial KNN for next-token prediction
- **[Training & Fine-Tuning](architecture/training.md)** - Gradient descent on geometry
- **[Archive Handling](architecture/archive-handler.md)** - Secure ZIP/TAR/GZIP extraction

**Audience**: Software architects, technical leads, researchers

---

### 🛠️ Implementation

Practical guides for implementing and extending Hartonomous.

- **[Database Schema](implementation/database-schema.md)** - SQL Server schema, indices, temporal tables
- **[T-SQL Pipelines](implementation/t-sql-pipelines.md)** - Service Broker OODA queues
- **[CLR Functions](implementation/clr-functions.md)** - 49 SIMD-optimized functions
- **[Neo4j Integration](implementation/neo4j-integration.md)** - Provenance graph and Merkle DAG
- **[Worker Services](implementation/worker-services.md)** - Background processing architecture
- **[Testing Strategy](implementation/testing-strategy.md)** - Unit, CLR, integration, E2E testing

**Audience**: Developers, database administrators, DevOps engineers

---

### ⚙️ Operations

Deploy, monitor, and maintain Hartonomous in production.

- **[Deployment](operations/deployment.md)** - Azure Arc, DACPAC, GitHub Actions CI/CD
- **[Monitoring](operations/monitoring.md)** - Application Insights, health checks, alerts
- **[Backup & Recovery](operations/backup-recovery.md)** - Database backup, disaster recovery
- **[Performance Tuning](operations/performance-tuning.md)** - Spatial index optimization, query plans
- **[Troubleshooting](operations/troubleshooting.md)** - Common issues and solutions
- **[Cognitive Kernel Seeding](operations/kernel-seeding.md)** - Bootstrap testing framework

**Audience**: Operations engineers, site reliability engineers, database administrators

---

### 📡 API Reference

Complete reference for all REST API endpoints.

- **[Ingestion Endpoints](api/ingestion.md)** - File, URL, database, model platform ingestion
- **[Query Endpoints](api/query.md)** - Semantic search, spatial KNN, cross-modal queries
- **[Reasoning Endpoints](api/reasoning.md)** - A* pathfinding, Chain-of-Thought, hypothesis generation
- **[Provenance Endpoints](api/provenance.md)** - Atom lineage, Merkle DAG traversal
- **[Streaming Endpoints](api/streaming.md)** - Server-sent events, WebSocket real-time updates

**Audience**: API consumers, frontend developers, integration engineers

---

### 🔬 Atomizers

Specialized documentation for the atomization pipeline.

- **[AI Model Platforms](atomizers/ai-model-platforms.md)** - Ollama, HuggingFace model ingestion
- **[Document Atomizers](atomizers/documents.md)** - PDF, Markdown, text splitting
- **[Image Atomizers](atomizers/images.md)** - OCR, object detection, scene analysis
- **[Video Atomizers](atomizers/videos.md)** - Frame extraction, shot detection
- **[Code Atomizers](atomizers/code.md)** - AST parsing, function extraction

**Audience**: Data engineers, ML engineers, content ingestion developers

---

### 🤝 Contributing

Join the Hartonomous development community.

- **[Contributing Guide](contributing/contributing.md)** - How to contribute, code of conduct
- **[Development Setup](contributing/development-setup.md)** - Local development environment
- **[Code Standards](contributing/code-standards.md)** - C#, T-SQL, PowerShell style guides
- **[Pull Request Process](contributing/pull-requests.md)** - PR templates, review process

**Audience**: Open source contributors, community developers

---

### 📋 Planning (Current Development)

Active development planning and architectural validation.

- **[Architectural Validation](planning/ARCHITECTURAL-VALIDATION-REPORT.md)** - Microsoft pattern validation
- **[Refactoring Plan](planning/ARCHITECTURAL-REFACTORING-PLAN.md)** - SOLID, Clean Architecture
- **[App Layer Production Plan](planning/APP-LAYER-PRODUCTION-PLAN.md)** - Production readiness roadmap

**Audience**: Core development team, technical decision makers

---

## 🎯 Quick Navigation by Role

### I'm a Developer

1. Start: [Quickstart Guide](getting-started/quickstart.md)
2. Understand: [Semantic-First Architecture](architecture/semantic-first.md)
3. Build: [API Reference](api/ingestion.md)
4. Contribute: [Development Setup](contributing/development-setup.md)

### I'm a Database Administrator

1. Install: [Installation Guide](getting-started/installation.md)
2. Deploy: [Deployment Guide](operations/deployment.md)
3. Optimize: [Performance Tuning](operations/performance-tuning.md)
4. Monitor: [Monitoring Guide](operations/monitoring.md)

### I'm a Data Scientist / ML Engineer

1. Concepts: [Model Atomization](architecture/model-atomization.md)
2. Ingest: [First Ingestion](getting-started/first-ingestion.md)
3. Train: [Training & Fine-Tuning](architecture/training.md)
4. Query: [Query Endpoints](api/query.md)

### I'm a DevOps Engineer

1. Deploy: [Deployment Guide](operations/deployment.md)
2. Monitor: [Monitoring Guide](operations/monitoring.md)
3. Backup: [Backup & Recovery](operations/backup-recovery.md)
4. Troubleshoot: [Troubleshooting Guide](operations/troubleshooting.md)

### I'm a Researcher / Architect

1. Innovation: [Semantic-First Architecture](architecture/semantic-first.md)
2. Autonomy: [OODA Loop](architecture/ooda-loop.md)
3. Geometry: [Spatial Geometry](architecture/spatial-geometry.md)
4. Learning: [Training & Fine-Tuning](architecture/training.md)

---

## 🔍 Documentation Search Tips

- **Getting Started**: Look in `getting-started/` for installation and first-run guides
- **How It Works**: Look in `architecture/` for design principles and innovations
- **How to Build**: Look in `implementation/` for concrete development guides
- **How to Deploy**: Look in `operations/` for production deployment and maintenance
- **API Usage**: Look in `api/` for endpoint reference and examples
- **Contributing**: Look in `contributing/` for development workflow and standards

---

## 📚 Additional Resources

### External Links

- **GitHub Repository**: [https://github.com/AHartTN/Hartonomous](https://github.com/AHartTN/Hartonomous)
- **Issue Tracker**: [https://github.com/AHartTN/Hartonomous/issues](https://github.com/AHartTN/Hartonomous/issues)
- **Discussions**: [https://github.com/AHartTN/Hartonomous/discussions](https://github.com/AHartTN/Hartonomous/discussions)

### Technology Documentation

- **SQL Server 2025**: [Microsoft Docs](https://learn.microsoft.com/en-us/sql/sql-server/)
- **Neo4j 5.x**: [Neo4j Documentation](https://neo4j.com/docs/)
- **.NET 10**: [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- **Azure Arc**: [Azure Arc Documentation](https://learn.microsoft.com/en-us/azure/azure-arc/)

### Research Papers

Key concepts in Hartonomous are inspired by:

- **Spatial Reasoning**: ["Efficient Spatial Data Structures for AI"](https://example.com)
- **Content-Addressable Storage**: ["Git Internals"](https://git-scm.com/book/en/v2/Git-Internals-Git-Objects)
- **OODA Loop**: [John Boyd's OODA Loop Theory](https://en.wikipedia.org/wiki/OODA_loop)
- **Geometric Inference**: Original research (paper pending)

---

## 🆘 Getting Help

### Documentation Issues

Found a typo or unclear explanation? Please [open an issue](https://github.com/AHartTN/Hartonomous/issues/new?labels=documentation) with:
- Documentation file path
- Section/heading
- Suggested improvement

### Technical Support

- **Community Forum**: [GitHub Discussions](https://github.com/AHartTN/Hartonomous/discussions)
- **Bug Reports**: [GitHub Issues](https://github.com/AHartTN/Hartonomous/issues)
- **Security Issues**: Email security@hartonomous.ai (private disclosure)

### Commercial Support

Enterprise support packages available. Contact: support@hartonomous.ai

---

## 📝 Documentation Versioning

This documentation corresponds to:
- **Hartonomous Version**: v0.9.0 (Beta)
- **Last Updated**: November 19, 2025
- **Documentation Version**: 1.0.0

For documentation for other versions, see [version archive](https://github.com/AHartTN/Hartonomous/wiki/Documentation-Archive).

---

## 📄 License

Documentation is licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).  
Code examples are licensed under [MIT License](../LICENSE).

---

**Ready to get started?** → [Quickstart Guide](getting-started/quickstart.md)

**Want to understand the innovation?** → [Semantic-First Architecture](architecture/semantic-first.md)

**Need API reference?** → [API Documentation](api/ingestion.md)
