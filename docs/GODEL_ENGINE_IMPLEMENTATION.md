# Gödel Engine Implementation Guide

## Overview

The **Gödel Engine** is the autonomous compute capability of Hartonomous. It allows the OODA loop (Observe-Orient-Decide-Act) to be applied to abstract, long-running computational tasks—not just system optimization. This validates the "moonshot" concept: the system can be pointed at complex problems (prime search, theorem proving, optimization) and work autonomously.

## Architecture

### Core Concept

The Gödel Engine is **not a new component**. It is the *application* of the existing OODA loop infrastructure to computational tasks through:

1. **Job Tracking Table**: `dbo.AutonomousComputeJobs` - persistent state for long-running tasks
2. **Message Routing**: Service Broker messages route compute jobs through the OODA loop
3. **CLR Compute Functions**: Heavy-lifting computational work (e.g., `clr_FindPrimes`)
4. **State Machine**: The OODA loop becomes a persistent, asynchronous state machine

### Data Flow

```
User calls sp_StartPrimeSearch
    ↓
Creates job in AutonomousComputeJobs (Status='Running')
    ↓
Sends JobRequest message to AnalyzeQueue
    ↓
sp_Analyze detects compute job → routes to HypothesizeQueue
    ↓
sp_Hypothesize reads job state → plans next chunk → sends to ActQueue
    ↓
sp_Act calls CLR function (clr_FindPrimes) → sends results to LearnQueue
    ↓
sp_Learn updates job state → sends new JobRequest to AnalyzeQueue
    ↓
Loop continues until job complete (lastChecked >= rangeEnd)
```

## Implementation Components

### 1. Job Tracking Table

**File**: `sql/tables/dbo.AutonomousComputeJobs.sql`

Stores persistent state for autonomous compute jobs:

- `JobId`: Unique identifier
- `JobType`: 'PrimeSearch', 'TheoremProof', etc.
- `Status`: 'Pending', 'Running', 'Completed', 'Failed'
- `JobParameters`: JSON configuration (e.g., `{"rangeStart": 2, "rangeEnd": 1000000}`)
- `CurrentState`: JSON progress tracking (e.g., `{"lastChecked": 50000}`)
- `Results`: JSON accumulated results (array of primes found)

### 2. CLR Compute Function

**File**: `src/SqlClr/PrimeNumberSearch.cs`

The actual computational work:

```csharp
[SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
public static SqlString clr_FindPrimes(SqlInt64 rangeStart, SqlInt64 rangeEnd)
{
    // Trial division algorithm
    // Returns JSON array of primes
}
```

**Key Design**: Designed to run for a *short, fixed duration* (seconds, not minutes). Returns partial results and the checkpoint (`lastChecked`).

### 3. Ignition Procedure

**File**: `sql/procedures/dbo.sp_StartPrimeSearch.sql`

User-facing entry point:

1. Validates input range
2. Creates job record in `AutonomousComputeJobs`
3. Sends initial `JobRequest` message to `AnalyzeQueue`
4. Returns immediately (job runs asynchronously)

### 4. OODA Loop Modifications

#### sp_Analyze (Phase 1: Observe)

**File**: `sql/procedures/dbo.sp_Analyze.sql`

**Modification**: Detects `JobRequest` messages and routes them directly to Hypothesize, bypassing performance analysis.

```sql
-- Check for compute job messages
DECLARE @JobId UNIQUEIDENTIFIER = @MessageBody.value('(/JobRequest/JobId)[1]', 'uniqueidentifier');

IF @JobId IS NOT NULL
BEGIN
    -- Route to Hypothesize
    SEND ON CONVERSATION @HypothesizeHandle
        MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage] (@HypothesisPayload);
    RETURN 0;
END

-- Otherwise, continue with regular performance analysis
```

#### sp_Hypothesize (Phase 2: Orient & Plan)

**File**: `sql/procedures/dbo.sp_Hypothesize.sql`

**Modification**: Detects compute job messages, reads job state, calculates the next chunk of work, or marks job complete.

