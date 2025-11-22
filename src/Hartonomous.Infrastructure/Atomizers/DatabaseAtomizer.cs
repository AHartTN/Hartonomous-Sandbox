using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Database;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes database content by extracting schema metadata, tables, columns, and row data.
/// Supports SQL Server with extensibility for other databases.
/// Converts relational data into atoms with spatial relationships.
/// </summary>
public class DatabaseAtomizer : IAtomizer<DatabaseConnectionInfo>
{
    private const int MaxAtomSize = 64;
    public int Priority => 40;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // This atomizer is invoked explicitly via connection info, not file type
        return false;
    }

    public async Task<AtomizationResult> AtomizeAsync(
        DatabaseConnectionInfo connectionInfo,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            using var connection = new SqlConnection(connectionInfo.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Create database metadata atom (just the name)
            var dbNameBytes = Encoding.UTF8.GetBytes(connection.Database);
            var dbHash = SHA256.HashData(dbNameBytes);
            var dbAtom = new AtomData
            {
                AtomicValue = dbNameBytes,
                ContentHash = dbHash,
                Modality = "database",
                Subtype = "database-name",
                ContentType = "application/x-sql",
                CanonicalText = connection.Database,
                Metadata = $"{{\"server\":\"{connection.DataSource}\",\"database\":\"{connection.Database}\"}}"
            };
            atoms.Add(dbAtom);

            // Get all tables
            var tables = await GetTablesAsync(connection, cancellationToken);
            
            int tableIndex = 0;
            foreach (var table in tables)
            {
                // Process each table (limit to avoid overwhelming)
                if (tableIndex >= connectionInfo.MaxTables)
                {
                    warnings.Add($"Limiting to {connectionInfo.MaxTables} tables");
                    break;
                }

                var tableResult = await AtomizeTableAsync(
                    connection,
                    table.Schema,
                    table.Name,
                    dbHash,
                    tableIndex,
                    connectionInfo.MaxRowsPerTable,
                    cancellationToken);

                atoms.AddRange(tableResult.atoms);
                compositions.AddRange(tableResult.compositions);
                warnings.AddRange(tableResult.warnings);

                tableIndex++;
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count(),
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(DatabaseAtomizer),
                    DetectedFormat = $"SQL Server - {tables.Count} tables",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Database atomization failed: {ex.Message}");
            throw;
        }
    }

    private async Task<List<(string Schema, string Name)>> GetTablesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var tables = new List<(string, string)>();
        
        var query = @"
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME;
        ";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add((reader.GetString(0), reader.GetString(1)));
        }

        return tables;
    }

    private async Task<(List<AtomData> atoms, List<AtomComposition> compositions, List<string> warnings)> AtomizeTableAsync(
        SqlConnection connection,
        string schema,
        string tableName,
        byte[] dbHash,
        int tableIndex,
        int maxRows,
        CancellationToken cancellationToken)
    {
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create table name atom
            var tableFullName = $"{schema}.{tableName}";
            var tableBytes = Encoding.UTF8.GetBytes(tableFullName);
            var tableHash = SHA256.HashData(tableBytes);
            var tableAtom = new AtomData
            {
                AtomicValue = tableBytes,
                ContentHash = tableHash,
                Modality = "database",
                Subtype = "table-name",
                ContentType = "application/x-sql",
                CanonicalText = tableFullName,
                Metadata = $"{{\"schema\":\"{schema}\",\"table\":\"{tableName}\"}}"
            };
            atoms.Add(tableAtom);

            // Link table to database
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = dbHash,
                ComponentAtomHash = tableHash,
                SequenceIndex = tableIndex,
                Position = new SpatialPosition { X = 0, Y = tableIndex, Z = 0 }
            });

            // Get columns
            var columns = await GetColumnsAsync(connection, schema, tableName, cancellationToken);

            int colIndex = 0;
            foreach (var column in columns)
            {
                var colBytes = Encoding.UTF8.GetBytes(column.Name);
                var colHash = SHA256.HashData(colBytes);
                var colAtom = new AtomData
                {
                    AtomicValue = colBytes,
                    ContentHash = colHash,
                    Modality = "database",
                    Subtype = "column-name",
                    ContentType = "application/x-sql",
                    CanonicalText = column.Name,
                    Metadata = $"{{\"name\":\"{column.Name}\",\"dataType\":\"{column.DataType}\",\"nullable\":{column.IsNullable.ToString().ToLower()}}}"
                };
                atoms.Add(colAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = tableHash,
                    ComponentAtomHash = colHash,
                    SequenceIndex = colIndex++,
                    Position = new SpatialPosition { X = colIndex, Y = 0, Z = 0 }
                });
            }

            // Get sample rows
            var query = $"SELECT TOP {maxRows} * FROM [{schema}].[{tableName}];";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            int rowIndex = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                // Create row metadata atom (composite key or first column value)
                var rowKey = reader.GetValue(0)?.ToString() ?? $"row_{rowIndex}";
                var rowBytes = Encoding.UTF8.GetBytes($"{tableFullName}:row{rowIndex}");
                var rowHash = SHA256.HashData(Encoding.UTF8.GetBytes($"{tableFullName}:{rowKey}:{rowIndex}"));
                var rowAtom = new AtomData
                {
                    AtomicValue = rowBytes,
                    ContentHash = rowHash,
                    Modality = "database",
                    Subtype = "row-metadata",
                    ContentType = "application/x-sql",
                    CanonicalText = rowKey,
                    Metadata = $"{{\"table\":\"{tableFullName}\",\"rowIndex\":{rowIndex},\"primaryKey\":\"{rowKey}\"}}"
                };
                atoms.Add(rowAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = tableHash,
                    ComponentAtomHash = rowHash,
                    SequenceIndex = rowIndex,
                    Position = new SpatialPosition { X = 0, Y = rowIndex, Z = 0 }
                });

                // Atomize cell values
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    if (value == null || value == DBNull.Value)
                        continue;

                    var cellValueStr = value.ToString() ?? "";
                    var cellBytes = Encoding.UTF8.GetBytes(cellValueStr);
                    
                    // Truncate large values
                    if (cellBytes.Length > MaxAtomSize)
                        cellBytes = cellBytes.Take(MaxAtomSize).ToArray();

                    var cellHash = SHA256.HashData(cellBytes);
                    var cellAtom = new AtomData
                    {
                        AtomicValue = cellBytes,
                        ContentHash = cellHash,
                        Modality = "database",
                        Subtype = "cell-value",
                        ContentType = "text/plain",
                        CanonicalText = cellValueStr.Length > 100 ? cellValueStr[..100] + "..." : cellValueStr,
                        Metadata = $"{{\"column\":\"{reader.GetName(i)}\",\"dataType\":\"{value.GetType().Name}\"}}"
                    };

                    // Only add if unique
                    if (!atoms.Any(a => a.ContentHash.SequenceEqual(cellHash)))
                    {
                        atoms.Add(cellAtom);
                    }

                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = rowHash,
                        ComponentAtomHash = cellHash,
                        SequenceIndex = i,
                        Position = new SpatialPosition { X = i, Y = rowIndex, Z = 0 }
                    });
                }

                rowIndex++;
            }

            if (rowIndex >= maxRows)
            {
                warnings.Add($"Table {tableFullName} limited to {maxRows} rows");
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Table {schema}.{tableName} atomization failed: {ex.Message}");
        }

        return (atoms, compositions, warnings);
    }

    private async Task<List<ColumnInfo>> GetColumnsAsync(
        SqlConnection connection,
        string schema,
        string tableName,
        CancellationToken cancellationToken)
    {
        var columns = new List<ColumnInfo>();
        
        var query = @"
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION;
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@TableName", tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES"
            });
        }

        return columns;
    }

    private class ColumnInfo
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "";
        public bool IsNullable { get; set; }
    }
}

/// <summary>
/// Database connection information for atomization.
/// </summary>
public class DatabaseConnectionInfo
{
    public required string ConnectionString { get; set; }
    public int MaxTables { get; set; } = 50;
    public int MaxRowsPerTable { get; set; } = 1000;
}
