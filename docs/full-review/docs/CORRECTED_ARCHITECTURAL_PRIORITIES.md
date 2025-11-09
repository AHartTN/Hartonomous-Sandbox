# docs/CORRECTED_ARCHITECTURAL_PRIORITIES.md

## Purpose and Context

- Follow-up architectural audit dated 2025-11-08 correcting earlier findings based on user clarification.
- Reframes priority issues as moderate severity, emphasizing specific remediation steps for Hartonomous service layout and Azure integrations.

## Key Points

- Confirms certain earlier concerns were false positives (no Dapper usage, third-party packages intentional, background console apps are production services).
- Highlights true architectural problems: model ingestion logic trapped in console project, duplication in `Hartonomous.Api`, need to merge `Hartonomous.Data` into Infrastructure, Core referencing infrastructure packages, and incomplete Azure integrations.
- Provides detailed plan to extract ingestion/business code into Infrastructure, reorganize repositories, clean domain dependencies, and enable Azure Service Bus usage.
- Suggests phased remediation roadmap (Phase 0 foundational cleanup → Phase 1 extensions → Phase 2 organization) with estimated effort.

## Potential Risks / Follow-ups

- Recommendations assume repository still matches described structure; validate before executing migrations.
- Proposed refactors affect multiple projects and deployment scripts—coordinate testing and environment updates.
- Azure integration tasks require production credential management and monitoring adjustments not detailed here.
