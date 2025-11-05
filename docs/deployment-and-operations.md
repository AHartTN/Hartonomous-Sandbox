# Deployment & Operations

Hartonomous environment provisioning and operational procedures.

## Environment Requirements

| Component | Notes |
| --- | --- |
| SQL Server 2025 | Enable Service Broker, CDC, vector/spatial types. Database name: `Hartonomous` |
| Neo4j 5.x | Single instance or clustered. Configure Bolt URI + credentials in application settings |
| Application Hosts | .NET 10 runtime, PowerShell 7. Deploy as Windows services, containers, or Azure App Service |
| Messaging | SQL Service Broker (internal) |

## Provisioning Steps

1. **Create the database.**

   ```powershell
   ./scripts/deploy-database.ps1 -ServerName "sql-prod" -DatabaseName "Hartonomous" -TrustedConnection $false -SqlUser "svc_hartonomous" -SqlPassword "<secret>"
   ```

2. **Enable Service Broker.**

   ```sql
   ALTER DATABASE [Hartonomous] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
   ```

3. **Apply migrations.** Current migrations: `InitialBaseline`, `RemoveLegacyEmbeddingsProduction`.

   ```powershell
   dotnet ef database update --project src/Hartonomous.Data --startup-project src/Hartonomous.Infrastructure --context HartonomousDbContext
   ```

4. **Publish SQL CLR artifacts (optional).**

   ```powershell
   dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release
   sqlcmd -S . -d master -Q "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
   sqlcmd -S . -d Hartonomous -i sql/types/provenance.AtomicStream.sql
   sqlcmd -S . -d Hartonomous -i sql/procedures/Common.ClrBindings.sql
   sqlcmd -S . -d Hartonomous -i sql/procedures/provenance.AtomicStreamSegments.sql
   ```

5. **Seed reference data.** Deploy script seeds default `publisher_core` rate plan and sample atoms with `-Seed $true`.
6. **Configure applications.** Update `appsettings.Production.json` with connection strings, Service Broker queue name, security options, and billing defaults. Register `IServiceBrokerResilienceStrategy`, throttle rules, and policy rules in DI.
7. **Launch services.** Start `CesConsumer`, `Neo4jSync`, admin UI, and worker processes.

## Operational Runbooks

### Database Migrations

- Migrations are the source of truth for schema changes
- Run migrations in maintenance windows for multi-tenant rollouts
- Validate with smoke tests post-migration:

   ```powershell
   dotnet run --project tools/SchemaSmokeTest
   ```

### Service Broker Health

- Inspect transmission queue:

   ```sql
   SELECT * FROM sys.transmission_queue;
   ```

- `ServiceBrokerMessagePump` routes poison messages to `MessageDeadLetters`. Monitor for spikes.
- Check for long-running conversations: `sys.conversation_endpoints`

### Neo4j Sync

- Worker logs all handler executions
- Repair SQL graph tables: `graph.usp_SyncAtomGraphFromRelations`
- Replay Service Broker messages if graph falls behind

### Billing Ledger

- Ledger entries written synchronously
- Monitor `BillingUsageLedger` size and archive to cold storage as needed
- Export ledger snapshots to finance systems (SQL Agent job or Azure Data Factory)

### Observability Checklist

| Signal | Source | Action |
| --- | --- | --- |
| Application logs | Serilog / structured logging | Centralize in Application Insights, Splunk, etc. |
| Activity traces | OpenTelemetry | Dashboard: billing throughput, queue latency, handler failures |
| SQL metrics | DMV queries | Monitor queue backlogs, lock contention, disk growth |
| Neo4j metrics | Neo4j browser / APOC | Track transaction times, heap usage |

## Configuration Reference

- `ConnectionStrings:HartonomousDb`: SQL Server connection.
- `Neo4j:Uri`, `Neo4j:User`, `Neo4j:Password`: Graph database access.
- `MessageBroker`: Initiator/target service names, queue name, contract, message type, conversation lifetime (SQL Service Broker).
- `ServiceBrokerResilience`: Retry counts, jitter, circuit breaker thresholds, poison message attempts.
- `Billing`: Default plan code/name, monthly fee, unit price per DCU, bundled storage/seat entitlements, operation rates/categories/units, multiplier catalogs (modality, complexity, content type, grounding, guarantee, provenance)
- `Security`: Rate limit rules, banned tenants/principals

## Backups & Disaster Recovery

- **SQL Server**: Full + differential backups, transaction log shipping. Re-enable Service Broker after restores.
- **Neo4j**: Online backups or cluster replication. Run catch-up job to reapply recent ledger events after restore.
- **Configuration**: Store secrets in Azure Key Vault or similar; backup separately.

## Incident Response

1. Acknowledge alert, gather context (logs, metrics, affected tenants)
2. Service Broker stalls: Inspect `MessageDeadLetters`, clear poison messages after root cause analysis
3. Billing ledger write failures: Stop worker to avoid data divergence, backfill from queued messages
4. Data corruption: Restore from backups, replay Service Broker conversations
