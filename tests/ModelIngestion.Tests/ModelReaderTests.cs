using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using ModelIngestion.ModelFormats;
using NetTopologySuite.Geometries;
using Xunit;

namespace ModelIngestion.Tests;

public class ModelReaderTests
{
    [Fact]
    public async Task OnnxModelReader_ReadAsync_ProducesLayersFromInitializers()
    {
        var repository = new TestModelLayerRepository();
        var loader = new TestOnnxLoader(new OnnxModelLoadResult(
            modelName: "test_model",
            graphName: "TestGraph",
            domain: "Transformer",
            producerName: "unit-test",
            description: "synthetic",
            inputs: new[] { "input" },
            outputs: new[] { "output" },
            initializers: new[]
            {
                new OnnxInitializerInfo(
                    "dense_weight",
                    1,
                    new long[] { 2, 2 },
                    new[] { 0.1f, 0.2f, 0.3f, 0.4f },
                    Array.Empty<double>(),
                    Array.Empty<byte>(),
                    "attention weights")
            }));

        var reader = new OnnxModelReader(NullLogger<OnnxModelReader>.Instance, repository, loader);

        var model = await reader.ReadAsync("synthetic.onnx");

        Assert.Equal("ONNX", model.ModelType);
        Assert.Single(model.Layers);
        Assert.Equal(4, model.ParameterCount);

        var layer = model.Layers.Single();
        Assert.Equal("dense_weight", layer.LayerName);
        Assert.Equal("float32", layer.TensorDtype);
        Assert.Equal("[2,2]", layer.TensorShape);
        Assert.Equal(4, layer.ParameterCount);
        Assert.NotNull(layer.WeightsGeometry);
        Assert.Equal(4, repository.LastGeometry?.NumPoints);
    }

    [Fact]
    public async Task SafetensorsModelReader_ReadAsync_ParsesHeaderAndWeights()
    {
        using var tempFile = new TempFile("test_model.safetensors");
        await SafetensorsFixture.WriteAsync(tempFile.Path);

        var repository = new TestModelLayerRepository();
        var reader = new SafetensorsModelReader(NullLogger<SafetensorsModelReader>.Instance, repository);

        var model = await reader.ReadAsync(tempFile.Path);

    Assert.Equal(Path.GetFileNameWithoutExtension(tempFile.Path), model.ModelName);
        Assert.Equal("Safetensors", model.ModelType);
        Assert.Single(model.Layers);

        var layer = model.Layers.Single();
        Assert.Equal("decoder.weight", layer.LayerName);
        Assert.Equal("f32", layer.TensorDtype);
        Assert.Equal(4, layer.ParameterCount);
        Assert.NotNull(layer.WeightsGeometry);
        Assert.Equal(4, repository.LastGeometry?.NumPoints);
    }

    [Fact]
    public async Task PyTorchModelReader_ReadAsync_UsesLoaderAndProducesMetadata()
    {
        var parameters = new List<PyTorchParameterInfo>
        {
            new("encoder.layers.0.attention.q_proj.weight", new[] { 0.1f, 0.2f, 0.3f, 0.4f }, new long[] { 2, 2 }, "float32", true),
            new("encoder.layers.0.ffn.linear.weight", new[] { 1f, 2f, 3f }, new long[] { 3 }, "float32", false)
        };

        var loader = new TestPyTorchLoader(new PyTorchModelLoadResult(parameters, new Dictionary<string, object>()));
        var repository = new TestModelLayerRepository();

        var reader = new PyTorchModelReader(
            NullLogger<PyTorchModelReader>.Instance,
            repository,
            loader);

        var model = await reader.ReadAsync("mock_model.pth");

        Assert.Equal("mock_model", model.ModelName);
        Assert.Equal("PyTorch JIT", model.ModelType);
        Assert.Equal(2, model.Layers.Count);
        Assert.Equal(7, model.ParameterCount);
        Assert.All(model.Layers, l => Assert.NotNull(l.WeightsGeometry));
        Assert.Equal("Transformer", model.Architecture);
    }

    private sealed class TestModelLayerRepository : IModelLayerRepository
    {
        public LineString? LastGeometry { get; private set; }

        public Task<ModelLayer?> GetByIdAsync(long layerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ModelLayer>> GetByModelAsync(int modelId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ModelLayer> AddAsync(ModelLayer layer, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task BulkInsertAsync(IEnumerable<ModelLayer> layers, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(ModelLayer layer, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeleteAsync(long layerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ModelLayer>> GetLayersByWeightRangeAsync(int modelId, double minValue, double maxValue, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ModelLayer>> GetLayersByImportanceAsync(int modelId, double minImportance, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public float[] ExtractWeightsFromGeometry(LineString geometry)
        {
            var result = new float[geometry.NumPoints];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (float)geometry.GetCoordinateN(i).Y;
            }

            return result;
        }

        public LineString CreateGeometryFromWeights(float[] weights, float[]? importanceScores = null, float[]? temporalMetadata = null)
        {
            var coordinates = new Coordinate[weights.Length];
            for (var i = 0; i < weights.Length; i++)
            {
                var z = importanceScores is { Length: > 0 } && i < importanceScores.Length ? importanceScores[i] : 0d;
                var m = temporalMetadata is { Length: > 0 } && i < temporalMetadata.Length ? temporalMetadata[i] : 0d;
                coordinates[i] = new CoordinateZM(i, weights[i], z, m);
            }

            LastGeometry = new LineString(coordinates);
            return LastGeometry;
        }
    }

    private sealed class TestPyTorchLoader : IPyTorchModelLoader
    {
        private readonly PyTorchModelLoadResult _result;

        public TestPyTorchLoader(PyTorchModelLoadResult result)
        {
            _result = result;
        }

        public PyTorchModelLoadResult Load(string modelPath, CancellationToken cancellationToken = default)
        {
            return _result;
        }
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile(string fileName, byte[]? contents = null)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
            if (contents is not null)
            {
                File.WriteAllBytes(Path, contents);
            }
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private static class SafetensorsFixture
    {
        public static Task WriteAsync(string path)
        {
            var header = new Dictionary<string, object>
            {
                ["__metadata__"] = new Dictionary<string, string>
                {
                    ["format"] = "Transformer",
                    ["architecture"] = "Encoder"
                },
                ["decoder.weight"] = new Dictionary<string, object>
                {
                    ["dtype"] = "f32",
                    ["shape"] = new[] { 2, 2 },
                    ["data_offsets"] = new[] { 0, 16 }
                }
            };

            var headerJson = JsonSerializer.Serialize(header);
            var headerBytes = Encoding.UTF8.GetBytes(headerJson);

            using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write((long)headerBytes.Length);
            writer.Write(headerBytes);

            // Write 4 float32 values (16 bytes) matching offsets
            var weights = new[] { 1.0f, 2.0f, 3.0f, 4.0f };
            foreach (var value in weights)
            {
                writer.Write(value);
            }

            writer.Flush();
            return Task.CompletedTask;
        }
    }

    private sealed class TestOnnxLoader : IOnnxModelLoader
    {
        private readonly OnnxModelLoadResult _result;

        public TestOnnxLoader(OnnxModelLoadResult result)
        {
            _result = result;
        }

        public OnnxModelLoadResult Load(string modelPath, CancellationToken cancellationToken = default)
        {
            return _result;
        }
    }
}
