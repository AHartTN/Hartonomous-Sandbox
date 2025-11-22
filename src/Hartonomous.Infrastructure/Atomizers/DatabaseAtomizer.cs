using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Database;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

public class DatabaseAtomizer : BaseAtomizer<DatabaseConnectionInfo>
{
    public DatabaseAtomizer(ILogger<DatabaseAtomizer> logger) : base(logger) { }

    public override int Priority => 40;

    public override bool CanHandle(string contentType, string? fileExtension) => false;

    protected override async Task AtomizeCoreAsync(
        DatabaseConnectionInfo connectionInfo,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(connectionInfo.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var dbNameBytes = Encoding.UTF8.GetBytes(connection.Database);
        var dbHash = HashUtilities.ComputeSHA256(dbNameBytes);
        
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

        var tables = await GetTablesAsync(connection, cancellationToken);
        
        int tableIndex = 0;
        foreach (var table in tables)
        {
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
    }

    protected override string GetDetectedFormat() => "SQL Server database";
    protected override string GetModality() => "database";

    protected override byte[] GetFileMetadataBytes(DatabaseConnectionInfo input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"database:sqlserver:{input.MaxTables}");
    }

    protected override string GetCanonicalFileText(DatabaseConnectionInfo input, SourceMetadata source)
    {
        return $"SQL Server ({input.MaxTables} tables max)";
    }

    protected override string GetFileMetadataJson(DatabaseConnectionInfo input, SourceMetadata source)
    {
        return $"{{\"type\":\"sqlserver\",\"maxTables\":{input.MaxTables},\"maxRowsPerTable\":{input.MaxRowsPerTable}}}";
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
            var tableFullName = $"{schema}.{tableName}";
            var tableBytes = Encoding.UTF8.GetBytes(tableFullName);
            var tableHash = CreateContentAtom(
                tableBytes,
                "database",
                "table-name",
                tableFullName,
                $"{{\"schema\":\"{schema}\",\"table\":\"{tableName}\"}}",
                atoms);

            CreateAtomComposition(dbHash, tableHash, tableIndex, compositions, y: tableIndex);

            var columns = await GetColumnsAsync(connection, schema, tableName, cancellationToken);

            int colIndex = 0;
            foreach (var column in columns)
            {
                var colBytes = Encoding.UTF8.GetBytes(column.Name);
                var colHash = CreateContentAtom(
                    colBytes,
                    "database",
                    "column-name",
                    column.Name,
                    $"{{\"name\":\"{column.Name}\",\"dataType\":\"{column.DataType}\",\"nullable\":{column.IsNullable.ToString().ToLower()}}}",
                    atoms);

                CreateAtomComposition(tableHash, colHash, colIndex++, compositions, x: colIndex);
            }

            var query = $"SELECT TOP {maxRows} * FROM [{schema}].[{tableName}];";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            int rowIndex = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                var rowKey = reader.GetValue(0)?.ToString() ?? $"row_{rowIndex}";
                var rowBytes = Encoding.UTF8.GetBytes($"{tableFullName}:row{rowIndex}");
                var rowHash = HashUtilities.ComputeSHA256(Encoding.UTF8.GetBytes($"{tableFullName}:{rowKey}:{rowIndex}"));
                
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

                CreateAtomComposition(tableHash, rowHash, rowIndex, compositions, y: rowIndex);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    if (value == null || value == DBNull.Value)
                        continue;

                    var cellValueStr = value.ToString() ?? "";
                    var cellBytes = Encoding.UTF8.GetBytes(cellValueStr);
                    
                    if (cellBytes.Length > MaxAtomSize)
                        cellBytes = cellBytes.Take(MaxAtomSize).ToArray();

                    var canonicalText = cellValueStr.Length > 100 ? cellValueStr[..100] + "..." : cellValueStr;
                    var cellHash = CreateContentAtom(
                        cellBytes,
                        "database",
                        "cell-value",
                        canonicalText,
                        $"{{\"column\":\"{reader.GetName(i)}\",\"dataType\":\"{value.GetType().Name}\"}}",
                        atoms);

                    CreateAtomComposition(rowHash, cellHash, i, compositions, x: i, y: rowIndex);
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
