using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Hartonomous.Clr.Contracts;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// SQL CLR implementation of ITensorProvider using NetTopologySuite.
    /// </summary>
    public class SqlTensorProvider : ITensorProvider
    {
        private static readonly SqlServerBytesReader _geometryReader = new SqlServerBytesReader();
        private readonly SqlConnection _connection;
        private readonly bool _ownsConnection;

        public SqlTensorProvider()
        {
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
                        var geometryObj = reader.GetValue(0);
                        var elementCount = reader.GetInt64(1);

                        if (geometryObj != null && geometryObj != DBNull.Value)
                        {
                            try
                            {
                                byte[] geometryBytes = null;
                                
                                if (geometryObj is byte[] bytes)
                                {
                                    geometryBytes = bytes;
                                }
                                else if (geometryObj is System.Data.SqlTypes.SqlBytes sqlBytes && !sqlBytes.IsNull)
                                {
                                    geometryBytes = sqlBytes.Value;
                                }

                                if (geometryBytes != null)
                                {
                                    var geometry = _geometryReader.Read(geometryBytes);
                                    
                                    if (geometry != null && !geometry.IsEmpty && geometry is LineString lineString)
                                    {
                                        int pointCount = Math.Min((int)elementCount, maxElements);
                                        pointCount = Math.Min(pointCount, lineString.NumPoints);

                                        // Extract weights from LineString coordinates (Y values)
                                        for (int i = 0; i < pointCount; i++)
                                        {
                                            var coord = lineString.GetCoordinateN(i);
                                            if (coord != null && !double.IsNaN(coord.Y))
                                            {
                                                weights.Add((float)coord.Y);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Skip invalid geometries
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
