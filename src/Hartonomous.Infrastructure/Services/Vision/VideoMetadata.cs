namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Video-specific metadata (codec, frame rate, tracks).
/// </summary>
public class VideoMetadata : MediaMetadata
{
    public override string MediaType => "Video";
    
    public string Container { get; set; } = "Unknown";
    public double FrameRate { get; set; }
    
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int? VideoBitrate { get; set; }
    public int? AudioBitrate { get; set; }
    
    public bool HasVideo { get; set; }
    public bool HasAudio { get; set; }
    
    public int EstimatedBitrate => 
        DurationSeconds > 0 ? (int)((FileSizeBytes * 8) / DurationSeconds) : 0;
}
