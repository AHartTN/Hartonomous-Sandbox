# deploy/hartonomous-model-ingestion.service

## Purpose and Context

- systemd unit to manage the Model Ingestion service deployed to `/srv/www/hartonomous/model-ingestion`.
- Maintains consistent configuration with other Hartonomous services.

## Key Settings

- Launches `ModelIngestion.dll` via `dotnet`, `Type=notify`, restarts automatically, logs under `hartonomous-model-ingestion`.
- Sets production environment and disables telemetry output.

## Potential Risks / Follow-ups

- Repository indicates ModelIngestion project was removed; ensure service remains valid or update to new worker architecture.
- Verify readiness notifications are implemented; adjust `Type` if necessary to prevent timeouts.
- Confirm environment variables cover any required credentials or configuration for ingestion workloads.
