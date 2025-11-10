# Gödel Engine Quick Reference

## Start a Job

```sql
EXEC dbo.sp_StartPrimeSearch @RangeStart = 2, @RangeEnd = 100000;
```

## Check Status

```sql
-- All jobs
SELECT JobId, JobType, Status, 
       JSON_VALUE(CurrentState, '$.lastChecked') AS Progress,
       CreatedAt
FROM dbo.AutonomousComputeJobs
ORDER BY CreatedAt DESC;

-- Specific job
SELECT * FROM dbo.AutonomousComputeJobs WHERE JobId = '<your-guid>';
```

## Get Results

```sql
-- View results
SELECT JobId, Status, Results 
FROM dbo.AutonomousComputeJobs 
WHERE Status = 'Completed';

-- Count primes found
SELECT JobId, 
       (SELECT COUNT(*) FROM OPENJSON(Results)) AS PrimeCount
FROM dbo.AutonomousComputeJobs 
WHERE Status = 'Completed';
```

## Monitor OODA Loop

```sql
-- Check Service Broker queues
SELECT 
    'AnalyzeQueue' AS Queue, 
    COUNT(*) AS Messages 
FROM AnalyzeQueue
UNION ALL
SELECT 'HypothesizeQueue', COUNT(*) FROM HypothesizeQueue
UNION ALL
SELECT 'ActQueue', COUNT(*) FROM ActQueue
UNION ALL
SELECT 'LearnQueue', COUNT(*) FROM LearnQueue;

-- Check for errors
SELECT * FROM sys.transmission_queue WHERE transmission_status != '';
```

## Manual Loop Execution (if not auto-activating)

```sql
-- Process one cycle manually
EXEC dbo.sp_Analyze;
EXEC dbo.sp_Hypothesize;
EXEC dbo.sp_Act;
EXEC dbo.sp_Learn;
```

## Test CLR Function Directly

```sql
-- Find primes from 2 to 100
SELECT dbo.clr_FindPrimes(2, 100);
```

## Clean Up Test Jobs

```sql
-- Delete completed test jobs
DELETE FROM dbo.AutonomousComputeJobs 
WHERE JobType = 'PrimeSearch' 
  AND Status = 'Completed' 
  AND DATEDIFF(HOUR, CompletedAt, GETUTCDATE()) > 24;
```

## Troubleshooting

### Job Stuck in 'Running'

```sql
-- Check Service Broker status
SELECT name, is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';

-- Enable if needed
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Check queue status
SELECT name, is_receive_enabled, is_activation_enabled 
FROM sys.service_queues 
WHERE name LIKE '%Queue';
```

### No CLR Function

```sql
-- Check if CLR is enabled
EXEC sp_configure 'clr enabled';
-- If 0, enable it:
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Check assembly
SELECT * FROM sys.assemblies WHERE name LIKE '%SqlClr%';

-- Redeploy if missing
-- .\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -Rebuild
```

### Service Broker Not Configured

```sql
-- Check services
SELECT * FROM sys.services 
WHERE name IN ('AnalyzeService', 'HypothesizeService', 'ActService', 'LearnService');

-- If missing, run setup script
-- sqlcmd -S localhost -d Hartonomous -i scripts\setup-service-broker.sql
```

## Architecture Cheat Sheet

```
┌─────────────────────────────────────────────────────────────┐
│                    Gödel Engine Flow                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  User: sp_StartPrimeSearch                                   │
│    ↓ (creates job in AutonomousComputeJobs)                  │
│    ↓ (sends JobRequest → AnalyzeQueue)                       │
│                                                              │
│  sp_Analyze (Phase 1: Observe)                               │
│    ↓ (detects compute job)                                   │
│    ↓ (routes to HypothesizeQueue)                            │
│                                                              │
│  sp_Hypothesize (Phase 2: Orient & Plan)                     │
│    ↓ (reads job state from table)                            │
│    ↓ (calculates next chunk [start, end])                    │
│    ↓ (sends to ActQueue)                                     │
│                                                              │
│  sp_Act (Phase 3: Execute)                                   │
│    ↓ (calls clr_FindPrimes CLR function)                     │
│    ↓ (sends results to LearnQueue)                           │
│                                                              │
│  sp_Learn (Phase 4: Loop)                                    │
│    ↓ (updates job state: lastChecked, results)               │
│    ↓ (sends new JobRequest → AnalyzeQueue)                   │
│    ↓                                                         │
│  [LOOP CONTINUES UNTIL COMPLETE]                             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Key Tables

| Table | Purpose |
|-------|---------|
| `AutonomousComputeJobs` | Job tracking (state, parameters, results) |
| `AnalyzeQueue` | Service Broker queue for Analyze phase |
| `HypothesizeQueue` | Service Broker queue for Hypothesize phase |
| `ActQueue` | Service Broker queue for Act phase |
| `LearnQueue` | Service Broker queue for Learn phase |

## Key Procedures

| Procedure | Phase | Purpose |
|-----------|-------|---------|
| `sp_StartPrimeSearch` | Entry | Create job, send initial message |
| `sp_Analyze` | 1 | Detect and route compute jobs |
| `sp_Hypothesize` | 2 | Plan next chunk of work |
| `sp_Act` | 3 | Execute CLR compute function |
| `sp_Learn` | 4 | Update state, continue loop |

## Key Files

| File | Description |
|------|-------------|
| `docs/GODEL_ENGINE_IMPLEMENTATION.md` | Full implementation guide |
| `docs/GODEL_ENGINE_CHANGES.md` | Summary of all changes |
| `sql/verification/GodelEngine_Validation.sql` | Validation test suite |
| `sql/tables/dbo.AutonomousComputeJobs.sql` | Job tracking table |
| `src/SqlClr/PrimeNumberSearch.cs` | CLR compute function |

## Validation

```powershell
# Run full test suite
sqlcmd -S localhost -d Hartonomous -i sql\verification\GodelEngine_Validation.sql
```

Expected: `Tests Passed: 6/6`

---

**For detailed documentation, see**: [docs/GODEL_ENGINE_IMPLEMENTATION.md](GODEL_ENGINE_IMPLEMENTATION.md)