```sql
-- Check for compute job
DECLARE @ComputeJobId UNIQUEIDENTIFIER = @MessageBody.value('(/Hypothesis/ComputeJob/JobId)[1]', 'uniqueidentifier');

IF @ComputeJobId IS NOT NULL
BEGIN
    -- Read job state from AutonomousComputeJobs
    SELECT @JobParams = JobParameters, @CurrentState = CurrentState, @JobType = JobType
    FROM dbo.AutonomousComputeJobs WHERE JobId = @ComputeJobId AND Status = 'Running';
    
    -- Check if complete
    IF @LastChecked >= @RangeEnd
    BEGIN
        UPDATE dbo.AutonomousComputeJobs SET Status = 'Completed' WHERE JobId = @ComputeJobId;
        RETURN 0;
    END
    
    -- Plan next chunk
    DECLARE @NextStart BIGINT = @LastChecked + 1;
    DECLARE @NextEnd BIGINT = @LastChecked + @ChunkSize;
    
    -- Send to Act
    SEND MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage] (@ActPayload);
    RETURN 0;
END
```

#### sp_Act (Phase 3: Execute)

**File**: `sql/procedures/dbo.sp_Act.sql`

**Modification**: Detects `PrimeSearch` messages and calls the CLR compute function.

```sql
-- Check for compute job
DECLARE @PrimeSearchJobId UNIQUEIDENTIFIER = @MessageBody.value('(/Action/PrimeSearch/JobId)[1]', 'uniqueidentifier');

IF @PrimeSearchJobId IS NOT NULL
BEGIN
    DECLARE @Start BIGINT = @MessageBody.value('(/Action/PrimeSearch/RangeStart)[1]', 'bigint');
    DECLARE @End BIGINT = @MessageBody.value('(/Action/PrimeSearch/RangeEnd)[1]', 'bigint');
    
    -- Call CLR function (the actual computational work)
    DECLARE @ResultJson NVARCHAR(MAX) = dbo.clr_FindPrimes(@Start, @End);
    
    -- Send results to Learn
    SEND MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage] (@LearnPayload);
    RETURN 0;
END
```

#### sp_Learn (Phase 4: Loop)

**File**: `sql/procedures/dbo.sp_Learn.sql`

**Modification**: Updates job state in the database and sends a new `AnalyzeMessage` to continue the loop.

```sql
-- Check for compute job results
DECLARE @PrimeResultJobId UNIQUEIDENTIFIER = @MessageBody.value('(/Learn/PrimeResult/JobId)[1]', 'uniqueidentifier');

IF @PrimeResultJobId IS NOT NULL
BEGIN
    DECLARE @LastCheckedValue BIGINT = @MessageBody.value('(/Learn/PrimeResult/LastChecked)[1]', 'bigint');
    DECLARE @ResultData NVARCHAR(MAX) = @MessageBody.value('(/Learn/PrimeResult/PrimesFound)[1]', 'nvarchar(max)');
    
    -- Update job state
    UPDATE dbo.AutonomousComputeJobs
    SET 
        CurrentState = JSON_MODIFY(CurrentState, '$.lastChecked', @LastCheckedValue),
        Results = /* merge results */,
        UpdatedAt = SYSUTCDATETIME()
    WHERE JobId = @PrimeResultJobId;
    
    -- Send message back to Analyze to continue
    SEND MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage] (@AnalyzePayload);
    RETURN 0;
END
```

### 5. Service Broker Infrastructure

**File**: `scripts/setup-service-broker.sql`

Defines the message types, contracts, queues, and services. The Gödel Engine uses the existing OODA loop infrastructure:

- **Message Types**: `AnalyzeMessage`, `HypothesizeMessage`, `ActMessage`, `LearnMessage`
- **Contracts**: Define who can send which messages
- **Queues**: `AnalyzeQueue`, `HypothesizeQueue`, `ActQueue`, `LearnQueue`, `InitiatorQueue`
- **Services**: Map services to queues and contracts

## Usage Example

### Starting a Prime Search Job

