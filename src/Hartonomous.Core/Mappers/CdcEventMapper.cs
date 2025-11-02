using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Core.Models;

namespace Hartonomous.Core.Mappers;

/// <summary>
/// Maps SQL Server CDC change events to platform BaseEvent format.
/// Encapsulates conversion logic for CDC-to-CloudEvent transformation.
/// </summary>
public class CdcEventMapper : IEventMapper<CdcChangeEvent, BaseEvent>
{
    private readonly string _serverName;
    private readonly string _databaseName;

    public CdcEventMapper(string? serverName = null, string? databaseName = null)
    {
        _serverName = serverName ?? Environment.MachineName;
        _databaseName = databaseName ?? "Hartonomous";
    }

    public BaseEvent Map(CdcChangeEvent source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var evt = new BaseEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri($"/sqlserver/{_serverName}/{_databaseName}"),
            Type = GetEventType(source.Operation),
            Time = DateTimeOffset.UtcNow,
            Subject = $"{source.TableName}/lsn:{source.Lsn}",
            DataSchema = new Uri("https://schemas.microsoft.com/sqlserver/2025/ces"),
            Data = source.Data ?? new Dictionary<string, object>()
        };

        // Add SQL Server specific extensions
        evt.Extensions["sqlserver"] = new Dictionary<string, object>
        {
            ["operation"] = GetOperationName(source.Operation),
            ["table"] = source.TableName,
            ["lsn"] = source.Lsn,
            ["database"] = _databaseName,
            ["server"] = _serverName
        };

        return evt;
    }

    public IEnumerable<BaseEvent> MapMany(IEnumerable<CdcChangeEvent> sources)
    {
        return sources.Select(Map);
    }

    private static string GetEventType(int operation) => operation switch
    {
        1 => "com.microsoft.sqlserver.cdc.delete",
        2 => "com.microsoft.sqlserver.cdc.insert",
        3 => "com.microsoft.sqlserver.cdc.update.before",
        4 => "com.microsoft.sqlserver.cdc.update.after",
        _ => "com.microsoft.sqlserver.cdc.unknown"
    };

    private static string GetOperationName(int operation) => operation switch
    {
        1 => "delete",
        2 => "insert",
        3 => "update_before",
        4 => "update_after",
        _ => "unknown"
    };
}
