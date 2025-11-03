# Deployment & Operations

This guide explains how to stand up Hartonomous environments and operate them day to day.  It assumes familiarity with SQL Server administration and Azure (or equivalent) infrastructure.

## Environment Blueprint

| Component | Notes |
| --- | --- |
| SQL Server 2025 | Enable Service Broker, CDC, vector/spatial types.  Suggested DB name: `Hartonomous`. |
| Neo4j 5.x | Single instance or clustered.  Configure Bolt URI + credentials in application settings. |
| Application Hosts | .NET 10 runtime, PowerShell 7.  Host services as Windows services, containers, or Azure App Service. |
| Event Broker (optional) | Azure Event Hubs recommended for cloud-scale ingestion, though the current stack relies on SQL Service Broker internally. |

## Provisioning Steps

1. **Create the database.**

   ```powershell
   ./scripts/deploy-database.ps1 -ServerName "sql-prod" -DatabaseName "Hartonomous" -TrustedConnection $false -SqlUser "svc_hartonomous" -SqlPassword "<secret>"
   ```

2. **Enable Service Broker.**

   ```sql
   ALTER DATABASE [Hartonomous] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
   ```

3. **Apply migrations.** Run from the CI/CD agent or the machine hosting deployments.

   ```powershell
   dotnet ef database update --project src/Hartonomous.Data --startup-project src/Hartonomous.Infrastructure --context HartonomousDbContext
   ```

4. **Publish SQL CLR artifacts.** Build the assembly, deploy the `AtomicStream` type, then recreate CLR helper bindings.

   ```powershell
   msbuild src/SqlClr/SqlClrFunctions.csproj /p:Configuration=Release
   sqlcmd -S . -d Hartonomous -i sql/types/provenance.AtomicStream.sql -v SqlClrAssemblyPath="D:\\deploy\\SqlClrFunctions.dll"
   sqlcmd -S . -d Hartonomous -i sql/procedures/Common.ClrBindings.sql
   sqlcmd -S . -d Hartonomous -i sql/tables/provenance.GenerationStreams.sql
   ```

5. **Seed reference data.** The deployment script (`deploy-database.ps1`) can seed base rate plans and sample atoms when `-Seed $true` is provided.
6. **Configure applications.**
   - Update `appsettings.Production.json` (or equivalent) with connection strings, Service Broker queue name, security options, and billing defaults.
   - Register `IServiceBrokerResilienceStrategy`, throttle rules, and policy rules in DI during host bootstrap.
7. **Launch services.** Start `CesConsumer`, `Neo4jSync`, admin UI, and any worker processes.  Confirm they can connect to SQL Server and Neo4j.

## Operational Runbooks

### Database Migrations

- Migrations are the single source of truth.  NEVER apply ad-hoc SQL unless it is captured in a migration.
- For multi-tenant rollouts, run migrations in maintenance windows or use blue/green databases.
- After migrations, run smoke checks:

   ```powershell
   dotnet run --project tools/SchemaSmokeTest
   ```

### Service Broker Health

- Inspect the transmission queue when troubleshooting:

   ```sql
   SELECT * FROM sys.transmission_queue;
   ```

- `ServiceBrokerMessagePump` moves poison messages into `MessageDeadLetters`.  Monitor this table and alert when counts spike.
- Ensure conversations are ended properly; look for long-running conversations using `sys.conversation_endpoints`.

### Neo4j Sync

- The worker logs every handler execution.  Use the job scheduler to restart the worker on failure.
- If the graph falls behind, run `graph.usp_SyncAtomGraphFromRelations` to repair SQL graph tables, then replay Service Broker messages.

### Billing Ledger

- Ledger entries are written synchronously.  Monitor `BillingUsageLedger` size and archive older entries to cold storage if required.
- Export ledger snapshots to finance systems (e.g., via SQL Agent job or Azure Data Factory).

### Observability Checklist

| Signal | Source | Action |
| --- | --- | --- |
| Application logs | Serilog / structured logging | Centralise in Application Insights, Splunk, etc. |
| Activity traces | OpenTelemetry | Build dashboards for billing throughput, queue latency, and handler failures. |
| SQL metrics | DMV queries | Watch for queue backlogs, lock contention, and disk growth. |
| Neo4j metrics | Neo4j browser / APOC | Track transaction times and heap usage. |

## Configuration Reference

- `ConnectionStrings:HartonomousDb`: SQL Server connection.
- `Neo4j:Uri`, `Neo4j:User`, `Neo4j:Password`: Graph database access.
- `MessageBroker`: Initiator/target service names, queue name, contract, message type, conversation lifetime.
- `ServiceBrokerResilience`: Retry counts, jitter, circuit breaker thresholds, poison message attempts.
- `Billing`: Default rate, per-operation rates, multipliers.
- `Security`: Rate limit rules, banned tenants/principals.

## Backups & Disaster Recovery

- **SQL Server:** Full + differential backups, plus transaction log shipping.  Ensure Service Broker is re-enabled after restores.
- **Neo4j:** Use online backups or cluster replication.  When restoring, run a catch-up job to reapply recent ledger events if necessary.
- **Configuration:** Store appsettings secrets in Azure Key Vault or similar; back them up separately.

## Incident Response

1. Acknowledge alert and gather context (logs, metrics, number of affected tenants).
2. If Service Broker is stalled, inspect `MessageDeadLetters` and clear poison messages after root cause is understood.
3. If billing ledger writes fail, stop the worker to avoid data divergence and backfill from queued messages once resolved.
4. For data corruption, restore from latest backups and replay necessary Service Broker conversations.

Keep this runbook concise.  When incidents occur, append lightweight postmortems or link to external docs rather than bloating this file.
