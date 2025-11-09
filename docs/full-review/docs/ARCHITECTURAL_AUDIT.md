# docs/ARCHITECTURAL_AUDIT.md

## Purpose and Context

- High-level architecture audit dated 2025-11-08 assessing the Hartonomous solution structure and technical debt.
- Highlights critical issues spanning project layout, layering violations, duplicated logic, and inconsistent data access patterns.

## Key Findings

- Flags proliferation of console apps (CesConsumer, ModelIngestion, Neo4jSync, Admin) and advocates consolidating shared logic into libraries or a single worker host.
- Identifies overlap between `Hartonomous.Data` and `Hartonomous.Infrastructure`, recommending merger and clear separation of EF Core, Dapper, and SQL CLR responsibilities.
- Notes dependency inversion breaches where `Hartonomous.Core` references infrastructure packages like `Microsoft.Data.SqlClient` and `EntityFrameworkCore`.
- Pinpoints duplicated repositories, ingestion services, and DTOs scattered across projects, urging centralization under `Shared.Contracts` and Infrastructure.
- Summarizes SQL CLR scope creep, proposing limiting CLR usage to spatial/vector functions and moving other logic into services.

## Recommended Actions

- Immediate Phase 0 plan: migrate ModelIngestion business logic into Infrastructure, remove the standalone `Hartonomous.Data` project, purify Core dependencies, and reorganize shared contracts.
- Subsequent phases: finish extension refactors (sql/logging/validation) and reorganize files to eliminate duplicates.
- Offers decision options prioritizing architectural remediation before tactical cleanups.

## Potential Risks / Follow-ups

- Audit assumes current project realities; verify against latest repository state to avoid acting on outdated observations.
- Consolidation work affects build and deployment pipelines; ensure phased migrations include regression testing.
- Transitioning SQL CLR functionality requires careful coordination with database deployment processes.
