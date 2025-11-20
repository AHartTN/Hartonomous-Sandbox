namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Audio-specific metadata (sample rate, channels, ID3/Vorbis tags).
/// </summary>
public class AudioMetadata : MediaMetadata
{
    public override string MediaType => "Audio";
    
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitDepth { get; set; }
    public int Bitrate { get; set; }
    
    public int EstimatedBitrate => 
        Bitrate > 0 ? Bitrate : (FileSizeBytes > 0 ? (FileSizeBytes * 8 / 1000) : 0);
}
