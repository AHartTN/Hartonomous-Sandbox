# Hartonomous Flagship Client Strategy

## Vision

Deliver a cohesive suite of flagship-grade clients (CLI, Admin UI, Public API, and future SDKs) that expose Hartonomous capabilities with identical reliability, ergonomics, and observability. Every client is treated as a first-class product, sharing the same contracts, telemetry, and operational playbooks.

## Guiding Principles

- **Shared Contracts First**: Stabilize domain DTOs, command models, events, and error codes in shared packages before building clients.
- **Single Source of Truth**: Core business logic lives in application services; clients consume these surfaces rather than duplicating behavior.
- **SOLID & DRY Execution**: Favor abstractions, generics, and dependency inversion to keep implementation interchangeable across channels.
- **Quality as a Feature**: Treat documentation, diagnostics, tests, and release processes as part of the product experience.
- **Observability Everywhere**: Uniform correlation IDs, structured logs, metrics, and tracing across all clients and backend services.

> **Execution Order:** API surfaces ship first; CLI/Admin/SDKs light up as API endpoints stabilize. Shared contracts and telemetry are non-negotiable prerequisites for every client.

## Platform Foundations (Weeks 0-2)

1. **Contract Package**
   - Project: `Hartonomous.Shared.Contracts`
   - Contents: DTOs, enums, result models, domain errors, correlation metadata, telemetry primitives.
   - Deliverables: NuGet packaging CI, semver versioning, analyzer enforcing contract usage across solutions.
2. **Core Application Services**
   - Refactor existing ingestion/generation/extraction services behind interfaces (e.g., `IModelIngestionCoordinator`, `IPipelineMonitor`, `IExportOrchestrator`).
   - Provide adapter layer for EF Core + SQL CLR bindings to encapsulate persistence details.
3. **Cross-Cutting Concerns**
   - Introduce shared middleware/components for auth, throttling, resilience, logging, and feature flags.
   - Define telemetry schema (ActivitySource names, metric dimensions).

## Public API Roadmap (Flagship #1)

1. **API Contract Definition** (Weeks 2-3)
   - gRPC + REST facades leveraging shared contracts.
   - API versioning strategy, OpenAPI/Protobuf publishing pipeline.
2. **Gateway & Auth** (Weeks 3-4)
   - Implement Identity integration (OAuth2/JWT), rate limiting, API keys, audit logging.
3. **Feature Endpoints (Parallels CLI)** (Weeks 4-6)
   - Model ingestion, pipeline execution, export packaging, telemetry queries.
4. **Quality Gates**
   - Contract tests vs shared service mocks.
   - Chaos/resilience tests (retry, circuit breaker validation).

## CLI Roadmap (Flagship #2)

1. **Experience Blueprint** (Week 2)
   - Define command taxonomy, narrative help content, UX style guide, JSON schema for machine-readable outputs.
   - Write CLI RFC documenting command structure and exit-code contract.
   - Document API dependencies per command; CLI work cannot close until backing endpoint is GA.
2. **Scaffolding & Infrastructure** (Weeks 3-4)
   - Project: `Hartonomous.Cli` using `System.CommandLine` + `Spectre.Console` for presentation.
   - Implement configuration stack (profiles, env overrides, secrets) and shared HTTP client factory.
   - Wire dependency injection to shared services.
3. **Priority Commands (MVP)** (Weeks 4-6)
   - `hart auth login/logout`
   - `hart model ingest/status`
   - `hart pipeline run/status`
   - `hart export package`
4. **Polish & Parity** (Weeks 6-7)
   - Interactive mode (`hart interactive`), shell completions (PowerShell, bash, zsh).
   - `--json` / `--quiet` / `--trace` flags wired to shared telemetry.
   - Golden-path integration tests using snapshot verification against staging API.
5. **Release Hardening** (Week 7)
   - Self-update check, signed binaries, installer scripts, documentation portal.

## Admin Portal Roadmap (Flagship #3)

1. **Design System & UX** (Weeks 3-4)
   - Shared UI component library (React + Tailwind or equivalent), consistent with CLI terminology.
2. **Operational Dashboards** (Weeks 4-6)
   - Ingestion queue, pipeline health, billing usage, audit trails.
3. **Action Surfaces**
   - Approve/reject ingestion jobs, roll back deployments, manage billing plans.
4. **Observability Integration**
   - Embed logs/metrics/traces using shared telemetry contracts.

## Future SDKs & Integrations

- **Language SDKs**: TypeScript, Python clients auto-generated from shared contracts, with handcrafted ergonomics for flagship feel.
- **Automation Hooks**: GitHub Actions templates, Terraform modules for provisioning pipelines.

## Testing & Quality Strategy

- **Unit & Integration**: Common test utilities project; consistent factories and fixtures.
- **End-to-End**: Scenario-based suites exercising CLI→API→DB pipeline (mockable but also staged environment flows).
- **Performance**: Load tests for ingestion/generation endpoints, CLI stress tests for bulk operations.
- **Security**: Static analysis, dependency audits, penetration tests, credential scanning pipelines.

## Deployment & Release Management

- **Environment Matrix**: Local (developer), Dev, Staging, Production; per-environment configuration bundles.
- **Release Train**: Monthly minor releases, weekly patch cadence; synchronized notes across CLI/API/Admin.
- **Rollback Procedures**: Documented restore scripts, migration downgrade paths, CLI hotfix delivery.

## Immediate Next Steps (Week 0 Backlog)

1. Stand up `Hartonomous.Shared.Contracts` project, move existing DTOs (model/layer/tensor ingest responses) into package.
2. Draft CLI command taxonomy & UX RFC under `docs/cli/cli-spec.md`.
3. Scaffold `Hartonomous.Cli` project with basic `hart --help` command pulling version info from Contracts package.
4. Integrate deployment script changes into CI to validate idempotent DB deploy on fresh/local environments.
5. Schedule design kickoff for Admin UI to align terminology with CLI spec.

This plan keeps the shared platform work slightly ahead of client deliverables, ensuring every flagship product shares the same backbone while allowing parallel execution once the contracts are stable.
