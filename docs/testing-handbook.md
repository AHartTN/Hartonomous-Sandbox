# Testing Handbook

This handbook captures the shared conventions for building and running the Hartonomous test suites. It complements `docs/development-handbook.md` and evolves alongside the Optimized Testing Initiative.

## Anchor Goals

- **Production fidelity:** Exercise Service Broker, SQL schema, CLR UDTs, Neo4j projections, Admin UI, CLI, and billing flows with realistic infrastructure.
- **Additive & idempotent:** Leverage content-addressable assets and deterministic fixtures so suites can run repeatedly without teardown pains.
- **Developer ergonomics:** Prefer single-command entry points, fast inner loops, and clear diagnostics when something fails.

## Suite Topology

| Suite | Purpose | Runtime Profile |
| --- | --- | --- |
| `Hartonomous.UnitTests` | Pure logic and fast feedback. | < 1 minute |
| `Hartonomous.IntegrationTests` | Service + SQL Server integration paths. | Several minutes |
| `Hartonomous.DatabaseTests` | Database-native contracts (tSQLt, CLR, schema assertions). | Several minutes |
| `Hartonomous.EndToEndTests` | Admin UI, CLI, and cross-service workflows. | Longer, gated |

Shared assets and helpers live under `tests/Common` and are linked into every project via `Directory.Build.props`.

## Deterministic Assets

- Text fixtures: `TestData.Text.*`
- Identity seeds: `TestData.Json.Identity.*`
- Hash enforcement: `Hartonomous.Testing.Common.Hashing.AssetHashValidator`
- Strongly typed helpers: `Hartonomous.Testing.Common.Seeds.IdentitySeedData`

Add new assets under `tests/Common/TestAssets` and expose them through `TestData`. Compute and record the SHA-256 hash so tests can assert integrity.

## Build & Tooling Conventions

- Target framework: `net10.0`
- Test SDK: `Microsoft.NET.Test.Sdk`
- Runner: `xUnit`
- Coverage: `coverlet.collector` with JSON/LCOV/OpenCover outputs under `artifacts/coverage/<ProjectName>/`
- Analyzer configuration: `EnableNETAnalyzers=true`, `AnalysisLevel=latest`

## Fixture Roadmap

1. **Containerized SQL Server**
   - Provision via `DotNet.Testcontainers`
   - Execute `scripts/deploy-database.ps1`
   - Install SQL CLR and billing seeds
   - Snapshot baseline for assertions
   - Capture deployment output and skip gracefully when Docker is unavailable
   - Contract tests validate stored procedures and `provenance.AtomicStream`
2. **Neo4j Testcontainer (optional)**
   - APOC enabled
   - Seed provenance graphs
3. **Shared smoke test**
   - Validates DbContext, Service Broker messaging, and baseline data

## Reflection Checkpoints

Each batch closes with a brief reflection:

- What failed first? (capture flaky steps)
- Does the suite still run with one command?
- Are diagnostics sufficient when a failure occurs?

Record outcomes in project notes and file follow-up issues for deferred work.

## Next Steps

- Finish container fixtures and smoke test harness
- Expand unit coverage for billing multipliers and retry policies
- Introduce tSQLt suite for stored procedures
- Select UI automation stack (Playwright + bUnit recommended)
