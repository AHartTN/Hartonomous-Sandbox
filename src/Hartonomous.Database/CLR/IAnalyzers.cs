using System.Collections.Generic;

namespace Hartonomous.Clr.Analysis
{
    // Data Transfer Objects (DTOs) for analysis results

    public class SlowQueryInfo
    {
        public long QueryId { get; set; }
        public string QueryText { get; set; } = null!;
        public double AvgDurationMs { get; set; }
        public long ExecutionCount { get; set; }
        public double TotalDurationMs { get; set; }
    }

    public class FailedTestInfo
    {
        public string TestSuite { get; set; } = null!;
        public string TestName { get; set; } = null!;
        public int FailureCount { get; set; }
        public string LastError { get; set; } = null!;
    }

    public class CostHotspotInfo
    {
        public string TenantId { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public decimal TotalCost { get; set; }
        public long RequestCount { get; set; }
        public decimal AvgCost { get; set; }
    }

    public class ComprehensiveAnalysisResult
    {
        public string AnalysisType => "comprehensive_analysis";
        public List<SlowQueryInfo> SlowQueries { get; set; } = null!;
        public List<FailedTestInfo> FailedTests { get; set; } = null!;
        public List<CostHotspotInfo> CostHotspots { get; set; } = null!;
        public string? TargetArea { get; set; }
        public System.DateTime AnalyzedAt { get; set; }
    }

    // Interfaces for Analyzers (Interface Segregation Principle)

    /// <summary>
    /// Defines a contract for analyzing query performance from the Query Store.
    /// </summary>
    public interface IQueryPerformanceAnalyzer
    {
        List<SlowQueryInfo> GetSlowQueries(int topN = 10, int hours = 24);
    }

    /// <summary>
    /// Defines a contract for analyzing test failure patterns.
    /// </summary>
    public interface ITestFailureAnalyzer
    {
        List<FailedTestInfo> GetFailedTests(int topN = 10, int days = 7);
    }

    /// <summary>
    /// Defines a contract for analyzing cost hotspots from the billing ledger.
    /// </summary>
    public interface ICostHotspotAnalyzer
    {
        List<CostHotspotInfo> GetCostHotspots(int topN = 10, int days = 7);
    }
}
