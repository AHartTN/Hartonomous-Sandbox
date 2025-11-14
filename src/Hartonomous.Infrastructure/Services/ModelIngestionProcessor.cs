using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Coordinates ingestion requests by delegating discovery to the orchestrator and persisting model artifacts.
/// </summary>
public class ModelIngestionProcessor
{
    private readonly ILogger<ModelIngestionProcessor> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly IModelLayerRepository _layerRepository;
    private readonly ModelIngestionOrchestrator _orchestrator;
    private readonly IAtomRepository _atomRepository;
    private readonly IAtomRelationRepository _atomRelationRepository;

    private static readonly JsonSerializerOptions LayerMetadataSerializer = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string ArchitectureSuccessorRelationType = "architecture.successor";

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelIngestionProcessor"/> class.
    /// </summary>
    /// <param name="logger">Logger for operational diagnostics.</param>
    /// <param name="modelRepository">Repository used to persist model definitions.</param>
    /// <param name="layerRepository">Repository used to store model layers.</param>
    /// <param name="orchestrator">Orchestrator responsible for reading model artifacts.</param>
    public ModelIngestionProcessor(
        ILogger<ModelIngestionProcessor> logger,
        IModelRepository modelRepository,
        IModelLayerRepository layerRepository,
        IAtomRepository atomRepository,
        IAtomRelationRepository atomRelationRepository,
        ModelIngestionOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
        _atomRepository = atomRepository ?? throw new ArgumentNullException(nameof(atomRepository));
        _atomRelationRepository = atomRelationRepository ?? throw new ArgumentNullException(nameof(atomRelationRepository));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <summary>
    /// Processes an ingestion request by reading the source model and persisting the resulting metadata and layers.
    /// </summary>
    /// <param name="request">Incoming ingestion request containing model location and overrides.</param>
    /// <param name="cancellationToken">Token that cancels ingestion work.</param>
    /// <returns>A result describing the success state and persisted model information.</returns>
    public async Task<ModelIngestionResult> ProcessAsync(ModelIngestionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _orchestrator.IngestModelAsync(request.ModelPath, cancellationToken);

            if (!string.IsNullOrEmpty(request.CustomName))
            {
                model.ModelName = request.CustomName;
            }

            var savedModel = await _modelRepository.AddAsync(model, cancellationToken).ConfigureAwait(false);

            if (model.ModelLayers is { Count: > 0 })
            {
                var orderedLayers = model.ModelLayers
                    .OrderBy(l => l.LayerIdx)
                    .ToList();

                var layerAtoms = new List<(ModelLayer Layer, Atom Atom)>(orderedLayers.Count);

                foreach (var layer in orderedLayers)
                {
                    layer.ModelId = savedModel.ModelId;
                    var atom = await CreateLayerAtomAsync(savedModel, layer, cancellationToken).ConfigureAwait(false);
                    layer.LayerAtomId = atom.AtomId;
                    layerAtoms.Add((layer, atom));
                }

                await _layerRepository.BulkInsertAsync(orderedLayers, cancellationToken).ConfigureAwait(false);
                savedModel.ModelLayers = orderedLayers;

                await CreateSuccessorRelationsAsync(savedModel, layerAtoms, cancellationToken).ConfigureAwait(false);
            }

            return new ModelIngestionResult
            {
                Success = true,
                ModelId = savedModel.ModelId,
                Model = savedModel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model ingestion processing failed");
            return new ModelIngestionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<Atom> CreateLayerAtomAsync(Model model, ModelLayer layer, CancellationToken cancellationToken)
    {
        var metadataPayload = new
        {
            modelId = model.ModelId,
            modelName = model.ModelName,
            architecture = model.Architecture,
            layerIdx = layer.LayerIdx,
            layerName = layer.LayerName,
            layerType = layer.LayerType,
            tensorShape = layer.TensorShape,
            tensorDtype = layer.TensorDtype,
            parameterCount = layer.ParameterCount,
            quantizationType = layer.QuantizationType,
            quantizationScale = layer.QuantizationScale,
            quantizationZeroPoint = layer.QuantizationZeroPoint,
            cacheHitRate = layer.CacheHitRate,
            avgComputeTimeMs = layer.AvgComputeTimeMs
        };

        var metadata = JsonSerializer.Serialize(metadataPayload, LayerMetadataSerializer);
        var hashSeed = $"{model.ModelId}:{layer.LayerIdx}:{layer.LayerName}:{layer.LayerType}:{layer.ParameterCount}:{layer.TensorShape}";
        var canonicalText = $"{model.ModelName} layer {layer.LayerIdx}:{layer.LayerName ?? layer.LayerType ?? "layer"}";

        var atom = new Atom
        {
            ContentHash = HashUtility.ComputeSHA256Bytes(hashSeed),
            Modality = "model",
            Subtype = "layer",
            SourceType = "model_ingestion",
            SourceUri = model.ModelName,
            CanonicalText = canonicalText,
            Metadata = metadata,
            PayloadLocator = $"model:{model.ModelId}/layer:{layer.LayerIdx}",
            ReferenceCount = 1,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SpatialKey = BuildLayerSpatialPoint(layer)
        };

        return await _atomRepository.AddAsync(atom, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateSuccessorRelationsAsync(
        Model model,
        IReadOnlyList<(ModelLayer Layer, Atom Atom)> layerAtoms,
        CancellationToken cancellationToken)
    {
        if (layerAtoms.Count < 2)
        {
            return;
        }

        for (var i = 0; i < layerAtoms.Count - 1; i++)
        {
            var current = layerAtoms[i];
            var next = layerAtoms[i + 1];

            var relation = new AtomRelation
            {
                SourceAtomId = current.Atom.AtomId,
                TargetAtomId = next.Atom.AtomId,
                RelationType = ArchitectureSuccessorRelationType,
                Weight = 1f,
                SpatialExpression = BuildRelationGeometry(current.Layer, next.Layer),
                Metadata = BuildRelationMetadata(model, current.Layer, next.Layer),
                CreatedAt = DateTime.UtcNow
            };

            await _atomRelationRepository.AddAsync(relation, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Point? BuildLayerSpatialPoint(ModelLayer layer)
    {
        var hasParameterCount = layer.ParameterCount.HasValue;
        var hasTiming = layer.AvgComputeTimeMs.HasValue;

        if (!hasParameterCount && !hasTiming)
        {
            return null;
        }

        var coordinate = new CoordinateZ(
            layer.LayerIdx,
            hasParameterCount ? Convert.ToDouble(layer.ParameterCount!.Value) : 0d,
            hasTiming ? layer.AvgComputeTimeMs!.Value : 0d);

        return new Point(coordinate) { SRID = 0 };
    }

    private static LineString? BuildRelationGeometry(ModelLayer source, ModelLayer target)
    {
        var sourceCoordinate = new CoordinateZ(
            source.LayerIdx,
            source.ParameterCount.HasValue ? Convert.ToDouble(source.ParameterCount.Value) : 0d,
            source.AvgComputeTimeMs ?? 0d);

        var targetCoordinate = new CoordinateZ(
            target.LayerIdx,
            target.ParameterCount.HasValue ? Convert.ToDouble(target.ParameterCount.Value) : 0d,
            target.AvgComputeTimeMs ?? 0d);

        var line = new LineString([sourceCoordinate, targetCoordinate])
        {
            SRID = 0
        };

        return line;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Console ingestion path - trimming not enabled for worker")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Console ingestion path - native AOT not used")]
    private static string BuildRelationMetadata(Model model, ModelLayer source, ModelLayer target)
    {
        var payload = new
        {
            modelId = model.ModelId,
            modelName = model.ModelName,
            architecture = model.Architecture,
            source = new
            {
                layerIdx = source.LayerIdx,
                layerName = source.LayerName,
                layerType = source.LayerType
            },
            target = new
            {
                layerIdx = target.LayerIdx,
                layerName = target.LayerName,
                layerType = target.LayerType
            }
        };

        return JsonSerializer.Serialize(payload, LayerMetadataSerializer);
    }
}
