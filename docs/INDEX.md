# Hartonomous Documentation Index

**Complete Navigation Guide for Engineers, Architects, and Decision Makers**

This index provides structured pathways through Hartonomous documentation based on your role and needs.

---

## 🎯 Quick Navigation by Role

### Executive / Decision Maker
**Time commitment: 15 minutes**

1. [System Overview](OVERVIEW.md) - High-level value proposition and capabilities
2. [Performance Metrics](technical-reference/performance.md) - Concrete benchmarks
3. [Use Cases](capabilities/use-cases.md) - Real-world applications

### Solution Architect
**Time commitment: 1-2 hours**

1. [System Overview](OVERVIEW.md) - Start here for context
2. [Core Concepts](architecture/core-concepts.md) - Fundamental design principles
3. [Architecture Layers](architecture/README.md) - Detailed technical architecture
4. [Emergent Capabilities](capabilities/README.md) - Unique platform features
5. [Integration Patterns](technical-reference/integration.md) - How to integrate

### Software Engineer / Developer
**Time commitment: 2-4 hours**

1. [Quick Start Guide](guides/quick-start.md) - Get running immediately
2. [Developer Guide](guides/development.md) - Setup and workflows
3. [API Reference](technical-reference/api-reference.md) - Stored procedures and functions
4. [CLR Functions](technical-reference/clr-functions.md) - Custom aggregates and operations
5. [Testing Guide](guides/testing.md) - How to test your code
6. [Troubleshooting](guides/troubleshooting.md) - Common issues

### Data Scientist / ML Engineer
**Time commitment: 2-3 hours**

1. [System Overview](OVERVIEW.md) - Understand the platform
2. [Vector Operations](technical-reference/vector-operations.md) - Embedding and similarity
3. [ML Aggregates](technical-reference/ml-aggregates.md) - Neural, reasoning, and analytical functions
4. [Model Management](guides/model-management.md) - Ingesting and optimizing models
5. [Inference Patterns](capabilities/inference-patterns.md) - Generation, search, and analysis

### DevOps / Platform Engineer
**Time commitment: 1-2 hours**

1. [Installation Guide](guides/installation.md) - Production deployment
2. [Performance Tuning](technical-reference/performance.md) - Optimization strategies
3. [Monitoring](guides/monitoring.md) - Health checks and observability
4. [Troubleshooting](guides/troubleshooting.md) - Diagnostic procedures

---

## 📚 Documentation Structure

### Overview & Concepts
**Start here if you're new to Hartonomous**

- [**OVERVIEW.md**](OVERVIEW.md)  
  10,000-foot view of the system architecture, design philosophy, and value proposition

- [**Core Concepts**](architecture/core-concepts.md)  
  Fundamental principles: spatial embeddings, trilateration, atomic streams, and autonomous operation

### Architecture Documentation
**Deep technical understanding of system design**

- [**Architecture Overview**](architecture/README.md)  
  Complete architectural breakdown organized by layers

- [**Storage Layer**](architecture/storage-layer.md)  
  GEOMETRY datatypes, UDTs, In-Memory OLTP, and FILESTREAM

- [**Computation Layer**](architecture/computation-layer.md)  
  CLR functions, SIMD optimization, and aggregates

- [**Intelligence Layer**](architecture/intelligence-layer.md)  
  Neural networks, attention mechanisms, and reasoning frameworks

- [**Autonomy Layer**](architecture/autonomy-layer.md)  
  OODA loop, self-improvement, and Service Broker orchestration

- [**Provenance Layer**](architecture/provenance-layer.md)  
  AtomicStream tracking, temporal versioning, and compliance

### Capabilities Documentation
**Revolutionary features enabled by the architecture**

- [**Capabilities Overview**](capabilities/README.md)  
  Summary of emergent capabilities organized by tier

- [**Tier 1: Unique Capabilities**](capabilities/tier1-unique.md)  
  Features nobody else has: temporal archaeology, billion-parameter SQL models, nano-provenance

- [**Tier 2: Integration Innovations**](capabilities/tier2-integration.md)  
  Cross-modal reasoning, consensus reality, semantic fields

- [**Tier 3: Performance Innovations**](capabilities/tier3-performance.md)  
  Spatial optimizations for traditional ML workloads

- [**Tier 4: Meta-Learning**](capabilities/tier4-meta-learning.md)  
  Interpretability, self-improvement, and introspection

- [**Use Cases**](capabilities/use-cases.md)  
  Real-world applications and implementation patterns

### Technical Reference
**Detailed specifications and API documentation**

- [**API Reference**](technical-reference/api-reference.md)  
  Complete stored procedure and function catalog

- [**CLR Functions**](technical-reference/clr-functions.md)  
  Custom aggregates, SIMD operations, and UDT documentation

- [**Vector Operations**](technical-reference/vector-operations.md)  
  Embedding, similarity, and spatial projection functions

