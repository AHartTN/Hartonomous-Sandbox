# deploy/hartonomous-api.service

## Purpose and Context

- systemd service unit for running the Hartonomous API under `/srv/www/hartonomous/api`.
- Designed for user-level deployment with restart behavior and production environment configuration.

## Key Settings

- Runs as `hartonomous` user, using `dotnet` to execute `Hartonomous.Api.dll`.
- `Type=notify` indicates the service should send readiness notifications; requires API to call `sd_notify` or similar.
- Auto-restarts with 10-second delay; logs identified as `hartonomous-api` via Syslog.
- Sets `ASPNETCORE_ENVIRONMENT=Production` and disables telemetry messages.

## Potential Risks / Follow-ups

- Ensure the API actually emits systemd notifications—if not, consider `Type=simple` to avoid startup timeouts.
- Verify working directory and paths match deployed layout; update if publish output changes.
- Add environment configuration for URLs, certificates, or secrets as needed for production deployments.
