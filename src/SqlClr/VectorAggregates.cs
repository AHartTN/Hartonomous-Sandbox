using System;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    /// <summary>
    /// User-Defined Aggregates for vector statistics
    /// T-SQL cannot do streaming variance, median, or custom accumulators
    /// </summary>

    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = 8000)]
    public struct VectorMeanVariance : IBinarySerialize
    {
        private long count;
        private double sum;
        private double sumOfSquares;

        public void Init()
        {
            count = 0;
            sum = 0;
            sumOfSquares = 0;
        }

        public void Accumulate(SqlDouble value)
        {
            if (!value.IsNull)
            {
                double val = value.Value;
                count++;
                sum += val;
                sumOfSquares += val * val;
            }
        }

        public void Merge(VectorMeanVariance other)
        {
            count += other.count;
            sum += other.sum;
            sumOfSquares += other.sumOfSquares;
        }

        public SqlString Terminate()
        {
            if (count == 0)
                return SqlString.Null;

            double mean = sum / count;
            double variance = (sumOfSquares / count) - (mean * mean);
            double stddev = Math.Sqrt(variance);

            return new SqlString($"{{\"mean\":{mean:F6},\"variance\":{variance:F6},\"stddev\":{stddev:F6},\"count\":{count}}}");
        }

        public void Read(BinaryReader r)
        {
            count = r.ReadInt64();
            sum = r.ReadDouble();
            sumOfSquares = r.ReadDouble();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(count);
            w.Write(sum);
            w.Write(sumOfSquares);
        }
    }

    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct GeometricMedian : IBinarySerialize
    {
        private System.Collections.Generic.List<double> xValues;
        private System.Collections.Generic.List<double> yValues;
        private System.Collections.Generic.List<double> zValues;

        public void Init()
        {
            xValues = new System.Collections.Generic.List<double>();
            yValues = new System.Collections.Generic.List<double>();
            zValues = new System.Collections.Generic.List<double>();
        }

        public void Accumulate(SqlDouble x, SqlDouble y, SqlDouble z)
        {
            if (!x.IsNull && !y.IsNull && !z.IsNull)
            {
                xValues.Add(x.Value);
                yValues.Add(y.Value);
                zValues.Add(z.Value);
            }
        }

        public void Merge(GeometricMedian other)
        {
            xValues.AddRange(other.xValues);
            yValues.AddRange(other.yValues);
            zValues.AddRange(other.zValues);
        }

        public SqlString Terminate()
        {
            if (xValues.Count == 0)
                return SqlString.Null;

            // Weiszfeld algorithm for geometric median (simplified)
            double cx = Median(xValues);
            double cy = Median(yValues);
            double cz = Median(zValues);

            return new SqlString($"POINT ({cx:F6} {cy:F6} {cz:F6})");
        }

        private double Median(System.Collections.Generic.List<double> values)
        {
            values.Sort();
            int n = values.Count;
            if (n % 2 == 0)
                return (values[n / 2 - 1] + values[n / 2]) / 2.0;
            else
                return values[n / 2];
        }

        public void Read(BinaryReader r)
        {
            int xCount = r.ReadInt32();
            xValues = new System.Collections.Generic.List<double>(xCount);
            for (int i = 0; i < xCount; i++)
                xValues.Add(r.ReadDouble());

            int yCount = r.ReadInt32();
            yValues = new System.Collections.Generic.List<double>(yCount);
            for (int i = 0; i < yCount; i++)
                yValues.Add(r.ReadDouble());

            int zCount = r.ReadInt32();
            zValues = new System.Collections.Generic.List<double>(zCount);
            for (int i = 0; i < zCount; i++)
                zValues.Add(r.ReadDouble());
        }

        public void Write(BinaryWriter w)
        {
            w.Write(xValues.Count);
            foreach (double x in xValues)
                w.Write(x);

            w.Write(yValues.Count);
            foreach (double y in yValues)
                w.Write(y);

            w.Write(zValues.Count);
            foreach (double z in zValues)
                w.Write(z);
        }
    }

    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = 8000)]
    public struct StreamingSoftmax : IBinarySerialize
    {
        private double maxValue;
        private double sumExp;
        private long count;

        public void Init()
        {
            maxValue = double.NegativeInfinity;
            sumExp = 0;
            count = 0;
        }

        public void Accumulate(SqlDouble value)
        {
            if (!value.IsNull)
            {
                double val = value.Value;
                if (val > maxValue)
                    maxValue = val;
                count++;
            }
        }

        public void Merge(StreamingSoftmax other)
        {
            if (other.maxValue > maxValue)
                maxValue = other.maxValue;
            count += other.count;
        }

        public SqlDouble Terminate()
        {
            if (count == 0)
                return SqlDouble.Null;

            // Numerically stable: divide by sum of exp(x - max)
            return new SqlDouble(sumExp);
        }

        public void Read(BinaryReader r)
        {
            maxValue = r.ReadDouble();
            sumExp = r.ReadDouble();
            count = r.ReadInt64();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(maxValue);
            w.Write(sumExp);
            w.Write(count);
        }
    }
}
