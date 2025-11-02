using System;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;

namespace ModelIngestion.Content;

internal sealed class AtomIngestionRequestBuilder
{
    private readonly AtomIngestionRequest _request = new();
    private string? _hashSource;
    private string? _hashSalt;
    private bool _computeHash = true;

    public AtomIngestionRequestBuilder WithCanonicalText(string? text)
    {
        _request.CanonicalText = text;
        return this;
    }

    public AtomIngestionRequestBuilder WithModality(string modality, string? subtype = null)
    {
        _request.Modality = modality;
        _request.Subtype = subtype;
        return this;
    }

    public AtomIngestionRequestBuilder WithSource(string? sourceType, string? sourceUri = null)
    {
        _request.SourceType = sourceType;
        _request.SourceUri = sourceUri;
        return this;
    }

    public AtomIngestionRequestBuilder WithEmbedding(float[]? embedding, string? embeddingType, int? modelId, string? policyName)
    {
        _request.Embedding = embedding;
        _request.EmbeddingType = embeddingType;
        _request.ModelId = modelId;
        _request.PolicyName = policyName;
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
        _request.Metadata = metadata.Serialize();
        return this;
    }

    public AtomIngestionRequestBuilder WithMetadata(string? metadataJson)
    {
        _request.Metadata = metadataJson;
        return this;
    }

    public AtomIngestionRequest Build()
    {
        if (_hashSource is not null)
        {
            if (_computeHash)
            {
                var hashInput = _hashSalt is null ? _hashSource : _hashSource + _hashSalt;
                _request.HashInput = HashUtility.ComputeSHA256Hash(hashInput);
            }
            else
            {
                _request.HashInput = _hashSource;
            }
        }
        else if (!string.IsNullOrEmpty(_request.CanonicalText))
        {
            _request.HashInput = HashUtility.ComputeSHA256Hash(_request.CanonicalText);
        }
        else
        {
            _request.HashInput ??= Guid.NewGuid().ToString("N");
        }

        return _request;
    }
}
