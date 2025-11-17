using System;
using System.Collections;
using System.Data.SqlTypes;
using System.IO;
using Hartonomous.Clr.ModelParsers;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Main dispatcher for model weight extraction. This class contains the
    /// primary SQL CLR function that delegates parsing to the appropriate parser.
    /// </summary>
    public static class ModelWeightExtractor
    {
        [SqlFunction(
            Name = "clr_ExtractModelWeights",
            FillRowMethodName = "FillWeightRow",
            TableDefinition = "TensorName nvarchar(255), LayerIndex int, WeightIndex bigint, WeightValue real"
        )]
        public static IEnumerable ExtractModelWeights(SqlString modelFormat, SqlBytes modelData)
        {
            if (modelFormat.IsNull || modelData.IsNull)
            {
                yield break;
            }

            string format = modelFormat.Value.ToLowerInvariant();
            using (var stream = new MemoryStream(modelData.Buffer))
            {
                if (format == "gguf")
                {
                    foreach (var row in GGUFParser.Parse(stream))
                    {
                        yield return row;
                    }
                }
                else if (format == "safetensors")
                {
                    foreach (var row in SafeTensorsParser.Parse(stream))
                    {
                        yield return row;
                    }
                }
                // Add other formats here
            }
        }

        public static void FillWeightRow(object row, out SqlString tensorName, out SqlInt32 layerIndex, out SqlInt64 weightIndex, out SqlSingle weightValue)
        {
            var values = (object[])row;
            tensorName = new SqlString((string)values[0]);
            layerIndex = new SqlInt32((int)values[1]);
            weightIndex = new SqlInt64((long)values[2]);
            weightValue = new SqlSingle(Convert.ToSingle(values[3]));
        }
    }
}
