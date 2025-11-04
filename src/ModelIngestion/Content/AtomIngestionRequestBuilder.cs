using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;

namespace ModelIngestion.Content;

internal sealed class AtomIngestionRequestBuilder
{
    private string? _canonicalText;
    private string? _modality;
    private string? _subtype;
    private string? _sourceType;
    private string? _sourceUri;
    private float[]? _embedding;
    private string? _embeddingType;
    private int? _modelId;
    private string? _policyName;
    private string? _metadata;
    private string? _payloadLocator;
    private List<AtomComponentDescriptor>? _components;

    private string? _hashSource;
    private string? _hashSalt;
    private bool _computeHash = true;

    public AtomIngestionRequestBuilder WithCanonicalText(string? text)
    {
        _canonicalText = text;
        return this;
    }

    public AtomIngestionRequestBuilder WithModality(string modality, string? subtype = null)
    {
        _modality = modality;
        _subtype = subtype;
        return this;
    }

    public AtomIngestionRequestBuilder WithSource(string? sourceType, string? sourceUri = null)
    {
        _sourceType = sourceType;
        _sourceUri = sourceUri;
        return this;
    }

    public AtomIngestionRequestBuilder WithPayloadLocator(string? locator)
    {
        _payloadLocator = locator;
        return this;
    }

    public AtomIngestionRequestBuilder WithComponents(IEnumerable<AtomComponentDescriptor>? components)
    {
        if (components is null)
        {
            _components = null;
            return this;
        }

        _components = components
            .Where(component => component is not null)
            .Select(component => new AtomComponentDescriptor(component.AtomId, component.Quantity))
            .ToList();

        return this;
    }

    public AtomIngestionRequestBuilder WithEmbedding(float[]? embedding, string? embeddingType, int? modelId, string? policyName)
    {
        _embedding = embedding;
        _embeddingType = embeddingType;
        _modelId = modelId;
        _policyName = policyName;
        return this;
    }

    public AtomIngestionRequestBuilder WithHash(string value, string? salt = null, bool compute = true)
    {
        _hashSource = value;
        _hashSalt = salt;
        _computeHash = compute;
        return this;
    }

    public AtomIngestionRequestBuilder WithPrecomputedHash(string hash)
    {
        _hashSource = hash;
        _hashSalt = null;
        _computeHash = false;
        return this;
    }

    public AtomIngestionRequestBuilder WithMetadata(MetadataEnvelope metadata)
    {
        _metadata = metadata.Serialize();
        return this;
    }

    public AtomIngestionRequestBuilder WithMetadata(string? metadataJson)
    {
        _metadata = metadataJson;
        return this;
    }

    public AtomIngestionRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_modality))
        {
            throw new InvalidOperationException("Modality must be specified for atom ingestion.");
        }

        string hashInput;

        if (_hashSource is not null)
        {
            hashInput = _computeHash
                ? HashUtility.ComputeSHA256Hash(_hashSalt is null ? _hashSource : _hashSource + _hashSalt)
                : _hashSource;
        }
        else if (!string.IsNullOrWhiteSpace(_canonicalText))
        {
            hashInput = HashUtility.ComputeSHA256Hash(_canonicalText);
        }
        else
        {
            hashInput = Guid.NewGuid().ToString("N");
        }

        return new AtomIngestionRequest
        {
            HashInput = hashInput,
            Modality = _modality,
            Subtype = _subtype,
            SourceType = _sourceType,
            SourceUri = _sourceUri,
            CanonicalText = _canonicalText,
            Metadata = _metadata,
            PayloadLocator = _payloadLocator,
            Embedding = _embedding,
            EmbeddingType = _embeddingType ?? "default",
            ModelId = _modelId,
            PolicyName = _policyName ?? "default",
            Components = _components is { Count: > 0 }
                ? _components.ToArray()
                : Array.Empty<AtomComponentDescriptor>()
        };
    }
}
