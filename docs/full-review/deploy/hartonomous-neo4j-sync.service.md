# deploy/hartonomous-neo4j-sync.service

## Purpose and Context

- systemd unit governing the Neo4j synchronization worker hosted at `/srv/www/hartonomous/neo4j-sync`.
- Aligns runtime model with other Hartonomous .NET services (same user, environment, restart policy).

## Key Settings

- Runs `Neo4jSync.dll` with `dotnet`, uses `Type=notify`, and restarts on failure with a 10-second delay.
- Registers logs under `hartonomous-neo4j-sync`, disables .NET telemetry messages, forces production environment.
- Depends on `network.target` and `neo4j.service`, ensuring sync starts after database availability.

## Potential Risks / Follow-ups

- `Type=notify` assumes the app emits systemd notifications; confirm implementation to avoid startup deadlocks.
- Validate the `hartonomous` user has execute permissions and access to Neo4j credentials/configuration.
- Consider additional environment variables (connection strings, auth secrets) if required but currently absent.
