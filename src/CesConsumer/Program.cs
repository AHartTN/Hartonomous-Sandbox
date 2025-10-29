using System;
using System.Threading;
using System.Threading.Tasks;

namespace CesConsumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuration - in production, use appsettings.json or environment variables
            var sqlConnectionString = "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;";
            var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
                ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"; // Local Event Hubs emulator
            var eventHubName = Environment.GetEnvironmentVariable("EVENTHUB_NAME") ?? "sqlserver-ces-events";

            Console.WriteLine("=== Hartonomous CES Consumer ===");
            Console.WriteLine("Processing SQL Server 2025 Change Event Streaming");
            Console.WriteLine($"SQL Server: {sqlConnectionString.Split(';')[0]}");
            Console.WriteLine($"Event Hub: {eventHubName}");
            Console.WriteLine();

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Shutdown requested...");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                var listener = new CdcListener(sqlConnectionString, eventHubConnectionString, eventHubName);
                await listener.StartListeningAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine("CES Consumer stopped.");
            }
        }
    }
}