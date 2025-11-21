using System;
using System.Collections.Generic;
using System.IO;
using Hartonomous.Clr.Models;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// High-performance serialization utilities for CLR aggregates.
    /// Uses Buffer.BlockCopy for 5-10x faster array serialization than per-element writes.
    /// Extended with Dictionary, List, and TensorInfo serialization.
    /// </summary>
    public static class BinarySerializationHelpers
    {
        /// <summary>
        /// Write float array to binary stream using fast block copy.
        /// Handles null arrays and empty arrays efficiently.
        /// </summary>
        public static void WriteFloatArray(this BinaryWriter writer, float[]? array)
        {
            if (array == null)
            {
                writer.Write(-1); // Null marker
                return;
            }

            writer.Write(array.Length);
            
            if (array.Length == 0)
            {
                return; // No data to write
            }

            // Fast bulk copy: ~10x faster than per-element Write()
            byte[] buffer = new byte[array.Length * sizeof(float)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            writer.Write(buffer);
        }

        /// <summary>
        /// Read float array from binary stream using fast block copy.
        /// Returns null if stream contains null marker.
        /// </summary>
        public static float[]? ReadFloatArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            
            if (length < 0)
            {
                return null; // Null marker
            }

            if (length == 0)
            {
                return Array.Empty<float>(); // Empty array
            }

            // Fast bulk copy: ~10x faster than per-element Read()
            byte[] buffer = reader.ReadBytes(length * sizeof(float));
            float[] result = new float[length];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }

        /// <summary>
        /// Write double array to binary stream using fast block copy.
        /// </summary>
        public static void WriteDoubleArray(this BinaryWriter writer, double[] array)
        {
            if (array == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(array.Length);
            
            if (array.Length == 0)
            {
                return;
            }

            byte[] buffer = new byte[array.Length * sizeof(double)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            writer.Write(buffer);
        }

        /// <summary>
        /// Read double array from binary stream using fast block copy.
        /// </summary>
        public static double[]? ReadDoubleArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            
            if (length < 0)
            {
                return null;
            }

            if (length == 0)
            {
                return Array.Empty<double>();
            }

            byte[] buffer = reader.ReadBytes(length * sizeof(double));
            double[] result = new double[length];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }

        /// <summary>
        /// Write int array to binary stream using fast block copy.
        /// </summary>
        public static void WriteIntArray(this BinaryWriter writer, int[] array)
        {
            if (array == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(array.Length);
            
            if (array.Length == 0)
            {
                return;
            }

            byte[] buffer = new byte[array.Length * sizeof(int)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            writer.Write(buffer);
        }

        /// <summary>
        /// Read int array from binary stream using fast block copy.
        /// </summary>
        public static int[]? ReadIntArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            
            if (length < 0)
            {
                return null;
            }

            if (length == 0)
            {
                return Array.Empty<int>();
            }

            byte[] buffer = reader.ReadBytes(length * sizeof(int));
            int[] result = new int[length];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }

        /// <summary>
        /// Write long array to binary stream using fast block copy.
        /// </summary>
        public static void WriteLongArray(this BinaryWriter writer, long[] array)
        {
            if (array == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(array.Length);
            
            if (array.Length == 0)
            {
                return;
            }

            byte[] buffer = new byte[array.Length * sizeof(long)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            writer.Write(buffer);
        }

        /// <summary>
        /// Read long array from binary stream using fast block copy.
        /// </summary>
        public static long[]? ReadLongArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            
            if (length < 0)
            {
                return null;
            }

            if (length == 0)
            {
                return Array.Empty<long>();
            }

            byte[] buffer = reader.ReadBytes(length * sizeof(long));
            long[] result = new long[length];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }

        #region Dictionary Serialization

        /// <summary>
        /// Write Dictionary&lt;string, string&gt; to binary stream.
        /// </summary>
        public static void WriteDictionary(this BinaryWriter writer, Dictionary<string, string>? dict)
        {
            if (dict == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(dict.Count);
            foreach (var kvp in dict)
            {
                writer.Write(kvp.Key ?? string.Empty);
                writer.Write(kvp.Value ?? string.Empty);
            }
        }

        /// <summary>
        /// Read Dictionary&lt;string, string&gt; from binary stream.
        /// </summary>
        public static Dictionary<string, string>? ReadDictionary(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0)
                return null;

            var dict = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                string value = reader.ReadString();
                dict[key] = value;
            }
            return dict;
        }

        /// <summary>
        /// Write Dictionary&lt;string, float&gt; to binary stream.
        /// </summary>
        public static void WriteDictionaryFloat(this BinaryWriter writer, Dictionary<string, float>? dict)
        {
            if (dict == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(dict.Count);
            foreach (var kvp in dict)
            {
                writer.Write(kvp.Key ?? string.Empty);
                writer.Write(kvp.Value);
            }
        }

        /// <summary>
        /// Read Dictionary&lt;string, float&gt; from binary stream.
        /// </summary>
        public static Dictionary<string, float>? ReadDictionaryFloat(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0)
                return null;

            var dict = new Dictionary<string, float>(count);
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                float value = reader.ReadSingle();
                dict[key] = value;
            }
            return dict;
        }

        #endregion

        #region List Serialization

        /// <summary>
        /// Write List&lt;string&gt; to binary stream.
        /// </summary>
        public static void WriteStringList(this BinaryWriter writer, List<string>? list)
        {
            if (list == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(list.Count);
            foreach (var item in list)
            {
                writer.Write(item ?? string.Empty);
            }
        }

        /// <summary>
        /// Read List&lt;string&gt; from binary stream.
        /// </summary>
        public static List<string>? ReadStringList(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0)
                return null;

            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }
            return list;
        }

        /// <summary>
        /// Write List&lt;float[]&gt; to binary stream (jagged array).
        /// </summary>
        public static void WriteFloatListArray(this BinaryWriter writer, List<float[]>? list)
        {
            if (list == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(list.Count);
            foreach (var array in list)
            {
                writer.WriteFloatArray(array);
            }
        }

        /// <summary>
        /// Read List&lt;float[]&gt; from binary stream (jagged array).
        /// </summary>
        public static List<float[]>? ReadFloatListArray(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0)
                return null;

            var list = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                var array = reader.ReadFloatArray();
                if (array != null)
                    list.Add(array);
            }
            return list;
        }

        #endregion

        #region TensorInfo Serialization

        /// <summary>
        /// Write TensorInfo to binary stream.
        /// </summary>
        public static void WriteTensorInfo(this BinaryWriter writer, TensorInfo tensor)
        {
            writer.Write(tensor.Name ?? string.Empty);
            writer.Write((int)tensor.Dtype);
            writer.Write((int)tensor.Quantization);
            writer.WriteLongArray(tensor.Shape);
            writer.Write(tensor.ElementCount);
            writer.Write(tensor.DataOffset);
            writer.Write(tensor.DataSize);
            writer.Write(tensor.LayerIndex);
            writer.Write((int)tensor.LayerType);
        }

        /// <summary>
        /// Read TensorInfo from binary stream.
        /// </summary>
        public static TensorInfo ReadTensorInfo(this BinaryReader reader)
        {
            var name = reader.ReadString();
            var dtype = (Enums.TensorDtype)reader.ReadInt32();
            var quantization = (Enums.QuantizationType)reader.ReadInt32();
            var shape = reader.ReadLongArray();
            var elementCount = reader.ReadInt64();
            var dataOffset = reader.ReadInt64();
            var dataSize = reader.ReadInt64();
            var layerIndex = reader.ReadInt32();
            var layerType = (Enums.LayerType)reader.ReadInt32();

            return new TensorInfo
            {
                Name = name ?? string.Empty,
                Dtype = dtype,
                Quantization = quantization,
                Shape = shape,
                ElementCount = elementCount,
                DataOffset = dataOffset,
                DataSize = dataSize,
                LayerIndex = layerIndex,
                LayerType = layerType
            };
        }

        /// <summary>
        /// Write TensorInfo array to binary stream.
        /// </summary>
        public static void WriteTensorInfoArray(this BinaryWriter writer, TensorInfo[]? array)
        {
            if (array == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(array.Length);
            foreach (var tensor in array)
            {
                writer.WriteTensorInfo(tensor);
            }
        }

        /// <summary>
        /// Read TensorInfo array from binary stream.
        /// </summary>
        public static TensorInfo[]? ReadTensorInfoArray(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0)
                return null;

            var array = new TensorInfo[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = reader.ReadTensorInfo();
            }
            return array;
        }

        #endregion
    }
}
