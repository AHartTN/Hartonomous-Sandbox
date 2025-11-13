using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Hartonomous.Clr.Contracts;
using Microsoft.SqlServer.Types;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// SQL CLR implementation of ITensorProvider.
    /// Uses context connection to query TensorAtoms.WeightsGeometry.
    /// FOLLOWS: AttentionGeneration.cs:363-490 pattern (proven working implementation).
    /// </summary>
    public class SqlTensorProvider : ITensorProvider
    {
        private readonly SqlConnection _connection;
        private readonly bool _ownsConnection;

        public SqlTensorProvider()
        {
            // Context connection for SQL CLR functions
            _connection = new SqlConnection("context connection=true");
            _connection.Open();
            _ownsConnection = true;
        }

        public SqlTensorProvider(SqlConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = false;
        }

        public float[] LoadWeights(string tensorNamePattern, int maxElements)
        {
            if (string.IsNullOrWhiteSpace(tensorNamePattern))
                throw new ArgumentException("Tensor name pattern cannot be empty", nameof(tensorNamePattern));

            var weights = new List<float>();

            using (var command = _connection.CreateCommand())
            {
                // Query pattern from AttentionGeneration.cs:373-380
                command.CommandText = @"
                    SELECT TOP 1 ta.WeightsGeometry, ta.ElementCount
                    FROM dbo.TensorAtoms ta
                    WHERE ta.TensorName LIKE '%' + @pattern + '%'
                    ORDER BY ta.ElementCount DESC;";

                command.Parameters.AddWithValue("@pattern", tensorNamePattern);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Extract GEOMETRY using pattern from AttentionGeneration.cs:383-397
                        var geometryObj = reader.GetValue(0);
                        var elementCount = reader.GetInt64(1);

                        if (geometryObj != null && geometryObj != DBNull.Value)
                        {
                            var geometry = geometryObj as SqlGeometry;
                            if (geometry != null && !geometry.IsNull)
                            {
                                int pointCount = Math.Min((int)elementCount, maxElements);
                                if (geometry.STNumPoints().IsNull)
                                    return weights.ToArray();

                                int actualPoints = geometry.STNumPoints().Value;
                                pointCount = Math.Min(pointCount, actualPoints);

                                // Extract weights from GEOMETRY.STPointN(i).STY.Value
                                // Pattern from AttentionGeneration.cs:388-397
                                for (int i = 1; i <= pointCount; i++)
                                {
                                    var point = geometry.STPointN(i);
                                    if (!point.IsNull && !point.STY.IsNull)
                                    {
                                        var value = point.STY.Value;
                                        weights.Add((float)value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return weights.ToArray();
        }

        public Dictionary<string, float[]> LoadWeightsBatch(Dictionary<string, string> tensorPatterns)
        {
            var results = new Dictionary<string, float[]>();

            foreach (var kvp in tensorPatterns)
            {
                results[kvp.Key] = LoadWeights(kvp.Value, int.MaxValue);
            }

            return results;
        }

        public TensorMetadata GetMetadata(string tensorNamePattern)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT TOP 1 ta.TensorName, ta.TensorShape, ta.DataType, ta.ElementCount, ta.ByteSize
                    FROM dbo.TensorAtoms ta
                    WHERE ta.TensorName LIKE '%' + @pattern + '%'
                    ORDER BY ta.ElementCount DESC;";

                command.Parameters.AddWithValue("@pattern", tensorNamePattern);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new TensorMetadata
                        {
                            TensorName = reader.GetString(0),
                            TensorShape = reader.GetString(1),
                            DataType = reader.GetString(2),
                            ElementCount = reader.GetInt64(3),
                            ByteSize = reader.GetInt64(4)
                        };
                    }
                }
            }

            throw new ArgumentException($"Tensor not found matching pattern: {tensorNamePattern}");
        }

        public void Dispose()
        {
            if (_ownsConnection && _connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
