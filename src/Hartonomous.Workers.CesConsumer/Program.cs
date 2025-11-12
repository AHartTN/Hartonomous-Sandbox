using CesConsumer.Services;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Mappers;
using Hartonomous.Core.Models;
using Hartonomous.Infrastructure;
using Hartonomous.Infrastructure.Services.CDC;
using Azure.Identity;
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

                // Azure App Configuration integration (production only)
                var settings = config.Build();
                var appConfigEndpoint = settings["Endpoints:AppConfiguration"];

                if (!string.IsNullOrEmpty(appConfigEndpoint)
                    && context.HostingEnvironment.IsProduction())
                {
                    // Production: Use Azure Arc managed identity
                    // var credential = new DefaultAzureCredential();

                    // config.AddAzureAppConfiguration(options =>
                    // {
                    //     options.Connect(new Uri(appConfigEndpoint), credential)
                    //         // Configure Key Vault integration for secret references
                    //         .ConfigureKeyVault(kv =>
                    //         {
                    //             kv.SetCredential(credential);
                    //         });
                    // });
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<CdcOptions>(context.Configuration.GetSection(CdcOptions.SectionName));

                // Register Hartonomous infrastructure
                services.AddHartonomousInfrastructure(context.Configuration);

                // Register CDC checkpoint manager (database-backed for production resilience)
                services.AddSingleton<ICdcCheckpointManager, SqlCdcCheckpointManager>(sp =>
                {
                    var connectionFactory = sp.GetRequiredService<ISqlServerConnectionFactory>();
                    var logger = sp.GetRequiredService<ILogger<SqlCdcCheckpointManager>>();
                    return new SqlCdcCheckpointManager(connectionFactory, logger, "CesConsumer");
                });

                // Register CDC event mapper
                services.AddSingleton<IEventMapperBidirectional<CdcChangeEvent, BaseEvent>>(
                    _ => new CdcEventMapper());

                // Register CDC event processor
                services.AddScoped<CdcEventProcessor>();

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