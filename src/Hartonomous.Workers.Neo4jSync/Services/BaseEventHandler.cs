using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;

namespace Hartonomous.Workers.Neo4jSync.Services;

public abstract class BaseEventHandler : IBaseEventHandler
{
    public bool CanHandle(BaseEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return CanHandleCore(message);
    }

    public Task HandleAsync(BaseEvent message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!CanHandleCore(message))
        {
            return Task.CompletedTask;
        }

        return HandleCoreAsync(message, cancellationToken);
    }

    protected abstract bool CanHandleCore(BaseEvent message);

    protected abstract Task HandleCoreAsync(BaseEvent message, CancellationToken cancellationToken);

    protected static bool TryGetSqlServerContext(BaseEvent message, out string table, out string operation)
    {
        table = string.Empty;
        operation = string.Empty;

        if (!message.Extensions.TryGetValue("sqlserver", out var extension) || extension is not Dictionary<string, object> sql)
        {
            return false;
        }

        table = sql.GetValueOrDefault("table")?.ToString() ?? string.Empty;
        operation = sql.GetValueOrDefault("operation")?.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(table) && !string.IsNullOrWhiteSpace(operation);
    }
}
