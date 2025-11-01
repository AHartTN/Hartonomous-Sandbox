# Router Log Pipeline

This document captures the router log ingestion pipeline as of 2025-11-01. It covers the router-side configuration, Windows automation, Azure targets, and current resilience posture.

## Router Configuration

- **Platform**: OpenWrt (ramips/mt7621) with AdGuard Home 0.107.57 installed.
- **Encrypted DNS**: AdGuard upstreams set to Quad9 and Cloudflare DoT with DNSSEC enabled (`/etc/adguardhome.yaml`).
- **Storage**: USB storage mounted at `/mnt/sdb`; cron archive script `/usr/local/bin/archive_router_logs.sh` snaps `logread` nightly (02:30), rolls archives to `/mnt/sdb/routerlogs/archive`, prunes archives older than 30 days, and truncates the active log.
- **SMB Exposure**: `ksmbd` shares `/mnt/sdb/routerlogs` as `\\192.168.1.1\\routerlogs` with `logcollector` credentials.

## Windows Automation

- **Credential Storage**: Router share credential saved via `cmdkey /add:192.168.1.1 /user:logcollector /pass:<password>`.
- **Scripts**:
  - `tools/Upload-RouterLogsToLogAnalytics.ps1`: batches `.log` files, converts to JSON, enforces Azure HTTP Data Collector limits (max 25 MB payload, 5k lines), and retries transient failures with exponential backoff.
  - `tools/Run-RouterLogUpload.ps1`: mounts the share, injects workspace secrets from `HKCU:\\Software\\Hartonomous`, runs the uploader, logs to `C:\\ProgramData\\Hartonomous\\RouterLogUpload.log`, and trims processed files older than 90 days.
- **Scheduling**: Windows Task Scheduler job `Hartonomous-RouterLogUpload` runs nightly at 03:00 using Windows PowerShell (`%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe -File ...`).

## Azure Targets

- **Log Analytics Workspace**: `Development` (Workspace ID `1a599919-38e5-453d-9711-6933cfe31729`). Script expects keys via registry values `RouterLogsWorkspaceId` and `RouterLogsSharedKey` under `HKCU:\\Software\\Hartonomous`.
- **Table**: Data is ingested with `Log-Type` = `RouterSyslog`. Records include `Router`, `FileName`, `Message`, `LoggedAtUtc` fields.
- **Application Insights**: `hartonomous-insights` connection string referenced in `src/ModelIngestion/appsettings.json` for service telemetry.

## Operational Notes

- Run `net use \\192.168.1.1\\routerlogs \\delete` before manual execution to clear stale sessions.
- For manual ingestion tests:

  ```powershell
  net use \\192.168.1.1\\routerlogs
  'Sample log line' | Out-File -LiteralPath \\192.168.1.1\\routerlogs\\archive\\manual-test.log
  powershell.exe -File .\\tools\\Run-RouterLogUpload.ps1
  Get-Content C:\\ProgramData\\Hartonomous\\RouterLogUpload.log -Tail 40
  ```

- The uploader splits oversized batches recursively to respect Azure’s 30 MB limit and retries up to 3 times (2 s start, doubling to max 5 min).
- Processed archives move to `\\\\192.168.1.1\\routerlogs\\processed`; files older than 90 days are removed automatically.

## Next Steps

- Consider persisting per-file state to avoid reprocessing if the script is interrupted after upload but before move.
- Add monitoring in Log Analytics to alert on ingest failures or missing nightly records.
- Validate azure ingestion after next scheduled run and ensure transformation meets query expectations.
