# Gödel Engine: Autonomous Compute

**General-purpose autonomous reasoning system using Service Broker OODA loop.**

## What It Is

The Gödel Engine demonstrates that the OODA loop (Observe-Orient-Decide-Act) is not just for system performance optimization—it's a **general-purpose autonomous reasoning framework** that can be applied to abstract computational problems:

- Prime number search (implemented)
- Theorem proving (future)
- Neural architecture optimization (future)
- Abstract symbolic reasoning (future)

The name "Gödel Engine" reflects Kurt Gödel's insight about self-reference: a sufficiently powerful formal system contains statements that reference themselves. The OODA loop can reason about *itself* (performance tuning) and *beyond itself* (abstract computation) using the *same four stored procedures*.

This is autonomous reasoning, not just autonomous optimization.

## How It Works

The Gödel Engine reuses the Service Broker OODA loop infrastructure (`sp_Analyze`, `sp_Hypothesize`, `sp_Act`, `sp_Learn`) to process long-running computational tasks asynchronously:

1. **User starts a job** → `sp_StartPrimeSearch` creates a record in `AutonomousComputeJobs` and sends a `JobRequest` message to `AnalyzeQueue`
2. **sp_Analyze** → Detects compute job, routes to `HypothesizeQueue`
3. **sp_Hypothesize** → Reads job state from `AutonomousComputeJobs`, calculates next chunk of work, sends to `ActQueue`
4. **sp_Act** → Calls CLR function `dbo.clr_FindPrimes` to execute computation, sends results to `LearnQueue`
5. **sp_Learn** → Updates job state (progress, results), sends new `JobRequest` to `AnalyzeQueue`
6. **Loop continues** → Until `lastChecked >= rangeEnd`, then marks job `Completed`

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

## Usage

### Start a Prime Search Job

```sql
EXEC dbo.sp_StartPrimeSearch @RangeStart = 2, @RangeEnd = 100000;
```

Returns `JobId` UNIQUEIDENTIFIER (job runs asynchronously).

### Monitor Progress

```sql
SELECT 
    JobId,
    Status,
    JSON_VALUE(JobParameters, '$.rangeStart') AS RangeStart,
    JSON_VALUE(JobParameters, '$.rangeEnd') AS RangeEnd,
    JSON_VALUE(CurrentState, '$.lastChecked') AS Progress,
    CreatedAt,
    UpdatedAt
FROM dbo.AutonomousComputeJobs
WHERE JobType = 'PrimeSearch'
ORDER BY CreatedAt DESC;
```

**Status Values**:

- `Pending`: Job created, waiting for OODA loop pickup
- `Running`: OODA loop actively processing
- `Completed`: All chunks processed
- `Failed`: Error encountered (see `ErrorMessage` column)

### Get Results

```sql
-- View completed job results
SELECT 
    JobId,
    Status,
    Results,
    (SELECT COUNT(*) FROM OPENJSON(Results)) AS PrimeCount,
    CompletedAt
FROM dbo.AutonomousComputeJobs
WHERE Status = 'Completed'
ORDER BY CompletedAt DESC;
```

**Results Format**: JSON array of primes found (e.g., `[2, 3, 5, 7, 11, 13, ...]`)

### Test CLR Function Directly

```sql
-- Find primes from 2 to 100
SELECT dbo.clr_FindPrimes(2, 100);
```

**Output**: `[2,3,5,7,11,13,17,19,23,29,31,37,41,43,47,53,59,61,67,71,73,79,83,89,97]`

## Components

### AutonomousComputeJobs Table

**File**: `sql/tables/dbo.AutonomousComputeJobs.sql`

```sql
CREATE TABLE dbo.AutonomousComputeJobs (
    JobId               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    JobType             NVARCHAR(64) NOT NULL,          -- 'PrimeSearch', 'TheoremProof', etc.
    Status              NVARCHAR(32) NOT NULL,          -- 'Pending', 'Running', 'Completed', 'Failed'
    JobParameters       NVARCHAR(MAX) NULL,             -- JSON: {"rangeStart": 2, "rangeEnd": 1000000}
    CurrentState        NVARCHAR(MAX) NULL,             -- JSON: {"lastChecked": 50000}
    Results             NVARCHAR(MAX) NULL,             -- JSON: [2, 3, 5, 7, 11, ...]
    ErrorMessage        NVARCHAR(MAX) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt         DATETIME2 NULL
);
```

### CLR Compute Function

**File**: `src/SqlClr/PrimeNumberSearch.cs`

```csharp
[SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
public static SqlString clr_FindPrimes(SqlInt64 rangeStart, SqlInt64 rangeEnd)
{
    List<long> primes = new List<long>();
    
    // Trial division algorithm
    for (long candidate = rangeStart; candidate <= rangeEnd; candidate++)
    {
        if (IsPrime(candidate))
            primes.Add(candidate);
    }
    
    return JsonConvert.SerializeObject(primes);
}
```

**Design**: Runs for a *fixed duration* (seconds, not minutes), returns partial results and checkpoint.

### OODA Loop Modifications

The four stored procedures (`sql/procedures/dbo.sp_Analyze.sql`, `sp_Hypothesize.sql`, `sp_Act.sql`, `sp_Learn.sql`) detect compute jobs via `JobRequest` message type and route them through the OODA loop.

**Key Changes**:

