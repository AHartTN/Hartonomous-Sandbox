using System;
using System.Collections.Generic;
using System.Globalization;

namespace ModelIngestion.Content;

internal sealed class MetadataEnvelope
{
    private readonly Dictionary<string, object?> _values;

    public MetadataEnvelope(IDictionary<string, string>? seed = null)
    {
        _values = seed != null
            ? new Dictionary<string, object?>(seed.Count, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (seed != null)
        {
            foreach (var kvp in seed)
            {
                _values[kvp.Key] = kvp.Value;
            }
        }
    }

    public MetadataEnvelope(IDictionary<string, object?>? seed, bool adoptCaseInsensitive)
    {
        _values = seed != null
            ? new Dictionary<string, object?>(seed, adoptCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            : new Dictionary<string, object?>(adoptCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    public MetadataEnvelope Set(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return this;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            _values.Remove(key);
        }
        else
        {
            _values[key] = value;
        }

        return this;
    }

    public MetadataEnvelope Set(string key, int value)
        => Set(key, value.ToString(CultureInfo.InvariantCulture));

    public MetadataEnvelope Set(string key, long value)
        => Set(key, value.ToString(CultureInfo.InvariantCulture));

    public MetadataEnvelope Set(string key, double value)
        => Set(key, value.ToString(CultureInfo.InvariantCulture));

    public MetadataEnvelope SetRaw(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return this;
        }

        if (value is null)
        {
            _values.Remove(key);
            return this;
        }

        _values[key] = value;
        return this;
    }

    public IDictionary<string, string> AsStrings()
    {
        var result = new Dictionary<string, string>(_values.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in _values)
        {
            result[key] = value switch
            {
                null => string.Empty,
                string s => s,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        return result;
    }

    public IDictionary<string, object?> AsDictionary() => _values;

    public string? Serialize() => MetadataUtilities.Serialize(_values);
}
