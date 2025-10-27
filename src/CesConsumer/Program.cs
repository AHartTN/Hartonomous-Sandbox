using System;
using System.Threading;
using System.Threading.Tasks;

namespace CesConsumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;";
            var listener = new CdcListener(connectionString);

            Console.WriteLine("Starting CES consumer...");
            await listener.StartListeningAsync(new CancellationToken());
        }
    }
}