- [**ML Aggregates**](technical-reference/ml-aggregates.md)  
  Neural, reasoning, graph, time-series, anomaly, and recommender aggregates

- [**Data Schema**](technical-reference/schema.md)  
  Table definitions, indexes, and relationships

- [**Performance Benchmarks**](technical-reference/performance.md)  
  Metrics, optimizations, and tuning guidelines

- [**Integration Patterns**](technical-reference/integration.md)  
  REST API, event streaming, and external system connections

### Guides & How-Tos
**Practical instructions for common tasks**

- [**Quick Start**](guides/quick-start.md)  
  10-minute setup and first inference

- [**Installation**](guides/installation.md)  
  Detailed production deployment instructions

- [**Model Management**](guides/model-management.md)  
  Ingesting, optimizing, and versioning models

- [**Development Workflow**](guides/development.md)  
  Local setup, coding standards, and contribution process

- [**Testing Guide**](guides/testing.md)  
  Unit, integration, and end-to-end testing strategies

- [**Monitoring & Observability**](guides/monitoring.md)  
  Health checks, metrics, and logging

- [**Troubleshooting**](guides/troubleshooting.md)  
  Common issues and diagnostic procedures

- [**Security & Compliance**](guides/security.md)  
  Authentication, authorization, and audit trails

---

## 🗺️ Learning Paths

### Path 1: "I need to evaluate this platform" (30 minutes)
```
1. OVERVIEW.md (10 min)
2. capabilities/use-cases.md (10 min)
3. technical-reference/performance.md (10 min)
```

### Path 2: "I need to deploy this in production" (3 hours)
```
1. OVERVIEW.md (15 min)
2. guides/installation.md (60 min)
3. guides/monitoring.md (30 min)
4. technical-reference/performance.md (30 min)
5. guides/troubleshooting.md (45 min)
```

### Path 3: "I need to understand the architecture" (4 hours)
```
1. OVERVIEW.md (15 min)
2. architecture/core-concepts.md (30 min)
3. architecture/README.md (90 min)
4. capabilities/README.md (60 min)
5. technical-reference/api-reference.md (45 min)
```

### Path 4: "I need to build a feature" (5 hours)
```
1. guides/quick-start.md (20 min)
2. guides/development.md (40 min)
3. technical-reference/api-reference.md (60 min)
4. architecture/README.md (90 min)
5. guides/testing.md (30 min)
6. Hands-on experimentation (120 min)
```

### Path 5: "I need to optimize ML workloads" (3 hours)
```
1. OVERVIEW.md (15 min)
2. technical-reference/vector-operations.md (45 min)
3. technical-reference/ml-aggregates.md (60 min)
4. guides/model-management.md (45 min)
5. technical-reference/performance.md (15 min)
```

---

## 📖 Documentation Conventions

### File Organization
- `README.md` in each directory provides overview and navigation
- Files are chunked into 100-300 line modules for readability
- Cross-references use relative paths for portability

### Navigation Aids
- **Breadcrumbs** at top of each document
- **See Also** sections linking related content
- **Next Steps** at bottom guiding progression

### Code Examples
- All SQL examples are tested and runnable
- CLR code includes namespace and assembly context
- Performance characteristics noted where relevant

### Versioning
- Documentation versioned with codebase
- **Last Updated** timestamps on all documents
- Change history in git commits

---

## 🔍 Search Tips

### Finding Specific Topics
- **Vector operations**: `technical-reference/vector-operations.md`
- **Spatial indexing**: `architecture/storage-layer.md`
- **Model ingestion**: `guides/model-management.md`
- **Performance tuning**: `technical-reference/performance.md`
- **SIMD optimization**: `architecture/computation-layer.md`
- **Autonomous behavior**: `architecture/autonomy-layer.md`
- **AtomicStream**: `architecture/provenance-layer.md`
- **Multi-modal generation**: `capabilities/tier1-unique.md`

### Common Questions
- "How fast is it?" → `technical-reference/performance.md`
- "How do I deploy?" → `guides/installation.md`
- "What makes it unique?" → `capabilities/tier1-unique.md`
- "How does it work?" → `architecture/README.md`
- "How do I use it?" → `guides/quick-start.md`
- "What can it do?" → `capabilities/use-cases.md`

---

## 🚀 Next Steps

**New to Hartonomous?**  
Start with [System Overview](OVERVIEW.md) to understand the platform's value proposition and design philosophy.

**Ready to deploy?**  
Follow the [Installation Guide](guides/installation.md) for production deployment instructions.

**Building features?**  
Check out the [Developer Guide](guides/development.md) and [API Reference](technical-reference/api-reference.md).

**Optimizing ML workloads?**  
Explore [ML Aggregates](technical-reference/ml-aggregates.md) and [Model Management](guides/model-management.md).

---

**Document Version:** 1.0  
**Last Updated:** November 6, 2025  
**Maintained by:** Hartonomous Core Team
