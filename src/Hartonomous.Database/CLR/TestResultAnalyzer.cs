using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr.Analysis
{
    /// <summary>
    /// Concrete implementation for analyzing test failure patterns from the dbo.TestResults table.
    /// Adheres to the Single Responsibility Principle.
    /// </summary>
    public class TestResultAnalyzer : ITestFailureAnalyzer
    {
        public List<FailedTestInfo> GetFailedTests(int topN = 10, int days = 7)
        {
            var results = new List<FailedTestInfo>();
            // This query is designed to be safe even if the TestResults table does not exist.
            var query = $@"
                IF OBJECT_ID('dbo.TestResults', 'U') IS NOT NULL
                BEGIN
                    SELECT TOP {topN}
                        TestSuite,
                        TestName,
                        COUNT(*) AS FailureCount,
                        MAX(ErrorMessage) AS LastError
                    FROM dbo.TestResults
                    WHERE TestOutcome = 'Failed'
                      AND RunCompletedAt >= DATEADD(day, -{days}, SYSUTCDATETIME())
                    GROUP BY TestSuite, TestName
                    ORDER BY COUNT(*) DESC;
                END;
            ";

            using (var connection = new SqlConnection("context connection=true"))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new FailedTestInfo
                            {
                                TestSuite = reader.GetString(0),
                                TestName = reader.GetString(1),
                                FailureCount = reader.GetInt32(2),
                                LastError = reader.IsDBNull(3) ? null : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return results;
        }
    }
}

