using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CesConsumer
{
    public class CdcListener
    {
        private readonly string _connectionString;

        public CdcListener(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                // This is a simplified implementation. A real-world implementation would
                // need to handle the LSN (Log Sequence Number) to keep track of the last
                // change that was processed.
                var command = new SqlCommand("SELECT * FROM cdc.dbo_Models_CT", connection);

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            // Process the change event
                            Console.WriteLine("Change detected in dbo.Models table:");
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.WriteLine($"  {reader.GetName(i)}: {reader.GetValue(i)}");
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }
    }
}
