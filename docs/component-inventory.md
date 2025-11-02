# Hartonomous Workspace Inventory

Last updated: 2025-11-01

## Application & Service Projects

| Component | Path | Purpose | Key Tech | Status |
| --- | --- | --- | --- | --- |
| Hartonomous.Core | src/Hartonomous.Core | Domain entities, value objects, and core interfaces for ingestion, inference, and semantic graph domains. | .NET 8 class library | Core/Authoritative |
| Hartonomous.Data | src/Hartonomous.Data | Entity Framework Core DbContext, fluent configurations, and migrations backing the Hartonomous SQL Server schema. | EF Core, SQL Server | Core |
| Hartonomous.Infrastructure | src/Hartonomous.Infrastructure | Concrete service/repository implementations, shared DI extensions, and SQL connectivity helpers. | EF Core, Dapper-style SQL helpers | Core |
| Hartonomous.Admin | src/Hartonomous.Admin | Blazor Server administrative console with SignalR telemetry and background operation orchestration. | ASP.NET Core, SignalR | Core |
| ModelIngestion | src/ModelIngestion | Production ingestion orchestrator for model artifacts, embeddings, and atomic storage pipelines. | .NET worker, HttpClient, Application Insights | Core |
| CesConsumer | src/CesConsumer | SQL Server 2025 change data capture (CES) listener that enriches change events and publishes CloudEvents to Event Hubs. | .NET worker, Event Hubs | Active |
| Neo4jSync | src/Neo4jSync | Background service that consumes enriched CloudEvents and materializes provenance graphs in Neo4j. | .NET worker, Azure Event Hubs, Neo4j.Driver | Active |
| SqlClrFunctions | src/SqlClr | SQL CLR assembly exposing advanced spatial/audio/vector helpers for in-database processing. | SQL CLR | Active |
| tests suite | tests/* | Unit, integration, and ingestion-specific test harnesses aligned with core/infrastructure projects. | xUnit, .NET test SDK | Needs upkeep |

## Data & Reasoning Assets

| Area | Path | Purpose | Notes | Status |
| --- | --- | --- | --- | --- |
| SQL Procedures (modern) | sql/procedures/01_SemanticSearch.sql, 03_MultiModelEnsemble.sql, 05_SpatialInference.sql, 05_VectorFunctions.sql, 06_ConvertVarbinary4ToReal.sql, 06_ProductionSystem.sql, 07_AdvancedInference.sql, 08_SpatialProjection.sql, 09_SemanticFeatures.sql, 16_SeedTokenVocabularyWithVector.sql, 17_FeedbackLoop.sql, 21_GenerateTextWithVector.sql, 22_SemanticDeduplication.sql, sp_GenerateImage.sql, TextToEmbedding.sql | Canonical T-SQL surfaces for inference, search, and token management aligned with latest vector features. | `sp_GenerateText` now solely defined by version 21. | Core/Current |
| SQL Procedures (tests) | sql/procedures/02_TestSemanticSearch.sql (smoke harness) | Historical prototype retained for manual testing. Legacy scripts (`04_ModelIngestion.sql`, `04_GenerateText.sql`, `07_SeedTokenVocabulary.sql`, `15_GenerateTextWithVector.sql`) removed 2025-11-01 to cut noise. | Tighten harness or remove once automated coverage exists. | Review |
| Verification scripts | sql/verification/SystemVerification.sql | System smoke-test queries validating vector, spatial, and inference tables. | Keep to support deployment QA. | Useful |
| SQL indexes | (removed 2025-11-01) | Placeholder folder deleted; index DDL now lives in EF Core migrations and targeted scripts. | Recreate only when discrete index files are required. | Closed |
| Neo4j schema | neo4j/schemas/CoreSchema.cypher | Authoritative schema & reference queries for provenance graph. | Covers constraints, explainability queries, and initialization. | Core |
| Neo4j queries | (removed 2025-11-01) | Placeholder folder deleted; add versioned Cypher files alongside `schemas/` when concrete workloads emerge. | Reintroduce with curated query set. | Closed |
| Deployment scripts | scripts/deploy-database.ps1 | Automates database deployment steps (needs verification against latest SQL assets). | Ensure it references modern SQL files only. | Review |

## Modern vs Legacy Highlights

- **Text generation:** `sql/procedures/21_GenerateTextWithVector.sql` is the production pathway. Legacy prototypes were removed 2025-11-01 to prevent accidental deployment of stale logic.
- **Token vocabulary seeding:** Vector-native seed script (`16_SeedTokenVocabularyWithVector.sql`) is now the only loader after removing the varbinary-based prototype.
- **Model ingestion:** Live ingestion pipeline resides in `src/ModelIngestion`; SQL walkthrough retired. Update docs to describe orchestrator-driven flow instead of the deleted script.
- **Common folder:** The empty `src/Common` directory has been deleted. Shared abstractions live in `Hartonomous.Core` and `Hartonomous.Infrastructure`.
- **CES & Neo4j:** `src/CesConsumer` plus `src/Neo4jSync` form the event streaming pipeline. Both projects use the latest Azure Event Hubs and CloudEvents enrichments.

## Completed Cleanup Actions

- Deleted transient build artifacts (`bin/`, `obj/`) across the workspace.
- Removed empty `src/Common` directory.
- Deleted legacy SQL prototypes (`04_*`, `07_SeedTokenVocabulary.sql`, `15_GenerateTextWithVector.sql`) to reduce clutter.
- Removed placeholder test files (`tests/Hartonomous.Core.Tests/UnitTest1.cs`, `tests/ModelIngestion.Tests/UnitTest1.cs`).
- Deleted placeholder directories (`sql/indexes`, `neo4j/queries`) so only live assets remain under source control.

## Recommended Next Steps

1. **Document deployment workflows:** Validate `scripts/deploy-database.ps1` and capture the end-to-end deployment story (SQL + Neo4j) under `docs/`.
2. **Test coverage audit:** Re-run unit/integration suites post-cleanup and identify any missing scenarios (especially around new vector procedures); replace deleted placeholders with real tests.
3. **Neo4j query library:** Draft the canonical analytical queries referenced in `CoreSchema.cypher` and reintroduce them as versioned Cypher files.
4. **Operational runbooks:** Extend this documentation with environment-specific configuration notes (connection strings, Event Hub/Neo4j secrets handling).
