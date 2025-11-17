using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Transactions;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Provides CLR functions for reading and writing tensor payload data
    /// to the FILESTREAM-enabled dbo.TensorAtomPayloads table.
    /// These functions handle the specifics of FILESTREAM transactions.
    /// </summary>
    public static class TensorDataIO
    {
        /// <summary>
        /// Stores a raw tensor payload (e.g., a vector from SVD) into the
        /// dbo.TensorAtomPayloads table using the FILESTREAM API.
        /// </summary>
        /// <param name="tensorAtomId">The ID of the parent TensorAtom.</param>
        /// <param name="payload">The raw binary data of the tensor segment.</param>
        [SqlProcedure]
        public static void clr_StoreTensorAtomPayload(SqlInt64 tensorAtomId, SqlBytes payload)
        {
            if (tensorAtomId.IsNull || payload.IsNull)
            {
                return;
            }

            try
            {
                // The FILESTREAM API requires an explicit transaction context.
                using (var ts = new TransactionScope())
                using (var conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();

                    // Step 1: Insert the metadata row and get the transaction context.
                    string sqlInsert = @"
                        INSERT INTO dbo.TensorAtomPayloads (TensorAtomId, Payload)
                        VALUES (@atomId, 0x);
                        SELECT Payload.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT()
                        FROM dbo.TensorAtomPayloads
                        WHERE PayloadId = SCOPE_IDENTITY();";

                    string filePath;
                    byte[] txContext;

                    using (var cmd = new SqlCommand(sqlInsert, conn))
                    {
                        cmd.Parameters.AddWithValue("@atomId", tensorAtomId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            filePath = reader.GetString(0);
                            txContext = (byte[])reader.GetValue(1);
                        }
                    }

                    // Step 2: Use the SqlFileStream API to write the data.
                    using (var fs = new SqlFileStream(filePath, txContext, System.IO.FileAccess.Write))
                    {
                        payload.Stream.CopyTo(fs);
                    }

                    // If all operations succeed, complete the transaction.
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                // Log the error to the SQL Server error log for debugging.
                SqlContext.Pipe.Send("Error in clr_StoreTensorAtomPayload: " + ex.Message);
                // Re-throw the exception to ensure the transaction is rolled back.
                throw;
            }
        }

        /// <summary>
        /// Retrieves a raw tensor payload from the dbo.TensorAtomPayloads table
        /// using the FILESTREAM API.
        /// </summary>
        /// <param name="tensorAtomId">The ID of the parent TensorAtom.</param>
        /// <returns>The raw binary data of the tensor segment as SqlBytes.</returns>
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static SqlBytes clr_GetTensorAtomPayload(SqlInt64 tensorAtomId)
        {
            if (tensorAtomId.IsNull)
            {
                return SqlBytes.Null;
            }

            try
            {
                SqlBytes result;

                using (var ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                using (var conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();

                    string sqlSelect = @"
                        SELECT Payload.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT()
                        FROM dbo.TensorAtomPayloads
                        WHERE TensorAtomId = @atomId;";

                    string filePath;
                    byte[] txContext;

                    using (var cmd = new SqlCommand(sqlSelect, conn))
                    {
                        cmd.Parameters.AddWithValue("@atomId", tensorAtomId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                // If no row is found, return null.
                                return SqlBytes.Null;
                            }
                            filePath = reader.GetString(0);
                            txContext = (byte[])reader.GetValue(1);
                        }
                    }

                    // Use the SqlFileStream API to read the data.
                    using (var fs = new SqlFileStream(filePath, txContext, System.IO.FileAccess.Read))
                    using (var ms = new MemoryStream())
                    {
                        fs.CopyTo(ms);
                        result = new SqlBytes(ms.ToArray());
                    }
                    
                    ts.Complete();
                }

                return result;
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send("Error in clr_GetTensorAtomPayload: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Converts a JSON string representing an array of floats into a VARBINARY format.
        /// </summary>
        /// <param name="jsonFloatArray">The JSON string, e.g., "[1.0, 2.5, -3.0]".</param>
        /// <returns>A SqlBytes object containing the binary representation of the float array.</returns>
        [SqlFunction(DataAccess = DataAccessKind.None)]
        public static SqlBytes clr_JsonFloatArrayToBytes(SqlString jsonFloatArray)
        {
            if (jsonFloatArray.IsNull)
            {
                return SqlBytes.Null;
            }

            try
            {
                var floats = Newtonsoft.Json.JsonConvert.DeserializeObject<float[]>(jsonFloatArray.Value);
                if (floats == null)
                {
                    return SqlBytes.Null;
                }

                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    foreach (var f in floats)
                    {
                        bw.Write(f);
                    }
                    return new SqlBytes(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send("Error in clr_JsonFloatArrayToBytes: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Converts a VARBINARY containing a float array into a JSON string.
        /// This is the inverse of clr_JsonFloatArrayToBytes.
        /// </summary>
        /// <param name="bytes">The binary data containing a sequence of 4-byte floats.</param>
        /// <returns>A JSON string representing the float array, e.g., "[1.0, 2.5, -3.0]".</returns>
        [SqlFunction(DataAccess = DataAccessKind.None)]
        public static SqlString clr_BytesToFloatArrayJson(SqlBytes bytes)
        {
            if (bytes.IsNull || bytes.Length == 0)
            {
                return SqlString.Null;
            }

            try
            {
                // Each float is 4 bytes
                int floatCount = (int)(bytes.Length / 4);
                float[] floats = new float[floatCount];

                using (var ms = new MemoryStream(bytes.Value))
                using (var br = new BinaryReader(ms))
                {
                    for (int i = 0; i < floatCount; i++)
                    {
                        floats[i] = br.ReadSingle();
                    }
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(floats);
                return new SqlString(json);
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send("Error in clr_BytesToFloatArrayJson: " + ex.Message);
                throw;
            }
        }
    }
}

