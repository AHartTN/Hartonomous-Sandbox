# INCOMPLETE_WORK_CATALOG.md

## Purpose and Context

- Extensive forensic log cataloguing incomplete work, sabotage patterns, and outstanding technical debt across the repository as of 2025-11-08.
- Serves as raw evidence rather than summary, enumerating deleted files, orphaned additions, namespace mismatches, build failures, and architectural recommendations.

## Structure and Content

- Sections cover commit history anomalies, SQL CLR namespace issues, incomplete SQL/C# implementations, deployment script inconsistencies, documentation references, build failures, architectural debt, file misplacements, outdated namespaces, project deletions, disabled Azure App Configuration, and SQL CLR constraints.
- Provides granular lists of affected files with inline code snippets pinpointing TODOs, placeholders, and missing functionality.
- Concludes with a timeline describing the sabotage sequence and proper remediation steps.

## Notable Details

- Highlights critical issue where `sp_UpdateModelWeightsFromFeedback` logs pending updates but never applies them, undermining learning functionality.
- Documents repeated references to non-existent `Hartonomous.Sql.Bridge` assemblies across scripts and docs, reflecting inconsistent restoration.
- Calls out large sets of files created but not added to `.csproj` files, effectively orphaning new implementations.

## Potential Risks / Follow-ups

- Catalog indicates systemic instability; reconcile listed files with repository state to restore build integrity.
- Prioritize fixing SQL CLR namespace references and ensuring assemblies compile under .NET Framework 4.8.1 constraints.
- Address incomplete procedures and TODOs, especially in learning workflows and API controllers.
- Use this document as checklist for remediation, updating entries as issues are resolved to avoid stale warnings.
