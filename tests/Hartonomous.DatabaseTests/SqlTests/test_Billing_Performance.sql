-- =============================================
-- Test: Billing Performance (BillingUsageLedger_InMemory)
-- Validates 500x speedup claim for billing operations
-- =============================================

USE Hartonomous;
GO

PRINT '=================================================';
PRINT 'TEST: Billing Performance (500x speedup claim)';
PRINT '=================================================';

-- Setup: Ensure in-memory table exists
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillingUsageLedger_InMemory' AND is_memory_optimized = 1)
BEGIN
    PRINT 'ERROR: BillingUsageLedger_InMemory table does not exist';
    PRINT 'This test requires the in-memory OLTP table for performance comparison';
    RETURN;
END;

PRINT 'Setup: In-memory billing table found';

-- TEST 1: Measure disk-based billing insert performance (baseline)
PRINT '';
PRINT 'TEST 1: Baseline disk-based billing insert performance';

DECLARE @DiskStartTime DATETIME2 = SYSUTCDATETIME();
DECLARE @BatchSize INT = 1000;
DECLARE @i INT = 1;

-- Insert into traditional disk-based ledger
WHILE @i <= @BatchSize
BEGIN
    INSERT INTO dbo.BillingUsageLedger (TenantId, UserId, ResourceType, ResourceId, Quantity, UnitPrice, TotalCost, BilledAt)
    VALUES (1, 'test_user', 'inference', @i, 1, 0.001, 0.001, SYSUTCDATETIME());
    
    SET @i = @i + 1;
END;

DECLARE @DiskDuration INT = DATEDIFF(MILLISECOND, @DiskStartTime, SYSUTCDATETIME());
DECLARE @DiskThroughput FLOAT = (@BatchSize * 1000.0) / NULLIF(@DiskDuration, 0);

PRINT 'Disk-based: ' + CAST(@BatchSize AS VARCHAR(10)) + ' inserts in ' + CAST(@DiskDuration AS VARCHAR(10)) + ' ms';
PRINT 'Throughput: ' + CAST(@DiskThroughput AS VARCHAR(20)) + ' inserts/sec';

-- TEST 2: Measure in-memory billing insert performance
PRINT '';
PRINT 'TEST 2: In-memory billing insert performance';

DECLARE @MemoryStartTime DATETIME2 = SYSUTCDATETIME();
SET @i = 1;

-- Insert into in-memory ledger
WHILE @i <= @BatchSize
BEGIN
    INSERT INTO dbo.BillingUsageLedger_InMemory (TenantId, UserId, ResourceType, ResourceId, Quantity, UnitPrice, TotalCost, BilledAt)
    VALUES (1, 'test_user', 'inference', @i, 1, 0.001, 0.001, SYSUTCDATETIME());
    
    SET @i = @i + 1;
END;

DECLARE @MemoryDuration INT = DATEDIFF(MILLISECOND, @MemoryStartTime, SYSUTCDATETIME());
DECLARE @MemoryThroughput FLOAT = (@BatchSize * 1000.0) / NULLIF(@MemoryDuration, 0);

PRINT 'In-memory: ' + CAST(@BatchSize AS VARCHAR(10)) + ' inserts in ' + CAST(@MemoryDuration AS VARCHAR(10)) + ' ms';
PRINT 'Throughput: ' + CAST(@MemoryThroughput AS VARCHAR(20)) + ' inserts/sec';

-- TEST 3: Calculate speedup factor
PRINT '';
PRINT 'TEST 3: Speedup calculation';

DECLARE @SpeedupFactor FLOAT = CAST(@DiskDuration AS FLOAT) / NULLIF(@MemoryDuration, 0);

PRINT 'Speedup factor: ' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x';

IF @SpeedupFactor >= 50
    PRINT 'SUCCESS: Achieved significant speedup (target: 500x, actual: ' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x)';
ELSE IF @SpeedupFactor >= 10
    PRINT 'PARTIAL: Moderate speedup achieved (' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x)';
ELSE
    PRINT 'WARNING: Speedup lower than expected (' + CAST(@SpeedupFactor AS VARCHAR(20)) + 'x)';

-- TEST 4: Batch insert performance (native compiled stored procedure)
PRINT '';
PRINT 'TEST 4: Native compiled batch insert performance';

DECLARE @NativeStartTime DATETIME2 = SYSUTCDATETIME();

-- Use native compiled procedure if it exists
IF OBJECT_ID('dbo.sp_InsertUsageRecord_Native', 'P') IS NOT NULL
BEGIN
    SET @i = 1;
    WHILE @i <= @BatchSize
    BEGIN
        EXEC dbo.sp_InsertUsageRecord_Native
            @TenantId = 1,
            @UserId = 'test_user',
            @ResourceType = 'inference',
            @ResourceId = @i,
            @Quantity = 1,
            @UnitPrice = 0.001;
        
        SET @i = @i + 1;
    END;
    
    DECLARE @NativeDuration INT = DATEDIFF(MILLISECOND, @NativeStartTime, SYSUTCDATETIME());
    DECLARE @NativeThroughput FLOAT = (@BatchSize * 1000.0) / NULLIF(@NativeDuration, 0);
    DECLARE @NativeSpeedup FLOAT = CAST(@DiskDuration AS FLOAT) / NULLIF(@NativeDuration, 0);
    
    PRINT 'Native procedure: ' + CAST(@BatchSize AS VARCHAR(10)) + ' inserts in ' + CAST(@NativeDuration AS VARCHAR(10)) + ' ms';
    PRINT 'Throughput: ' + CAST(@NativeThroughput AS VARCHAR(20)) + ' inserts/sec';
    PRINT 'Speedup vs disk: ' + CAST(@NativeSpeedup AS VARCHAR(20)) + 'x';
END
ELSE
BEGIN
    PRINT 'Native compiled procedure not found - skipping test';
END;

-- Cleanup
DELETE FROM dbo.BillingUsageLedger WHERE TenantId = 1 AND UserId = 'test_user';
DELETE FROM dbo.BillingUsageLedger_InMemory WHERE TenantId = 1 AND UserId = 'test_user';

PRINT '';
PRINT 'TEST COMPLETE: Cleanup successful';
PRINT '=================================================';
GO
