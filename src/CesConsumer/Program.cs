using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CesConsumer;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register Hartonomous infrastructure
                services.AddHartonomousInfrastructure(context.Configuration);

                // Register CES-specific services
                var eventHubConnectionString = context.Configuration["EventHub:ConnectionString"]
                    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key";
                var eventHubName = context.Configuration["EventHub:Name"] ?? "sqlserver-ces-events";

                services.AddSingleton<CdcListener>(sp => new CdcListener(
                    sp.GetRequiredService<ICdcRepository>(),
                    sp.GetRequiredService<ILogger<CdcListener>>(),
                    eventHubConnectionString,
                    eventHubName));

                // Register hosted service
                services.AddHostedService<CesConsumerService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();

        Console.WriteLine("=== Hartonomous CES Consumer ===");
        Console.WriteLine("Processing SQL Server 2025 Change Event Streaming");

        await host.RunAsync();
    }
}