using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace Hartonomous.Clr.TensorOperations
{
    /// <summary>
    /// Contains shape-to-model synthesis logic using NetTopologySuite.
    /// </summary>
    public static class ModelSynthesis
    {
        private class AtomComponent
        {
            public long AtomId { get; set; }
            public double Coefficient { get; set; }
        }

        /// <summary>
        /// Synthesizes a new model layer by querying for tensor atoms that intersect a given spatial shape.
        /// </summary>
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static SqlString clr_SynthesizeModelLayer(SqlBytes query_shape, SqlInt64 parent_layer_id)
        {
            if (query_shape.IsNull || parent_layer_id.IsNull)
            {
                return new SqlString(JsonConvert.SerializeObject(new { error = "Query shape and parent layer ID cannot be null." }));
            }

            var components = new List<AtomComponent>();
            float[] total_layer = null;

            try
            {
                using (var conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();
                    var query = @"
                        SELECT ta.TensorAtomId, tac.Coefficient
                        FROM dbo.TensorAtom AS ta
                        JOIN dbo.TensorAtomCoefficient AS tac ON ta.TensorAtomId = tac.TensorAtomId
                        WHERE tac.ParentLayerId = @layerId 
                          AND ta.SpatialSignature.STIntersects(geometry::STGeomFromWKB(@shape, 0)) = 1;";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@layerId", parent_layer_id.Value);
                        cmd.Parameters.AddWithValue("@shape", query_shape.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                components.Add(new AtomComponent
                                {
                                    AtomId = reader.GetInt64(0),
                                    Coefficient = reader.GetDouble(1)
                                });
                            }
                        }
                    }
                }

                foreach (var component in components)
                {
                    SqlBytes payload = TensorDataIO.clr_GetTensorAtomPayload(new SqlInt64(component.AtomId));

                    if (payload.IsNull) continue;

                    float[] atom_float_array = BytesToFloatArray(payload);

                    if (atom_float_array == null) continue;

                    if (total_layer == null)
                    {
                        total_layer = new float[atom_float_array.Length];
                    }
                    else if (total_layer.Length != atom_float_array.Length)
                    {
                        continue;
                    }

                    for (int i = 0; i < total_layer.Length; i++)
                    {
                        total_layer[i] += atom_float_array[i] * (float)component.Coefficient;
                    }
                }

                if (total_layer == null)
                {
                    return new SqlString(JsonConvert.SerializeObject(new { warning = "No intersecting tensor atoms found or their payloads were empty." }));
                }

                return new SqlString(JsonConvert.SerializeObject(total_layer));
            }
            catch (Exception ex)
            {
                return new SqlString(JsonConvert.SerializeObject(new { error = ex.Message, stack_trace = ex.StackTrace }));
            }
        }

        private static float[] BytesToFloatArray(SqlBytes bytes)
        {
            if (bytes.IsNull || bytes.Length == 0)
                return null;

            using (var ms = new MemoryStream(bytes.Value))
            using (var br = new BinaryReader(ms))
            {
                var floats = new List<float>();
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    floats.Add(br.ReadSingle());
                }
                return floats.ToArray();
            }
        }
    }
}
