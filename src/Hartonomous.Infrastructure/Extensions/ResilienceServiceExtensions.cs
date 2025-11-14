using System;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Resilience;
using Hartonomous.Infrastructure.Resilience;
using Hartonomous.Infrastructure.Services.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering resilience services (circuit breakers, retry policies, HTTP resilience)
/// </summary>
public static class ResilienceServiceExtensions
{
    /// <summary>
    /// Registers resilience patterns: circuit breaker, retry, timeout
    /// </summary>
    public static IServiceCollection AddHartonomousResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        services.Configure<ServiceBrokerResilienceOptions>(configuration.GetSection(ServiceBrokerResilienceOptions.SectionName));

        // Core resilience primitives
        services.AddSingleton<ITransientErrorDetector, SqlServerTransientErrorDetector>();
        services.AddSingleton<Func<RetryPolicyOptions, IRetryPolicy>>(sp => options =>
        {
            var detector = sp.GetRequiredService<ITransientErrorDetector>();
            var logger = sp.GetRequiredService<ILogger<ExponentialBackoffRetryPolicy>>();
            return new ExponentialBackoffRetryPolicy(options, detector, logger);
        });
        services.AddSingleton<ICircuitBreakerPolicy>(sp =>
        {
            var resilienceOptions = sp.GetRequiredService<IOptions<ServiceBrokerResilienceOptions>>().Value;
            var options = new CircuitBreakerOptions
            {
                FailureThreshold = resilienceOptions.CircuitBreakerFailureThreshold,
                BreakDuration = resilienceOptions.CircuitBreakerBreakDuration,
                HalfOpenSuccessThreshold = resilienceOptions.CircuitBreakerHalfOpenSuccessThreshold
            };

            return new CircuitBreakerPolicy(options, sp.GetRequiredService<ITransientErrorDetector>(), sp.GetRequiredService<ILogger<CircuitBreakerPolicy>>());
        });
        services.AddSingleton<IServiceBrokerResilienceStrategy, ServiceBrokerResilienceStrategy>();
        services.AddSingleton<IMessageDeadLetterSink, SqlMessageDeadLetterSink>();

        // HttpClient factory
        services.AddHttpClient();

        // Default resilience pipeline (circuit breaker + retry + timeout)
        services.AddHttpClient(ResiliencePipelineNames.Default)
            .AddStandardResilienceHandler(options =>
            {
                var resilienceConfig = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();

                // Circuit breaker: Open after 5 failures in 30s, break for 60s
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = resilienceConfig.CircuitBreakerSamplingDuration;
                options.CircuitBreaker.MinimumThroughput = resilienceConfig.CircuitBreakerMinimumThroughput;
                options.CircuitBreaker.BreakDuration = resilienceConfig.CircuitBreakerBreakDuration;

                // Retry: 5 attempts with exponential backoff (2s, 4s, 8s, 16s, 32s) + jitter
                options.Retry.MaxRetryAttempts = resilienceConfig.RetryMaxAttempts;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = resilienceConfig.RetryUseJitter;
                options.Retry.Delay = resilienceConfig.RetryBaseDelay;

                // Timeout: 30s default
                options.TotalRequestTimeout.Timeout = resilienceConfig.DefaultTimeout;
            });

        // Inference resilience pipeline (longer timeout for model inference)
        services.AddHttpClient(ResiliencePipelineNames.Inference)
            .AddStandardResilienceHandler(options =>
            {
                var resilienceConfig = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = resilienceConfig.CircuitBreakerSamplingDuration;
                options.CircuitBreaker.MinimumThroughput = resilienceConfig.CircuitBreakerMinimumThroughput;
                options.CircuitBreaker.BreakDuration = resilienceConfig.CircuitBreakerBreakDuration;
                options.Retry.MaxRetryAttempts = 3; // Fewer retries for long-running operations
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = resilienceConfig.InferenceTimeout; // 5 minutes
            });

        // Generation resilience pipeline (longest timeout for video/audio generation)
        services.AddHttpClient(ResiliencePipelineNames.Generation)
            .AddStandardResilienceHandler(options =>
            {
                var resilienceConfig = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = resilienceConfig.CircuitBreakerSamplingDuration;
                options.CircuitBreaker.MinimumThroughput = 5; // Lower threshold for generation
                options.CircuitBreaker.BreakDuration = resilienceConfig.CircuitBreakerBreakDuration;
                options.Retry.MaxRetryAttempts = 2; // Minimal retries for very long operations
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = resilienceConfig.GenerationTimeout; // 10 minutes
            });

        return services;
    }
}
