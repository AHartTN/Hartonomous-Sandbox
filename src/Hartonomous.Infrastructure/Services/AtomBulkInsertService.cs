using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// High-performance bulk atom insertion with SHA-256 deduplication via MERGE.
/// Uses Table-Valued Parameters for batch inserts with ACID compliance.
/// </summary>
public class AtomBulkInsertService : IAtomBulkInsertService
{
    private readonly string _connectionString;
    private readonly ILogger<AtomBulkInsertService> _logger;

    public AtomBulkInsertService(string connectionString, ILogger<AtomBulkInsertService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Bulk inserts an atomization result (atoms + compositions) into the database.
    /// </summary>
    public async Task<int> BulkInsertAsync(AtomizationResult result, CancellationToken cancellationToken = default)
    {
        // Extract tenant ID from first atom's metadata or default to 1
        var tenantId = 1; // TODO: Extract from result.ProcessingInfo or auth context
        
        // Insert atoms and get ID mappings
        var atomIdMap = await BulkInsertAtomsAsync(result.Atoms, tenantId, cancellationToken);
        
        // Insert compositions if any exist
        if (result.Compositions?.Count > 0)
        {
            await BulkInsertCompositionsAsync(result.Compositions, atomIdMap, tenantId, cancellationToken);
        }
        
        return result.Atoms.Count;
    }

    /// <summary>
    /// Bulk insert atoms with automatic deduplication based on ContentHash.
    /// Returns mapping of ContentHash → AtomId for composition linking.
    /// </summary>
    public async Task<Dictionary<string, long>> BulkInsertAtomsAsync(
        List<AtomData> atoms,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atomIdMap = new Dictionary<string, long>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Create DataTable for TVP
                var atomTable = CreateAtomDataTable(atoms, tenantId);

                // Step 2: MERGE atoms (upsert with deduplication)
                var mergedAtoms = await MergeAtomsAsync(connection, transaction, atomTable, cancellationToken);

                // Step 3: Build hash → ID mapping
                foreach (var row in mergedAtoms.AsEnumerable())
                {
                    var hash = row.Field<byte[]>("ContentHash");
                    var atomId = row.Field<long>("AtomId");
                    if (hash != null)
                    {
                        var hashStr = Convert.ToBase64String(hash);
                        atomIdMap[hashStr] = atomId;
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "Bulk inserted {TotalAtoms} atoms ({UniqueAtoms} unique) in {DurationMs}ms",
                    atoms.Count,
                    atomIdMap.Count,
                    sw.ElapsedMilliseconds);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk atom insert failed");
            throw;
        }

        return atomIdMap;
    }

    /// <summary>
    /// Bulk insert atom compositions (parent-child relationships).
    /// </summary>
    public async Task BulkInsertCompositionsAsync(
        List<AtomComposition> compositions,
        Dictionary<string, long> atomIdMap,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();

            try
            {
                // Create DataTable for compositions
                var compositionTable = CreateCompositionDataTable(compositions, atomIdMap, tenantId);

                // Bulk insert compositions
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
                {
                    DestinationTableName = "dbo.AtomComposition",
                    BatchSize = 10000,
                    BulkCopyTimeout = 300
                };

                bulkCopy.ColumnMappings.Add("ParentAtomId", "ParentAtomId");
                bulkCopy.ColumnMappings.Add("ComponentAtomId", "ComponentAtomId");
                bulkCopy.ColumnMappings.Add("SequenceIndex", "SequenceIndex");
                bulkCopy.ColumnMappings.Add("SpatialKey", "SpatialKey");
                bulkCopy.ColumnMappings.Add("TenantId", "TenantId");

                await bulkCopy.WriteToServerAsync(compositionTable, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "Bulk inserted {CompositionCount} compositions in {DurationMs}ms",
                    compositions.Count,
                    sw.ElapsedMilliseconds);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk composition insert failed");
            throw;
        }
    }

    private DataTable CreateAtomDataTable(List<AtomData> atoms, int tenantId)
    {
        var table = new DataTable();
        table.Columns.Add("ContentHash", typeof(byte[]));
        table.Columns.Add("AtomicValue", typeof(byte[]));
        table.Columns.Add("Modality", typeof(string));
        table.Columns.Add("Subtype", typeof(string));
        table.Columns.Add("ContentType", typeof(string));
        table.Columns.Add("CanonicalText", typeof(string));
        table.Columns.Add("Metadata", typeof(string));
        table.Columns.Add("TenantId", typeof(int));

        foreach (var atom in atoms)
        {
            table.Rows.Add(
                atom.ContentHash,
                atom.AtomicValue,
                atom.Modality ?? "unknown",
                atom.Subtype,
                atom.ContentType,
                atom.CanonicalText,
                atom.Metadata,
                tenantId
            );
        }

        return table;
    }

    private async Task<DataTable> MergeAtomsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DataTable atomTable,
        CancellationToken cancellationToken)
    {
        var mergeQuery = @"
            MERGE dbo.Atom AS target
            USING @NewAtoms AS source
            ON target.ContentHash = source.ContentHash AND target.TenantId = source.TenantId
            WHEN MATCHED THEN
                UPDATE SET ReferenceCount = target.ReferenceCount + 1
            WHEN NOT MATCHED THEN
                INSERT (ContentHash, AtomicValue, Modality, Subtype, ContentType, CanonicalText, Metadata, TenantId, ReferenceCount, CreatedAt)
                VALUES (source.ContentHash, source.AtomicValue, source.Modality, source.Subtype, source.ContentType, source.CanonicalText, source.Metadata, source.TenantId, 1, SYSUTCDATETIME())
            OUTPUT inserted.AtomId, inserted.ContentHash;
        ";

        using var command = new SqlCommand(mergeQuery, connection, transaction);
        command.CommandTimeout = 300;

        // Add TVP parameter
        var tvpParam = command.Parameters.AddWithValue("@NewAtoms", atomTable);
        tvpParam.SqlDbType = SqlDbType.Structured;
        tvpParam.TypeName = "dbo.AtomTableType"; // Assumes this UDT exists

        // Execute and return results
        var resultTable = new DataTable();
        using var adapter = new SqlDataAdapter(command);
        adapter.Fill(resultTable);

        return resultTable;
    }

    private DataTable CreateCompositionDataTable(
        List<AtomComposition> compositions,
        Dictionary<string, long> atomIdMap,
        int tenantId)
    {
        var table = new DataTable();
        table.Columns.Add("ParentAtomId", typeof(long));
        table.Columns.Add("ComponentAtomId", typeof(long));
        table.Columns.Add("SequenceIndex", typeof(long));
        table.Columns.Add("SpatialKey", typeof(string)); // WKT format
        table.Columns.Add("TenantId", typeof(int));

        foreach (var comp in compositions)
        {
            var parentHashStr = Convert.ToBase64String(comp.ParentAtomHash);
            var componentHashStr = Convert.ToBase64String(comp.ComponentAtomHash);

            // Skip if atom IDs not found (shouldn't happen if atoms inserted first)
            if (!atomIdMap.TryGetValue(parentHashStr, out var parentId) ||
                !atomIdMap.TryGetValue(componentHashStr, out var componentId))
            {
                _logger.LogWarning("Skipping composition with missing atom IDs");
                continue;
            }

            table.Rows.Add(
                parentId,
                componentId,
                comp.SequenceIndex,
                comp.Position.ToWkt(),
                tenantId
            );
        }

        return table;
    }
}