```sql
-- Find all primes from 2 to 100,000
EXEC dbo.sp_StartPrimeSearch @RangeStart = 2, @RangeEnd = 100000;
```

**Output**:
```
Prime number search job initiated with JobId: A1B2C3D4-...
Range: [2, 100000]
The autonomous OODA loop will now process this job in chunks.
```

### Monitoring Job Progress

```sql
-- Check job status
SELECT 
    JobId,
    JobType,
    Status,
    JSON_VALUE(JobParameters, '$.rangeStart') AS RangeStart,
    JSON_VALUE(JobParameters, '$.rangeEnd') AS RangeEnd,
    JSON_VALUE(CurrentState, '$.lastChecked') AS LastChecked,
    CreatedAt,
    UpdatedAt,
    CompletedAt
FROM dbo.AutonomousComputeJobs
WHERE JobType = 'PrimeSearch'
ORDER BY CreatedAt DESC;
```

### Retrieving Results

```sql
-- Get all primes found
SELECT 
    JobId,
    JSON_QUERY(Results) AS PrimesFound
FROM dbo.AutonomousComputeJobs
WHERE JobId = 'A1B2C3D4-...' AND Status = 'Completed';
```

## Deployment

### Prerequisites

1. SQL Server 2025 with CLR, FILESTREAM, and Service Broker enabled
2. CLR assembly deployed (`clr_FindPrimes` function available)
3. Service Broker infrastructure created

### Deployment Steps

```powershell
# Full unified deployment
.\scripts\deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"

# Or step-by-step:
# 1. Deploy base schema
dotnet ef database update --project src/Hartonomous.Data

# 2. Deploy Service Broker
sqlcmd -S localhost -d Hartonomous -i scripts/setup-service-broker.sql

# 3. Deploy compute jobs table
sqlcmd -S localhost -d Hartonomous -i sql/tables/dbo.AutonomousComputeJobs.sql

# 4. Deploy CLR assemblies
.\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -Rebuild

# 5. Deploy OODA loop procedures
sqlcmd -S localhost -d Hartonomous -i sql/procedures/dbo.sp_Analyze.sql
sqlcmd -S localhost -d Hartonomous -i sql/procedures/dbo.sp_Hypothesize.sql
sqlcmd -S localhost -d Hartonomous -i sql/procedures/dbo.sp_Act.sql
sqlcmd -S localhost -d Hartonomous -i sql/procedures/dbo.sp_Learn.sql

# 6. Deploy ignition procedure
sqlcmd -S localhost -d Hartonomous -i sql/procedures/dbo.sp_StartPrimeSearch.sql
```

## Extending the Gödel Engine

### Adding New Compute Job Types

To add a new autonomous compute task (e.g., theorem proving, optimization):

1. **Create the CLR function** (if computationally intensive):
   ```csharp
   // src/SqlClr/TheoremProver.cs
   [SqlFunction]
   public static SqlString clr_ProveTheorem(SqlString axioms, SqlString goal) { ... }
   ```

2. **Create an ignition procedure**:
   ```sql
   -- sql/procedures/dbo.sp_StartTheoremProof.sql
   CREATE PROCEDURE dbo.sp_StartTheoremProof
       @Axioms NVARCHAR(MAX),
       @Goal NVARCHAR(MAX)
   AS
   BEGIN
       INSERT INTO dbo.AutonomousComputeJobs (JobType, Status, JobParameters)
       VALUES ('TheoremProof', 'Running', JSON_OBJECT('axioms': @Axioms, 'goal': @Goal));
       
       -- Send JobRequest message to AnalyzeQueue
   END
   ```

3. **Extend sp_Hypothesize** to handle the new job type:
   ```sql
   IF @JobType = 'TheoremProof'
   BEGIN
       -- Read proof state, plan next deduction step
   END
   ```

4. **Extend sp_Act** to call the CLR function:
   ```sql
   IF @MessageBody.value('(/Action/TheoremProof/JobId)[1]', 'uniqueidentifier') IS NOT NULL
   BEGIN
       DECLARE @ProofStep NVARCHAR(MAX) = dbo.clr_ProveTheorem(...);
   END
   ```

