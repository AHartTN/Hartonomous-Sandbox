using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr.Analysis
{
    /// <summary>
    /// Concrete implementation for analyzing cost hotspots from the dbo.BillingUsageLedger table.
    /// Adheres to the Single Responsibility Principle.
    /// </summary>
    public class BillingLedgerAnalyzer : ICostHotspotAnalyzer
    {
        public List<CostHotspotInfo> GetCostHotspots(int topN = 10, int days = 7)
        {
            var results = new List<CostHotspotInfo>();
            var query = $@"
                SELECT TOP {topN}
                    TenantId,
                    Operation,
                    SUM(TotalCost) AS TotalCost,
                    COUNT(*) AS RequestCount,
                    AVG(TotalCost) AS AvgCost
                FROM dbo.BillingUsageLedger
                WHERE TimestampUtc >= DATEADD(day, -{days}, SYSUTCDATETIME())
                GROUP BY TenantId, Operation
                ORDER BY SUM(TotalCost) DESC;
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
                            results.Add(new CostHotspotInfo
                            {
                                TenantId = reader.GetString(0),
                                Operation = reader.GetString(1),
                                TotalCost = reader.GetDecimal(2),
                                RequestCount = reader.GetInt64(3),
                                AvgCost = reader.GetDecimal(4)
                            });
                        }
                    }
                }
            }
            return results;
        }
    }
}
