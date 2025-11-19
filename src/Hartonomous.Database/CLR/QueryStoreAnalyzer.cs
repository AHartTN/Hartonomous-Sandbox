using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr.Analysis
{
    /// <summary>
    /// Concrete implementation for analyzing query performance from the SQL Server Query Store.
    /// Adheres to the Single Responsibility Principle.
    /// </summary>
    public class QueryStoreAnalyzer : IQueryPerformanceAnalyzer
    {
        public List<SlowQueryInfo> GetSlowQueries(int topN = 10, int hours = 24)
        {
            var results = new List<SlowQueryInfo>();
            var query = $@"
                SELECT TOP {topN}
                    q.query_id AS QueryId,
                    qt.query_sql_text AS QueryText,
                    rs.avg_duration / 1000.0 AS AvgDurationMs,
                    rs.count_executions AS ExecutionCount,
                    rs.avg_duration * rs.count_executions / 1000.0 AS TotalDurationMs
                FROM sys.query_store_query q
                INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                INNER JOIN sys.query_store_plan p ON q.query_id = p.query_id
                INNER JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
                WHERE rs.last_execution_time >= DATEADD(hour, -{hours}, SYSUTCDATETIME())
                ORDER BY rs.avg_duration * rs.count_executions DESC;
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
                            results.Add(new SlowQueryInfo
                            {
                                QueryId = reader.GetInt64(0),
                                QueryText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                AvgDurationMs = reader.GetDouble(2),
                                ExecutionCount = reader.GetInt64(3),
                                TotalDurationMs = reader.GetDouble(4)
                            });
                        }
                    }
                }
            }
            return results;
        }
    }
}