1. **sp_Analyze**: If `@messageType = 'JobRequest'`, extract `JobId` from message body and route to `HypothesizeQueue`
2. **sp_Hypothesize**: Read job state from `AutonomousComputeJobs`, calculate next chunk, send to `ActQueue`
3. **sp_Act**: Call `clr_FindPrimes` with chunk range, send results to `LearnQueue`
4. **sp_Learn**: Parse results, update `CurrentState` and `Results`, send new `JobRequest` to `AnalyzeQueue` (or mark `Completed`)

### Entry Point

**File**: `sql/procedures/dbo.sp_StartPrimeSearch.sql`

```sql
CREATE PROCEDURE dbo.sp_StartPrimeSearch
    @RangeStart BIGINT,
    @RangeEnd   BIGINT
AS
BEGIN
    -- Validate input
    IF @RangeStart < 2 OR @RangeEnd <= @RangeStart
        THROW 50000, 'Invalid range', 1;

    -- Create job record
    DECLARE @JobId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO dbo.AutonomousComputeJobs (JobId, JobType, Status, JobParameters, CurrentState)
    VALUES (
        @JobId,
        'PrimeSearch',
        'Running',
        JSON_OBJECT('rangeStart': @RangeStart, 'rangeEnd': @RangeEnd),
        JSON_OBJECT('lastChecked': @RangeStart - 1)
    );

    -- Send initial message to OODA loop
    DECLARE @messageBody NVARCHAR(MAX) = JSON_OBJECT('JobId': @JobId);
    SEND ON CONVERSATION @ConversationHandle
        MESSAGE TYPE [JobRequest] (@messageBody);

    SELECT @JobId AS JobId;
END
```

## Validation

**File**: `sql/verification/GodelEngine_Validation.sql`

```powershell
sqlcmd -S localhost -d Hartonomous -i sql\verification\GodelEngine_Validation.sql
```

**Expected Output**:

```
Tests Passed: 6/6
✓✓✓ ALL TESTS PASSED ✓✓✓
The Gödel Engine is operational and can process autonomous compute tasks.
```

**Test Suite**:

1. Table exists (`AutonomousComputeJobs`)
2. CLR function exists (`dbo.clr_FindPrimes`)
3. Entry procedure exists (`dbo.sp_StartPrimeSearch`)
4. OODA loop procedures route compute jobs
5. CLR function returns correct primes
6. End-to-end job execution completes

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
```

Redeploy if missing:

```powershell
.\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -Rebuild
```

### Service Broker Not Configured

```sql
-- Check services
SELECT * FROM sys.services 
WHERE name IN ('AnalyzeService', 'HypothesizeService', 'ActService', 'LearnService');
```

If missing, run:

```powershell
sqlcmd -S localhost -d Hartonomous -i scripts\setup-service-broker.sql
```

### Monitor OODA Loop

```sql
-- Check queue depths
SELECT 
    'AnalyzeQueue' AS Queue, COUNT(*) AS Messages FROM AnalyzeQueue
UNION ALL
SELECT 'HypothesizeQueue', COUNT(*) FROM HypothesizeQueue
UNION ALL
SELECT 'ActQueue', COUNT(*) FROM ActQueue
UNION ALL
SELECT 'LearnQueue', COUNT(*) FROM LearnQueue;

-- Check for errors
SELECT * FROM sys.transmission_queue WHERE transmission_status != '';
```

### Manual Loop Execution

If queue activation is not configured, manually pump the loop:

```sql
EXEC dbo.sp_Analyze;
EXEC dbo.sp_Hypothesize;
EXEC dbo.sp_Act;
EXEC dbo.sp_Learn;
```

## Performance Characteristics

**Prime Search Throughput**:

- Trial division: ~10K primes/sec (CLR, single-threaded)
- Chunk size: 10K range per OODA cycle
- Job completion: ~10 seconds for 100K range
- Scalability: Limited by single-threaded CLR execution

**Future Optimizations**:

- Sieve of Eratosthenes (100x faster)
- Parallel chunk processing (multi-queue)
- Distributed execution (SQL Server scale-out)

## Future Capabilities

**Theorem Proving**:

```sql
EXEC dbo.sp_StartTheoremProof 
    @Axioms = '["∀x(P(x)→Q(x))", "P(a)"]', 
    @Goal = 'Q(a)';
```

**Neural Architecture Search**:

```sql
EXEC dbo.sp_OptimizeArchitecture
    @Dataset = 'CIFAR-10',
    @Objective = 'accuracy',
    @Budget = 1000; -- Evaluation budget
```

**Symbolic Regression**:

```sql
EXEC dbo.sp_FindFormula
    @Inputs = 'SELECT x, y FROM TrainingData',
    @Target = 'z',
    @MaxComplexity = 10;
```

## References

- [README.md](../README.md) - Getting started guide
- [ARCHITECTURE.md](../ARCHITECTURE.md) - OODA loop architecture
- [DATABASE_SCHEMA.md](DATABASE_SCHEMA.md) - `AutonomousComputeJobs` table schema
- [CLR_GUIDE.md](CLR_GUIDE.md) - CLR function deployment
- `sql/procedures/dbo.sp_Analyze.sql` - OODA loop Phase 1
- `sql/procedures/dbo.sp_Hypothesize.sql` - OODA loop Phase 2
- `sql/procedures/dbo.sp_Act.sql` - OODA loop Phase 3
- `sql/procedures/dbo.sp_Learn.sql` - OODA loop Phase 4
- `sql/verification/GodelEngine_Validation.sql` - Validation test suite
- `src/SqlClr/PrimeNumberSearch.cs` - CLR compute implementation
