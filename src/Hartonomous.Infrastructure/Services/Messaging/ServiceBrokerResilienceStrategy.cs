using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Messaging;

public sealed class ServiceBrokerResilienceStrategy : IServiceBrokerResilienceStrategy
{
    private readonly IRetryPolicy _publishRetryPolicy;
    private readonly IRetryPolicy _receiveRetryPolicy;
    private readonly ICircuitBreakerPolicy _circuitBreakerPolicy;

    public ServiceBrokerResilienceStrategy(
        IOptions<ServiceBrokerResilienceOptions> options,
        Func<RetryPolicyOptions, IRetryPolicy> retryPolicyFactory,
        ICircuitBreakerPolicy circuitBreakerPolicy,
        ILogger<ServiceBrokerResilienceStrategy> logger)
    {
        var resilienceOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _circuitBreakerPolicy = circuitBreakerPolicy ?? throw new ArgumentNullException(nameof(circuitBreakerPolicy));
        _ = logger ?? throw new ArgumentNullException(nameof(logger));

        _publishRetryPolicy = retryPolicyFactory?.Invoke(new RetryPolicyOptions
        {
            MaxAttempts = resilienceOptions.PublishMaxAttempts,
            BaseDelay = resilienceOptions.PublishBaseDelay,
            MaxDelay = resilienceOptions.PublishMaxDelay,
            ExponentialFactor = resilienceOptions.ExponentialFactor,
            JitterFactor = resilienceOptions.JitterFactor
        }) ?? throw new ArgumentNullException(nameof(retryPolicyFactory));

        _receiveRetryPolicy = retryPolicyFactory.Invoke(new RetryPolicyOptions
        {
            MaxAttempts = resilienceOptions.ReceiveMaxAttempts,
            BaseDelay = resilienceOptions.ReceiveBaseDelay,
            MaxDelay = resilienceOptions.ReceiveMaxDelay,
            ExponentialFactor = resilienceOptions.ExponentialFactor,
            JitterFactor = resilienceOptions.JitterFactor
        });
    }

    public Task ExecutePublishAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        return _circuitBreakerPolicy.ExecuteAsync(ct => _publishRetryPolicy.ExecuteAsync(operation, ct), cancellationToken);
    }

    public Task<TResult> ExecuteReceiveAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        return _circuitBreakerPolicy.ExecuteAsync(ct => _receiveRetryPolicy.ExecuteAsync(operation, ct), cancellationToken);
    }
}
