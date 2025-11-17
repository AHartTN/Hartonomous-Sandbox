using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class TestResult : ITestResult
{
    public long TestResultId { get; set; }

    public string TestName { get; set; } = null!;

    public string TestSuite { get; set; } = null!;

    public string TestStatus { get; set; } = null!;

    public double? ExecutionTimeMs { get; set; }

    public string? ErrorMessage { get; set; }

    public string? StackTrace { get; set; }

    public string? TestOutput { get; set; }

    public DateTime ExecutedAt { get; set; }

    public string? Environment { get; set; }

    public string? TestCategory { get; set; }

    public double? MemoryUsageMb { get; set; }

    public double? CpuUsagePercent { get; set; }
}
