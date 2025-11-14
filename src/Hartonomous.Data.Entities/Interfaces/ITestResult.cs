using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITestResult
{
    long TestResultId { get; set; }
    string TestName { get; set; }
    string TestSuite { get; set; }
    string TestStatus { get; set; }
    double? ExecutionTimeMs { get; set; }
    string? ErrorMessage { get; set; }
    string? StackTrace { get; set; }
    string? TestOutput { get; set; }
    DateTime ExecutedAt { get; set; }
    string? Environment { get; set; }
    string? TestCategory { get; set; }
    double? MemoryUsageMb { get; set; }
    double? CpuUsagePercent { get; set; }
}
