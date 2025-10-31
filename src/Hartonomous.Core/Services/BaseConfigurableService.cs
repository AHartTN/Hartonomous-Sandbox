using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Services;

public abstract class BaseConfigurableService<TConfig> : BaseService where TConfig : class
{
    protected TConfig Config { get; }

    protected BaseConfigurableService(ILogger logger, TConfig config)
        : base(logger)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await base.InitializeAsync(cancellationToken);
        Logger.LogDebug("{ServiceName} loaded configuration: {ConfigType}", ServiceName, typeof(TConfig).Name);
    }
}
