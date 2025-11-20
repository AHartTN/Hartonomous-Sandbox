using BenchmarkDotNet.Attributes;

namespace Hartonomous.Core.Performance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class SampleBenchmark
{
    private readonly List<int> _data;

    public SampleBenchmark()
    {
        _data = Enumerable.Range(0, 1000).ToList();
    }

    [Benchmark(Baseline = true)]
    public int SumWithForLoop()
    {
        var sum = 0;
        for (int i = 0; i < _data.Count; i++)
        {
            sum += _data[i];
        }
        return sum;
    }

    [Benchmark]
    public int SumWithLinq()
    {
        return _data.Sum();
    }

    [Benchmark]
    public int SumWithForeach()
    {
        var sum = 0;
        foreach (var item in _data)
        {
            sum += item;
        }
        return sum;
    }

    [Benchmark]
    public int SumWithAggregate()
    {
        return _data.Aggregate(0, (acc, x) => acc + x);
    }
}
