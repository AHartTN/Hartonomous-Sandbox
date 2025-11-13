using System;

namespace SqlClrFunctions.Analysis
{
    /// <summary>
    /// Orchestrates a comprehensive analysis of the system by composing multiple,
    /// single-responsibility analyzer components. This demonstrates the Dependency Inversion
    /// and Single Responsibility principles.
    /// </summary>
    public class SystemAnalyzer
    {
        private readonly IQueryPerformanceAnalyzer _queryAnalyzer;
        private readonly ITestFailureAnalyzer _testAnalyzer;
        private readonly ICostHotspotAnalyzer _costAnalyzer;

        public SystemAnalyzer(
            IQueryPerformanceAnalyzer queryAnalyzer,
            ITestFailureAnalyzer testAnalyzer,
            ICostHotspotAnalyzer costAnalyzer)
        {
            _queryAnalyzer = queryAnalyzer ?? throw new ArgumentNullException(nameof(queryAnalyzer));
            _testAnalyzer = testAnalyzer ?? throw new ArgumentNullException(nameof(testAnalyzer));
            _costAnalyzer = costAnalyzer ?? throw new ArgumentNullException(nameof(costAnalyzer));
        }

        /// <summary>
        /// Performs a comprehensive analysis by delegating to specialized analyzers
        /// and aggregating their results.
        /// </summary>
        /// <param name="targetArea">An optional area to focus the analysis on (for future use).</param>
        /// <returns>A single DTO containing the aggregated analysis results.</returns>
        public ComprehensiveAnalysisResult PerformComprehensiveAnalysis(string targetArea)
        {
            var result = new ComprehensiveAnalysisResult
            {
                SlowQueries = _queryAnalyzer.GetSlowQueries(),
                FailedTests = _testAnalyzer.GetFailedTests(),
                CostHotspots = _costAnalyzer.GetCostHotspots(),
                TargetArea = targetArea,
                AnalyzedAt = DateTime.UtcNow
            };

            return result;
        }
    }
}