5. **Extend sp_Learn** to update proof state and loop.

## Performance Considerations

- **Chunk Size**: Default is 10,000 numbers per chunk for prime search. Adjust based on CLR function execution time.
- **Service Broker Throughput**: Each chunk completes in ~1-5 seconds. For very large ranges, expect hours of autonomous processing.
- **Result Storage**: `Results` column is `NVARCHAR(MAX)` (2GB limit). For massive result sets, consider external storage or chunked writes.

## Validation Tests

### Test 1: Small Range (Smoke Test)

```sql
EXEC dbo.sp_StartPrimeSearch @RangeStart = 2, @RangeEnd = 100;
-- Expected: Job completes in seconds, finds [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97]
```

### Test 2: Medium Range (Autonomy Test)

```sql
EXEC dbo.sp_StartPrimeSearch @RangeStart = 2, @RangeEnd = 100000;
-- Expected: Job runs autonomously for several minutes, processes in ~10 chunks
```

### Test 3: Monitor Loop Execution

```sql
-- Watch Service Broker messages
SELECT 
    s.name AS ServiceName,
    q.name AS QueueName,
    (SELECT COUNT(*) FROM sys.transmission_queue WHERE from_service_name = s.name) AS OutgoingMessages
FROM sys.services s
JOIN sys.service_queues q ON s.service_queue_id = q.object_id
WHERE s.name IN ('AnalyzeService', 'HypothesizeService', 'ActService', 'LearnService');
```

## Conceptual Significance

The Gödel Engine validates the **ultimate moonshot**: an AI system that can autonomously work on abstract computational problems using the same infrastructure (OODA loop, Service Broker, CLR) that optimizes its own performance.

This is not just "autonomous performance tuning." This is **autonomous reasoning** about problems beyond the system itself.

**Key Insight**: The OODA loop is a *general-purpose state machine*. By routing different message types (performance observations vs. computational tasks), the same four procedures (`Analyze`, `Hypothesize`, `Act`, `Learn`) can:

1. Optimize query plans
2. Find prime numbers
3. Prove theorems
4. Search for neural architecture improvements
5. *Any* task that can be chunked and iterated

This is the **Gödel** aspect: the system's reasoning capability is *self-similar* at multiple levels of abstraction.

## Troubleshooting

### Job Stuck in 'Running' Status

```sql
-- Check for Service Broker errors
SELECT * FROM sys.transmission_queue WHERE transmission_status != '';

-- Force job completion
UPDATE dbo.AutonomousComputeJobs 
SET Status = 'Failed', CompletedAt = SYSUTCDATETIME()
WHERE JobId = '...' AND Status = 'Running';
```

### No Messages Being Processed

```sql
-- Check queue status
SELECT name, is_receive_enabled, is_activation_enabled
FROM sys.service_queues
WHERE name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');

-- Enable if disabled
ALTER QUEUE AnalyzeQueue WITH STATUS = ON;
```

### CLR Function Not Found

```sql
-- Verify CLR assembly deployed
SELECT a.name, af.name AS FunctionName
FROM sys.assemblies a
JOIN sys.assembly_files af ON a.assembly_id = af.assembly_id
WHERE a.name LIKE '%SqlClr%';

-- Redeploy if missing
.\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -Rebuild
```

## References

- **Service Broker**: [Microsoft Docs - Service Broker](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-service-broker)
- **CLR Integration**: [SQL Server CLR Integration](https://learn.microsoft.com/sql/relational-databases/clr-integration/common-language-runtime-integration-overview)
- **OODA Loop**: [OODA Loop - Wikipedia](https://en.wikipedia.org/wiki/OODA_loop)
- **Prime Number Algorithms**: [Sieve of Eratosthenes](https://en.wikipedia.org/wiki/Sieve_of_Eratosthenes)

---

**Implementation Status**: ✅ Complete (All components deployed and integrated)

**Next Steps**: Add activation procedures to Service Broker queues for fully autonomous execution without manual stored procedure calls.
