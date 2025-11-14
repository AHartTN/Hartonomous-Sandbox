using System;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering persistence services (DbContext, repositories)
/// </summary>
public static class PersistenceServiceExtensions
{
    /// <summary>
    /// Registers database context and all repository implementations
    /// </summary>
    public static IServiceCollection AddHartonomousPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        var connectionString = configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found");

        services.Configure<SqlServerOptions>(configuration.GetSection(SqlServerOptions.SectionName));
        services.PostConfigure<SqlServerOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                options.ConnectionString = connectionString;
            }

            if (options.CommandTimeoutSeconds <= 0)
            {
                options.CommandTimeoutSeconds = configuration.GetValue<int?>($"{SqlServerOptions.SectionName}:CommandTimeoutSeconds") ?? 30;
            }
        });

        services.AddDbContext<HartonomousDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);

                // Enable spatial types
                sqlOptions.UseNetTopologySuite();

                // Use split queries by default to prevent cartesian explosion
                // Individual queries can override with .AsSingleQuery() if needed
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("DetailedErrors", false))
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }

            // Enable query logging in development for performance analysis
            var logLevel = configuration.GetValue<string>("Logging:LogLevel:Microsoft.EntityFrameworkCore");
            if (logLevel == "Debug")
            {
                options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            }
        });

        // Repository registrations
        services.AddScoped<IAtomRepository, AtomRepository>();
        services.AddScoped<IAtomEmbeddingRepository, AtomEmbeddingRepository>();
        services.AddScoped<ITensorAtomRepository, TensorAtomRepository>();
        services.AddScoped<ITokenVocabularyRepository, TokenVocabularyRepository>();
        services.AddScoped<IAtomRelationRepository, AtomRelationRepository>();
        services.AddScoped<IIngestionJobRepository, IngestionJobRepository>();
        services.AddScoped<IDeduplicationPolicyRepository, DeduplicationPolicyRepository>();
        services.AddScoped<IModelLayerRepository, ModelLayerRepository>();
        services.AddScoped<ILayerTensorSegmentRepository, LayerTensorSegmentRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ICdcRepository, CdcRepository>();

        // OODA Loop implemented via SQL Server Service Broker + T-SQL stored procedures:
        // - sp_Analyze: Observe/analyze performance, detect anomalies
        // - sp_Act: Execute improvements (UPDATE STATISTICS, force plans, warm cache)
        // - sp_Learn: Measure outcomes, update model weights
        // No C# repositories needed - all logic in database for ACID guarantees

        services.AddScoped<IInferenceRequestRepository, InferenceRequestRepository>();

        // Atomic component repositories (deduplicated storage)
        services.AddScoped<Core.Interfaces.IAtomicTextTokenRepository, Repositories.AtomicTextTokenRepository>();
        services.AddScoped<Core.Interfaces.IAtomicPixelRepository, Repositories.AtomicPixelRepository>();
        services.AddScoped<Core.Interfaces.IAtomicAudioSampleRepository, Repositories.AtomicAudioSampleRepository>();

        // CDC checkpoint management
        services.AddScoped<ICdcCheckpointManager, Services.CDC.SqlCdcCheckpointManager>();

        return services;
    }
}
