using CesConsumer.Services;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Mappers;
using Hartonomous.Core.Models;
using Hartonomous.Infrastructure;
using Hartonomous.Infrastructure.Services.CDC;
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

                // Register CDC checkpoint manager (file-based for now)
                services.AddSingleton<ICdcCheckpointManager, FileCdcCheckpointManager>();

                // Register CDC event mapper
                services.AddSingleton<IEventMapperBidirectional<CdcChangeEvent, BaseEvent>>(
                    _ => new CdcEventMapper());

                // Register CDC event processor
                services.AddSingleton<CdcEventProcessor>();

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