# azure-pipelines.yml

## Purpose and Context

- Primary Azure DevOps pipeline orchestrating build, database deployment, application deployment, and smoke testing.
- Targets multiple environments (Arc-enabled SQL on HART-DESKTOP, production API/services on HART-SERVER) with mix of Windows and Linux agents.

## Structure and Flow

1. Trigger/variables definition (currently malformed) intended to run on `main` and maybe `develop` branches, with common build settings like `buildConfiguration`, `dotnetSdkVersion`, and SQL deployment parameters.
2. **Stage: Build**
   - Restores, builds, tests, and publishes artifacts for all .NET projects (API, CesConsumer, Neo4jSync, ModelIngestion, SQL CLR).
   - Copies deployment scripts and service files into artifact staging; publishes combined `drop` artifact.
3. **Stage: DeployDatabase / DeployDatabaseToArc**
   - Intended to build CLR assemblies, copy SQL scripts, package deployment artifacts, and execute PowerShell/SSH steps against Arc-enabled SQL instance on `HART-DESKTOP`.
   - Runs database deployment orchestrator, uploads artifacts via SSH, and executes verification queries.
4. **Stage: DeployApplications / DeployToProduction**
   - Deploys IIS website for API on Windows target and systemd services on Linux host; handles service stop/copy/start cycles for CesConsumer, Neo4jSync, ModelIngestion.
5. **Stage: SmokeTest**
   - Executes health checks for API endpoints, verifies Windows services, and exercises embedding generation API.

## Notable Details

- Multiple duplicate/misaligned definitions (e.g., repeated stage/job sections, YAML keys mashed on single lines) imply the file is corrupted or merged incorrectly; Azure DevOps would fail to parse as-is.
- Uses both classic Windows service deployment scripts and Linux/systemd commands within same stage, suggesting hybrid deployment targets.
- Contains inline PowerShell and Bash scripts with hard-coded paths, credentials referenced via pipeline variables (`$(SQL_USERNAME)` etc.), and direct `systemctl` calls requiring user-level systemd.
- Stage gating uses `dependsOn` and `condition` expressions to limit production deployment to main branch, though indentation issues may break evaluation.

## Potential Risks / Follow-ups

- YAML syntax is invalid: keys run together (`trigger:trigger`, `branches:  - main`), indentation is corrupted, and sections repeat; pipeline cannot run until fully reformatted.
- Variable scoping inconsistent (`variables:` repeated in multiple places) and may override unexpectedly even after reformatting.
- Mixed Windows/Linux deployment steps should confirm target agents and service management approaches remain accurate; consider separate stages per environment for clarity.
- Credentials (`$(SQL_PASSWORD)` etc.) must be set as secure variables; verify no secrets are exposed in inline scripts or logs.
- Consider modularizing script execution (invoke existing deploy scripts instead of large inline blocks) to reduce maintenance burden and enable local reuse.
