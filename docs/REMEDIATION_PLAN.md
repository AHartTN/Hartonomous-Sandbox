# Hartonomous Remediation Plan (2025-10-31)

This document captures the current production gaps versus documented expectations and lays out the remediation workstreams needed to make the system production-ready.

## 1. Data Schema & Migrations

- **Issue**: EF Core migrations only create a subset of the documented data model (e.g., no `Atoms`, `AtomEmbeddings`, `TensorAtoms`, or knowledge tables). Stored procedures reference non-existent objects.
- **Actions**:
  - Audit documentation (`docs/`, `sql/schemas/`) and enumerate definitive table requirements.
  - Extend migrations to include atom substrate, tensor atoms, ingestion jobs, dedup policies, etc., with `VECTOR`, `GEOMETRY`, and JSON mappings.
  - Generate seed data migrations where docs mandate initial policies or vocabularies.
  - Remove or archive stale reference SQL once parity achieved.

## 2. Stored Procedures & SQL Assets

- **Issue**: Procedures expect schemas that migrations do not create; deployment script rewrites `CREATE` but does not guard dependencies; DiskANN logic still gated on GA.
- **Actions**:
  - Reconcile each procedure with actual schema; adjust parameters, table names, and column types.
  - Introduce EF migration(s) to deploy required procedures or encapsulate via idempotent scripts with dependency checks.
  - Add tests using `sqlcmd` or `Microsoft.Data.SqlClient` to validate sprocs post-deploy.
  - Keep DiskANN commented but document activation path.

## 3. Application Layer & Services

- **Issue**: `EmbeddingRepository` is an empty stub; `ModelRepository.UpdateLayerWeightsAsync` no-ops; ingestion services do not persist spatial components or production tables.
- **Actions**:
  - Implement full repository logic covering CRUD, VECTOR, and spatial columns.
  - Wire ingestion services to repositories (or rewrite to EF-first) and ensure spatial projection writes both vector and geometry fields.
  - Remove `.old` files and finish refactors.

## 4. CDC → EventHub → Neo4j Pipeline

- **Issue**: `CdcRepository` hardcodes `cdc.dbo_Models_CT`; CloudEvent extensions serialized as anonymous objects then consumed as dictionaries, causing runtime failures; LSN checkpoint stored in flat file.
- **Actions**:
  - Generalize CDC queries (schema/table parameters) and return strongly typed payloads.
  - Serialize CloudEvents using a schema that round-trips; adjust Neo4j processor to consume typed extensions.
  - Replace file checkpoints with durable storage (SQL table or blob).
  - Add backpressure, retries, and configuration validations.

## 5. SQL CLR

- **Issue**: CLR functions still assume VARBINARY vectors despite native VECTOR type; deployment story incomplete.
- **Actions**:
  - Remove obsolete distance functions; retain only arithmetic operations not covered by native features.
  - Provide automated deployment (PowerShell script) that builds and issues `CREATE ASSEMBLY` statements with signature verification.

## 6. Testing & Quality

- **Issue**: Unit tests are placeholders; no integration tests; no CI pipeline.
- **Actions**:
  - Build unit tests for dedup logic, repositories, and services using in-memory and SQL containers.
  - Create integration tests that spin up SQL Server 2025 RC1 (container) and validate migrations + sprocs.
  - Add GitHub Actions workflow covering build, test, lint, and SQL deployment smoke tests.

## 7. Deployment & Operations

- **Issue**: `deploy-database.ps1` checks wrong table names (e.g., `Embeddings` vs `Embeddings_Production`); no environment validation; CLR deployment skipped silently.
- **Actions**:
  - Update script to query actual EF table names via INFORMATION_SCHEMA or `sys.tables`.
  - Parameterize and validate required tooling versions (dotnet, sqlcmd, Azurite, Neo4j).
  - Provide rollback guidance and environment bootstrap instructions.

## 8. Documentation Alignment

- **Issue**: README and architecture docs overstate current implementation.
- **Actions**:
  - Update docs to reflect actual capabilities or prioritize implementation to meet claims.
  - Maintain a changelog of delivered vs planned features.

## Immediate Next Steps

1. Finalize schema parity: catalogue required tables and update migrations.
2. Implement repository logic and wire ingestion to EF context.
3. Normalize CloudEvent serialization path to unblock CDC pipeline.
4. Establish integration test harness (SQL container + basic stored-proc smoke).

Progress against this plan should be tracked in the repository (issues/PRs) with automated validation wherever possible.
