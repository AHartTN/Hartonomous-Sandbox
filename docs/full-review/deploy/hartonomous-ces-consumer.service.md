# deploy/hartonomous-ces-consumer.service

## Purpose and Context

- systemd unit for the CES Consumer worker running from `/srv/www/hartonomous/ces-consumer`.
- Mirrors API unit structure to manage the background service lifecycle.

## Key Settings

- Executes `CesConsumer.dll` via `/usr/bin/dotnet`, running under `hartonomous` user with `Type=notify`.
- Configured to restart on failure with a 10-second delay; logs tagged `hartonomous-ces-consumer`.
- Sets production environment and suppresses telemetry messages.

## Potential Risks / Follow-ups

- Confirm service emits systemd notifications; otherwise, adjust service type to avoid startup issues.
- Validate working directory and binary names align with current deployment (especially if merged into a worker project).
- Consider additional environment variables for connection strings or queue configuration.
