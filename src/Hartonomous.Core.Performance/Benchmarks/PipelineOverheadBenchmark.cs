using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Pipelines;
using Hartonomous.Core.Pipelines.Ingestion;
using System.Threading.Channels;

namespace Hartonomous.Core.Performance.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PipelineOverheadBenchmark
{
    private Channel<AtomIngestionPipelineRequest>? _channel;
    private IPipeline<AtomIngestionPipelineRequest, AtomIngestionPipelineRequest>? _pipeline;
    private AtomIngestionPipelineRequest _request = null!;
    private IPipelineContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        _channel = Channel.CreateBounded<AtomIngestionPipelineRequest>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _pipeline = PipelineBuilder<AtomIngestionPipelineRequest, AtomIngestionPipelineRequest>
            .Create("benchmark", null, null)
            .AddStep("passthrough", (req, ctx, ct) => Task.FromResult(req))
            .Build();

        _request = new AtomIngestionPipelineRequest
        {
            HashInput = "benchmark test content",
            Modality = Modality.Text.ToJsonString(),
            Subtype = "plain",
            CanonicalText = "Benchmark test atom",
            SourceUri = "benchmark://test",
            SourceType = "benchmark"
        };

        _context = PipelineContext.Create(null, "benchmark");
    }

    [Benchmark(Baseline = true)]
    public async Task DirectMethodCall()
    {
        var result = _request;
        await Task.CompletedTask;
    }

    [Benchmark]
    public async Task PipelineExecution()
    {
        var result = await _pipeline!.ExecuteAsync(_request, _context, CancellationToken.None);
    }

    [Benchmark]
    public async Task ChannelWriteRead()
    {
        await _channel!.Writer.WriteAsync(_request);
        var result = await _channel.Reader.ReadAsync();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<PipelineOverheadBenchmark>();
    }
}
