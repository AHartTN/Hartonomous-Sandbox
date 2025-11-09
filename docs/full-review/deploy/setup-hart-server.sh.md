# deploy/setup-hart-server.sh

## Purpose and Context

- Bash bootstrap for provisioning the Hartonomous application server (`HART-SERVER`).
- Enforces execution by the `ahart` account and prepares prerequisites prior to deploying services.

## Key Actions

- Installs the .NET runtime, preferring version 10, then falling back to 9 or 8 if packages are unavailable.
- Creates the `/srv/www/hartonomous` directory tree with subfolders for API, CES consumer, Neo4j sync, and model ingestion workloads.
- Reports disk usage for `/srv/www` and provides next steps to execute the PowerShell deployment and verify systemd services.

## Potential Risks / Follow-ups

- `dotnet-runtime-10.0` packages are not yet generally available; verify repository support or pin to a stable, released version.
- Script assumes Ubuntu and availability of `lsb_release`; add guards for alternate distributions or missing dependencies.
- Requires sudo privileges for package install, yet script requires user `ahart`; clarify privilege escalation expectations.
- Consider setting directory ownership/permissions beyond `chmod 755` to ensure service user write access where needed.
