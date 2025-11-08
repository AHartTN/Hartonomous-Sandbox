using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SqlClrFunctions.JsonProcessing;

namespace SqlClrFunctions.Analysis
{
    /// <summary>
    /// Public static class containing the Table-Valued Function (TVF) for autonomous analysis.
    /// This class is the entry point that will be registered with SQL Server.
    /// </summary>
    public static class AutonomousAnalyticsTVF
    {
        /// <summary>
        /// A CLR Table-Valued Function that performs a comprehensive system analysis and
        /// returns the results as a single JSON document. This function encapsulates
        /// complex logic that is more performant and maintainable in C# than in T-SQL.
        /// </summary>
        /// <param name="targetArea">An optional string to specify a focus area for the analysis.</param>
        /// <returns>An IEnumerable that yields the analysis result.</returns>
        [SqlFunction(
            FillRowMethodName = "FillAnalysisRow",
            TableDefinition = "AnalysisJson nvarchar(max)")]
        public static IEnumerable fn_clr_AnalyzeSystemState(SqlString targetArea)
        {
            // 1. Instantiate the concrete analyzer implementations
            var queryAnalyzer = new QueryStoreAnalyzer();
            var testAnalyzer = new TestResultAnalyzer();
            var costAnalyzer = new BillingLedgerAnalyzer();

            // 2. Compose the main SystemAnalyzer service with the concrete analyzers
            var systemAnalyzer = new SystemAnalyzer(queryAnalyzer, testAnalyzer, costAnalyzer);

            // 3. Perform the analysis
            var analysisResult = systemAnalyzer.PerformComprehensiveAnalysis(targetArea.IsNull ? null : targetArea.Value);

            // 4. Serialize the final result object to JSON
            var jsonSerializer = new JsonSerializerImpl();
            var jsonResult = jsonSerializer.Serialize(analysisResult);

            // 5. Return the result (which will be processed by FillAnalysisRow)
            yield return jsonResult;
        }

        /// <summary>
        /// The callback method required by SQL Server to populate the rows of the TVF result set.
        /// </summary>
        /// <param name="obj">The object yielded from the main function (the JSON string).</param>
        /// <param name="analysisJson">The output column of the TVF.</param>
        public static void FillAnalysisRow(object obj, out SqlString analysisJson)
        {
            analysisJson = new SqlString((string)obj);
        }
    }
}
