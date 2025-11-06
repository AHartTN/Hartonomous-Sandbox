# Hartonomous

**The AI Platform That Lives in Your Database**

[![License](https://img.shields.io/badge/license-All%20Rights%20Reserved-red.svg)](LICENSE)

## What It Does

Hartonomous turns SQL Server into a complete AI inference engine. No external services, no microservices, no containers. Just your database doing multimodal AI generation, semantic search, and autonomous reasoning - all from T-SQL stored procedures.

It stores neural network weights as spatial geometries and runs inference by querying them. Embeddings become 3D coordinates. Attention mechanisms become spatial searches. The entire inference pipeline executes inside your database transaction.

## Why You'd Want This

**Your AI infrastructure collapses into your existing database stack.** No Kubernetes, no model servers, no vector databases. Your DBAs already know how to manage it. Your backup strategy already covers it. Your security policies already apply.

**Query semantics become literal SQL queries.** Want the 10 most similar documents? `SELECT TOP 10 ... ORDER BY VECTOR_DISTANCE()`. Want to trace where a generated answer came from? `MATCH (source)-[:USED_IN]->(result)`. Want to bill based on usage? `INSERT INTO BillingLedger` happens in the same transaction as the inference.

**The system improves itself while you sleep.** Service Broker orchestrates an autonomous OODA loop that observes query patterns, generates optimizations, tests them, and deploys the winners. It learns which indexes to create, which queries to rewrite, which embeddings to pre-compute.

**Everything is auditable by design.** Every inference request creates an immutable provenance chain stored in temporal tables and graph structures. Legal asks "where did this AI output come from?" You run a SQL query. Compliance asks "show me every model weight that contributed to this decision." You run a graph traversal.

## How It Works

**Neural weights become spatial data.** A 62GB model becomes a `GEOMETRY(LINESTRING)` with billions of points. You don't load it into memory - you query it. `STPointN(index)` fetches exactly the weights you need for this inference, right now.

**Embeddings become coordinates.** That 1998-dimensional vector? Projected to 3D space using trilateration. Now semantic similarity is geometric proximity. Now nearest-neighbor search is spatial indexing.

**Attention becomes spatial search.** Instead of matrix multiplication over billions of parameters, you do a spatial R-tree query that pre-filters to the relevant region, then exact cosine similarity on the survivors. 

**Autonomous operation via Service Broker.** The system runs an OODA loop (Observe-Orient-Decide-Act-Learn) that analyzes performance, generates hypotheses about improvements, tests them, and deploys the winners. No human intervention required.

**Provenance via graph + temporal tables.** Every atom of computation gets tracked: which embeddings were retrieved, which weights were accessed, which aggregations were performed. Full lineage from input to output, immutable, queryable.

## What You Can Build

**Semantic search that actually works.** Not "find documents with similar embeddings." More like "find the visual equivalent of this audio clip" or "show me research that contradicts this conclusion." Cross-modal, cross-domain, with full provenance showing exactly why each result matched.

**AI systems that explain themselves.** Every output includes the complete chain of reasoning: which source documents were retrieved, which model weights fired, which heuristics applied. Not black-box scores - actual traversable graphs you can query.

**Inference that scales with your data, not your infrastructure.** Your model is stored as geometry. Your indexes are spatial. Your queries use the same optimizer that handles your transactional workload. Add more cores, get more throughput. No separate scaling strategy.

**Autonomous analytics that improve over time.** The system watches which queries are slow, generates hypotheses about better indexes or better embeddings, tests them, measures results, keeps winners. Your semantic search gets faster every week without you touching it.

**Multi-tenant AI with row-level security.** Tenant isolation isn't a middleware concern - it's `WHERE TenantId = @TenantId` in the database. Rate limiting is a trigger. Billing is a native compiled stored procedure. Security policies are SQL Server security policies.

## Technical Foundation

This isn't a hack. It's a fundamental rethinking of where AI inference belongs in your stack.

**Spatial datatypes as neural storage.** `GEOMETRY` types give you billion-element arrays with native indexing and lazy access. `STPointN()` gives you O(1) random access without loading the full structure. Your 62GB model becomes a queryable geometry instead of a memory-resident blob.

**R-tree indexes for semantic search.** Project high-dimensional embeddings to 3D coordinates. Build spatial indexes. Now k-nearest-neighbor search becomes "find points within this bounding box, then rank by exact distance." Spatial index eliminates 99.9% of candidates in milliseconds.

**CLR integration for performance-critical paths.** AVX2 SIMD vector operations run orders of magnitude faster than T-SQL loops. Batch-aware aggregates process 900 rows at once. GPU acceleration for on-prem deployments via UNSAFE assemblies.

**Service Broker for autonomous orchestration.** Queue-based message passing with ACID guarantees. Poison message handling. Conversation groups for workflow coordination. The autonomous loop runs as a series of messages through Service Broker, completely decoupled from your application tier.

**Graph + temporal tables for provenance.** SQL Server Graph gives you `MATCH` queries over computation graphs. Temporal tables give you point-in-time queries over how embeddings evolved. Combine them and you get "show me the lineage of this inference as of last Tuesday."

**In-Memory OLTP for real-time metrics.** Billing ledgers, request tracking, usage quotas - all in native compiled stored procedures with hash indexes. Microsecond inserts. Lock-free reads. No external metrics infrastructure needed.

## Quick Start

```powershell
# Deploy the database
.\scripts\deploy-database.ps1 -ServerInstance "localhost" -DatabaseName "Hartonomous"

# Deploy CLR assemblies (on-prem with GPU support)
.\scripts\deploy-clr-unsafe.sql

# Or for cloud deployment (Azure SQL MI)
.\scripts\deploy-clr-safe.sql
```

```sql
-- Ingest a model
EXEC dbo.sp_IngestModel 
    @ModelName = 'llama-3.1-8b',
    @FilePath = 'D:\Models\llama-3.1-8b.gguf';

-- Run inference
EXEC dbo.sp_GenerateText 
    @Prompt = 'Explain how neural networks are stored as geometry',
    @MaxTokens = 500;

-- Semantic search
EXEC dbo.sp_SearchSemanticVector
    @QueryText = 'machine learning deployment strategies',
    @TopK = 10;

-- Get full provenance
SELECT * FROM dbo.fn_GetAtomLineage(@ResultAtomId);
```

## Documentation

**[docs/OVERVIEW.md](docs/OVERVIEW.md)** - System architecture and design philosophy  
**[docs/INDEX.md](docs/INDEX.md)** - Full documentation index  
**[docs/CLR_DEPLOYMENT_STRATEGY.md](docs/CLR_DEPLOYMENT_STRATEGY.md)** - Deployment guide

## Project Structure

```
Hartonomous/
├── src/
│   ├── SqlClr/                    # CLR functions and aggregates
│   ├── Hartonomous.Api/           # REST API layer
│   ├── Hartonomous.Core/          # Shared domain models
│   └── CesConsumer/               # Event streaming consumer
├── sql/
│   ├── procedures/                # Stored procedures
│   ├── tables/                    # Schema definitions
│   └── types/                     # User-defined types
├── tests/
│   ├── Hartonomous.UnitTests/
│   ├── Hartonomous.IntegrationTests/
│   └── Hartonomous.EndToEndTests/
├── docs/                          # Comprehensive documentation
└── deploy/                        # Deployment scripts
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Application Layer                                           │
│ (T-SQL stored procedures, REST API, Blazor clients)        │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│ Autonomous Layer (Service Broker OODA Loop)                 │
│ Observe → Orient → Decide → Act → Learn                     │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│ Intelligence Layer (CLR Aggregates & Functions)             │
│ Neural nets, clustering, attention, reasoning               │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│ Computation Layer (SIMD, GPU, Batch Processing)             │
│ AVX2/AVX512 vector ops, native compilation                  │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│ Storage Layer (Spatial, Graph, Temporal, In-Memory)         │
│ GEOMETRY, Graph nodes/edges, Temporal tables, FILESTREAM    │
└─────────────────────────────────────────────────────────────┘
```

**Built on:**
- SQL Server 2025 (spatial types, graph, Service Broker, In-Memory OLTP)
- .NET 9 CLR integration (SIMD, native compilation, GPU via UNSAFE)
- C# 12 (advanced language features, performance optimizations)
- ASP.NET Core 9 (REST API, authentication, rate limiting)
- Blazor (admin dashboard and client applications)

## Why This Matters

**Infrastructure consolidation.** You're running vector databases, model servers, message queues, graph stores, and a relational database. Hartonomous collapses all of that into SQL Server. Fewer systems to manage, fewer failure modes, simpler operations.

**Transactional semantics for AI.** Your inference happens in a transaction. Either the whole operation succeeds (model accessed, embeddings retrieved, result generated, billing recorded, provenance logged) or it all rolls back. No eventual consistency, no distributed transactions, no reconciliation jobs.

**Compliance without bolt-ons.** You don't add audit logging to your AI system. The AI system IS audit logging. Every operation creates immutable provenance records. Temporal tables give you time-travel. Graph edges give you lineage. It's not "auditable" - it's "query the database."

**Performance through architecture, not scale.** Traditional ML systems scale by adding GPUs and model servers. Hartonomous scales by using better algorithms: spatial indexes instead of brute-force search, lazy evaluation instead of eager loading, native compilation instead of interpreted execution. You get orders-of-magnitude improvements without buying more hardware.

**Autonomous operation as a first principle.** The system doesn't wait for you to tune it. It observes its own behavior, generates hypotheses about improvements, tests them in shadow mode, measures results, and deploys winners. Your database gets smarter over time.

## Who This Is For

**Teams that need AI but don't want to become infrastructure experts.** Your DBAs already know SQL Server. Your devs already write stored procedures. Your security team already has policies for database access. Hartonomous fits into your existing competencies.

**Organizations with serious compliance requirements.** Financial services, healthcare, legal - anywhere you need to prove where AI outputs came from and show that the system behaved correctly. Graph traversals and temporal queries give you that proof.

**Products that need semantic search without vendor lock-in.** You're not calling an embedding API and storing vectors in someone else's cloud. You're storing your own data as geometry in your own database. You control the indexes, the queries, the data retention.

**Research platforms that need to study their own behavior.** Because everything is stored as queryable data structures, you can analyze the system's decision-making process. "Show me all inferences where the model was uncertain" becomes a SQL query.

**Anyone tired of duct-taping microservices together.** You wanted AI in your app. Instead you got Kubernetes, Kafka, Redis, Pinecone, and a model server, all of which need monitoring, scaling, and debugging. Hartonomous is one database.

## Current Status

Active development. Core inference engine works. Autonomous OODA loop functional. Provenance tracking operational. See `git log` for detailed progress.

The vision: AI infrastructure that's as reliable, queryable, and manageable as your database. Not there yet. But getting closer every commit.

## License

Copyright © 2025 Hartonomous. All Rights Reserved.

---

Built on SQL Server 2025, .NET 9, and the belief that databases are underutilized for AI workloads.
