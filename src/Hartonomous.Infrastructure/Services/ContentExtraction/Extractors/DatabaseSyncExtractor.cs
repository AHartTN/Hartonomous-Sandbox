using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services.ContentExtraction;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Hartonomous.Infrastructure.Services.ContentExtraction.Extractors;

/// <summary>
/// Extracts data from external SQL databases (SQL Server, PostgreSQL, MySQL).
/// Maps tables -> atom types, rows -> atoms, foreign keys -> relationships.
/// </summary>
public sealed class DatabaseSyncExtractor : IContentExtractor
{
    public bool CanHandle(ContentExtractionContext context)
    {
        // This extractor handles database sources specified via metadata
        return context.SourceType == ContentSourceType.Stream && 
               context.Metadata != null && 
               context.Metadata.ContainsKey("database_type");
    }

    public async Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        if (context.Metadata == null)
        {
            throw new ArgumentException("Metadata is required for database extraction", nameof(context));
        }

        var dbType = context.Metadata.TryGetValue("database_type", out var type) ? type : "unknown";
        var connectionString = context.Metadata.TryGetValue("connection_string", out var connStr) ? connStr : null;
        var tableName = context.Metadata.TryGetValue("table_name", out var table) ? table : null;
        var sourceUri = context.Metadata.TryGetValue("sourceUri", out var uri) ? uri : $"database://{dbType}";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("connection_string is required in metadata", nameof(context));
        }

        var requests = new List<AtomIngestionRequest>();
        var diagnostics = new Dictionary<string, string>();

        diagnostics["database_type"] = dbType;
        diagnostics["source_uri"] = sourceUri;

        try
        {
            await using var connection = CreateConnection(dbType, connectionString);
            await connection.OpenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                // Extract single table
                await ExtractTable(connection, tableName, requests, sourceUri, diagnostics, cancellationToken);
            }
            else
            {
                // Extract all tables
                var tables = await GetTableNames(connection, dbType, cancellationToken);
                diagnostics["tables_found"] = tables.Count.ToString();

                foreach (var tblName in tables)
                {
                    await ExtractTable(connection, tblName, requests, sourceUri, diagnostics, cancellationToken);
                }
            }

            diagnostics["extraction_status"] = "success";
            diagnostics["atoms_created"] = requests.Count.ToString();
        }
        catch (Exception ex)
        {
            diagnostics["extraction_status"] = "failed";
            diagnostics["error"] = ex.Message;
        }

        return new ContentExtractionResult(requests, diagnostics);
    }

    private DbConnection CreateConnection(string dbType, string connectionString)
    {
        return dbType.ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" => new SqlConnection(connectionString),
            "postgresql" or "postgres" => new NpgsqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database type '{dbType}' is not supported")
        };
    }

    private async Task<List<string>> GetTableNames(DbConnection connection, string dbType, CancellationToken cancellationToken)
    {
        var tables = new List<string>();

        var query = dbType.ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" => "SELECT TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            "postgresql" or "postgres" => "SELECT schemaname || '.' || tablename FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema')",
            "mysql" => "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            _ => throw new NotSupportedException($"Database type '{dbType}' is not supported")
        };

        await using var command = connection.CreateCommand();
        command.CommandText = query;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (dbType.ToLowerInvariant() == "mysql")
            {
                tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
            }
            else
            {
                tables.Add(reader.GetString(0));
            }
        }

        return tables;
    }

    private async Task ExtractTable(
        DbConnection connection, 
        string tableName, 
        List<AtomIngestionRequest> requests,
        string sourceUri,
        Dictionary<string, string> diagnostics,
        CancellationToken cancellationToken)
    {
        // Get column metadata
        var columns = await GetColumnMetadata(connection, tableName, cancellationToken);
        var primaryKeys = await GetPrimaryKeys(connection, tableName, cancellationToken);
        var foreignKeys = await GetForeignKeys(connection, tableName, cancellationToken);

        diagnostics[$"{tableName}_columns"] = columns.Count.ToString();
        diagnostics[$"{tableName}_primary_keys"] = primaryKeys.Count.ToString();
        diagnostics[$"{tableName}_foreign_keys"] = foreignKeys.Count.ToString();

        // Create table metadata atom
        var tableMetadata = new MetadataEnvelope()
            .Set("table_name", tableName)
            .Set("column_count", columns.Count)
            .Set("primary_keys", string.Join(",", primaryKeys))
            .Set("has_foreign_keys", foreignKeys.Count > 0 ? "true" : "false");

        var tableAtom = new AtomIngestionRequestBuilder()
            .WithCanonicalText($"Table: {tableName}")
            .WithModality("schema", "database_table")
            .WithSource("database_sync", sourceUri)
            .WithMetadata(tableMetadata)
            .Build();

        requests.Add(tableAtom);

        // Extract rows
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";
        command.CommandTimeout = 300; // 5 minutes

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        int rowCount = 0;
        const int maxRows = 10000; // Limit to prevent memory issues

        while (await reader.ReadAsync(cancellationToken) && rowCount < maxRows)
        {
            var rowData = new Dictionary<string, string>();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "NULL";
                rowData[columnName] = value;
            }

            // Build primary key value for hash
            var pkValue = string.Join("|", primaryKeys.Select(pk => rowData.TryGetValue(pk, out var v) ? v : "NULL"));
            var rowText = string.Join(", ", rowData.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            var rowMetadata = new MetadataEnvelope()
                .Set("table_name", tableName)
                .Set("row_number", rowCount)
                .Set("primary_key_value", pkValue);

            foreach (var kvp in rowData.Take(20)) // Limit metadata fields
            {
                rowMetadata.Set($"col_{kvp.Key}", kvp.Value);
            }

            var rowAtom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(rowText)
                .WithModality("structured_data", "database_row")
                .WithSource("database_sync", sourceUri)
                .WithHash($"{tableName}|{pkValue}")
                .WithMetadata(rowMetadata)
                .Build();

            requests.Add(rowAtom);
            rowCount++;
        }

        diagnostics[$"{tableName}_rows_extracted"] = rowCount.ToString();
    }

    private async Task<List<string>> GetColumnMetadata(DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<string>();

        var parts = tableName.Split('.');
        var schema = parts.Length > 1 ? parts[0] : "dbo";
        var table = parts.Length > 1 ? parts[1] : parts[0];

        await using var command = connection.CreateCommand();
        
        if (connection is SqlConnection)
        {
            command.CommandText = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table 
                ORDER BY ORDINAL_POSITION";
            
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@Schema";
            schemaParam.Value = schema;
            command.Parameters.Add(schemaParam);
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@Table";
            tableParam.Value = table;
            command.Parameters.Add(tableParam);
        }
        else if (connection is NpgsqlConnection)
        {
            command.CommandText = @"
                SELECT column_name 
                FROM information_schema.columns 
                WHERE table_schema = @Schema AND table_name = @Table 
                ORDER BY ordinal_position";
            
            command.Parameters.Add(new NpgsqlParameter("@Schema", schema));
            command.Parameters.Add(new NpgsqlParameter("@Table", table));
        }
        else if (connection is MySqlConnection)
        {
            command.CommandText = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table 
                ORDER BY ORDINAL_POSITION";
            
            command.Parameters.Add(new MySqlParameter("@Schema", schema));
            command.Parameters.Add(new MySqlParameter("@Table", table));
        }

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(0));
            }
        }
        catch
        {
            // Fallback: query the table directly
        }

        return columns;
    }

    private async Task<List<string>> GetPrimaryKeys(DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var primaryKeys = new List<string>();

        var parts = tableName.Split('.');
        var schema = parts.Length > 1 ? parts[0] : "dbo";
        var table = parts.Length > 1 ? parts[1] : parts[0];

        await using var command = connection.CreateCommand();

        if (connection is SqlConnection)
        {
            command.CommandText = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                  AND TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table
                ORDER BY ORDINAL_POSITION";
            
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@Schema";
            schemaParam.Value = schema;
            command.Parameters.Add(schemaParam);
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@Table";
            tableParam.Value = table;
            command.Parameters.Add(tableParam);
        }
        else if (connection is NpgsqlConnection)
        {
            command.CommandText = @"
                SELECT a.attname
                FROM pg_index i
                JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
                WHERE i.indrelid = (@Schema || '.' || @Table)::regclass AND i.indisprimary";
            
            command.Parameters.Add(new NpgsqlParameter("@Schema", schema));
            command.Parameters.Add(new NpgsqlParameter("@Table", table));
        }
        else if (connection is MySqlConnection)
        {
            command.CommandText = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE CONSTRAINT_NAME = 'PRIMARY' AND TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table
                ORDER BY ORDINAL_POSITION";
            
            command.Parameters.Add(new MySqlParameter("@Schema", schema));
            command.Parameters.Add(new MySqlParameter("@Table", table));
        }

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                primaryKeys.Add(reader.GetString(0));
            }
        }
        catch
        {
            // No primary keys found
        }

        return primaryKeys;
    }

    private async Task<List<(string Column, string RefTable, string RefColumn)>> GetForeignKeys(
        DbConnection connection, 
        string tableName, 
        CancellationToken cancellationToken)
    {
        var foreignKeys = new List<(string, string, string)>();

        var parts = tableName.Split('.');
        var schema = parts.Length > 1 ? parts[0] : "dbo";
        var table = parts.Length > 1 ? parts[1] : parts[0];

        await using var command = connection.CreateCommand();

        if (connection is SqlConnection)
        {
            command.CommandText = @"
                SELECT 
                    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS Column,
                    OBJECT_SCHEMA_NAME(fc.referenced_object_id) + '.' + OBJECT_NAME(fc.referenced_object_id) AS RefTable,
                    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS RefColumn
                FROM sys.foreign_key_columns fc
                WHERE OBJECT_SCHEMA_NAME(fc.parent_object_id) = @Schema 
                  AND OBJECT_NAME(fc.parent_object_id) = @Table";
            
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@Schema";
            schemaParam.Value = schema;
            command.Parameters.Add(schemaParam);
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@Table";
            tableParam.Value = table;
            command.Parameters.Add(tableParam);
        }
        else if (connection is NpgsqlConnection)
        {
            command.CommandText = @"
                SELECT 
                    kcu.column_name,
                    ccu.table_schema || '.' || ccu.table_name AS ref_table,
                    ccu.column_name AS ref_column
                FROM information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name
                JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY' 
                  AND tc.table_schema = @Schema 
                  AND tc.table_name = @Table";
            
            command.Parameters.Add(new NpgsqlParameter("@Schema", schema));
            command.Parameters.Add(new NpgsqlParameter("@Table", table));
        }

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                foreignKeys.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }
        }
        catch
        {
            // No foreign keys found
        }

        return foreignKeys;
    }
}
