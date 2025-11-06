using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a generated content stream (audio, video, image sequences) stored via FILESTREAM.
/// Enables efficient streaming of large generated outputs without loading entire payloads into memory.
/// </summary>
public class GenerationStream
{
    /// <summary>
    /// Gets or sets the unique identifier for the generation stream.
    /// </summary>
    public Guid StreamId { get; set; }

    /// <summary>
    /// Gets or sets the scope or context of the generation (e.g., 'user_session', 'batch_job', 'pipeline_run').
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the name or identifier of the model used for generation.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the stream was created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the binary stream data stored via FILESTREAM.
    /// Contains the generated content in its native format (e.g., WAV, MP4, PNG sequence).
    /// </summary>
    public byte[] Stream { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the size of the payload in bytes.
    /// Automatically computed based on the Stream length.
    /// </summary>
    public long PayloadSizeBytes { get; private set; }
}
