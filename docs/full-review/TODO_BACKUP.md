# TODO_BACKUP.md

## Purpose and Context

- Snapshot of the refactoring plan captured before build/integration fixes, acting as a backup of tasks and status.
- Tracks consolidation efforts, interface audits, and long-term roadmap phases for Hartonomous.

## Structure and Content

- Organized into numbered phases covering repository consolidation, service unification, interface audits, architectural reorganization, Azure integrations, performance improvements, and more.
- Provides status indicators (NOT STARTED, IN-PROGRESS) alongside detailed action items and notes about existing artifacts (e.g., orphaned interfaces, misplaced model classes).
- Includes a "Current State Summary" listing 178+ files created but not integrated, critical issues, and immediate priorities with sequencing.

## Notable Details

- Documents misplacement of model classes under Interfaces and the prevalence of multi-class files needing splits.
- Highlights that new files were added without updating `.csproj` references, causing builds to break—critical context for recovery.
- Emphasizes need to remove `Hartonomous.Sql.Bridge` project reference and integrate newly added assets.

## Potential Risks / Follow-ups

- Use this checklist to drive remediation; ensure each phase updates corresponding documentation to avoid divergence.
- Once tasks are completed, either update this backup or supersede it with a living plan to prevent conflicting guidance.
- Monitor for duplication across other documents (e.g., `RECOVERY_STATUS.md`) to maintain a single source of truth.
