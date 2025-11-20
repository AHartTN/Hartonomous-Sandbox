using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Hartonomous.Infrastructure.Services.Vision.BinaryReaderHelper;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Extract metadata from audio files (MP3, FLAC, WAV, AAC, OGG Vorbis).
/// Parses codec info, sample rate, channels, bit depth, ID3/Vorbis tags.
/// Uses shared BinaryReaderHelper utilities.
/// </summary>
public static class AudioMetadataExtractor
{
    public static AudioMetadata ExtractMetadata(byte[] audioData)
    {
        var metadata = new AudioMetadata
        {
            FileSizeBytes = audioData.Length
        };
        
        // Detect format
        if (IsMp3(audioData))
        {
            metadata.Format = "MP3";
            ExtractMp3Metadata(audioData, metadata);
        }
        else if (IsFlac(audioData))
        {
            metadata.Format = "FLAC";
            ExtractFlacMetadata(audioData, metadata);
        }
        else if (IsWav(audioData))
        {
            metadata.Format = "WAV";
            ExtractWavMetadata(audioData, metadata);
        }
        else if (IsAac(audioData))
        {
            metadata.Format = "AAC";
            ExtractAacMetadata(audioData, metadata);
        }
        else if (IsOgg(audioData))
        {
            metadata.Format = "OGG Vorbis";
            ExtractOggMetadata(audioData, metadata);
        }
        else if (IsM4a(audioData))
        {
            metadata.Format = "M4A";
            ExtractM4aMetadata(audioData, metadata);
        }
        
        return metadata;
    }

    private static bool IsMp3(byte[] data) => 
        data.Length >= 3 && (
            (data[0] == 0xFF && (data[1] & 0xE0) == 0xE0) || // MPEG frame sync
            (data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33) // ID3v2 header "ID3"
        );

    private static bool IsFlac(byte[] data) => 
        data.Length >= 4 && data[0] == 0x66 && data[1] == 0x4C && data[2] == 0x61 && data[3] == 0x43; // "fLaC"

    private static bool IsWav(byte[] data) => 
        data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
        data[8] == 0x57 && data[9] == 0x41 && data[10] == 0x56 && data[11] == 0x45; // "RIFF" + "WAVE"

    private static bool IsAac(byte[] data) => 
        data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xF6) == 0xF0; // ADTS header

    private static bool IsOgg(byte[] data) => 
        data.Length >= 4 && data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53; // "OggS"

    private static bool IsM4a(byte[] data) => 
        data.Length >= 12 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70 &&
        (data[8] == 0x4D || data[8] == 0x69); // "ftyp" + "M4A " or "isom"

    private static void ExtractMp3Metadata(byte[] data, AudioMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Check for ID3v2 tag
        if (data.Length >= 10 && data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
        {
            ParseId3v2Tag(ms, metadata);
        }
        
        // Find first MPEG audio frame
        while (ms.Position < ms.Length - 4)
        {
            var header = new byte[4];
            ReadExact(ms, header, 0, 4);
            
            if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
            {
                // Valid MPEG frame sync
                ParseMpegFrameHeader(header, metadata);
                break;
            }
            
            ms.Seek(-3, SeekOrigin.Current); // Backtrack for next attempt
        }
    }

    private static void ParseId3v2Tag(Stream ms, AudioMetadata metadata)
    {
        ms.Seek(3, SeekOrigin.Begin); // Skip "ID3"
        
        var version = ms.ReadByte();
        var revision = ms.ReadByte();
        var flags = ms.ReadByte();
        
        // Read tag size (synchsafe integer - 7 bits per byte)
        var sizeBytes = new byte[4];
        ReadExact(ms, sizeBytes, 0, 4);
        var tagSize = (sizeBytes[0] << 21) | (sizeBytes[1] << 14) | (sizeBytes[2] << 7) | sizeBytes[3];
        
        var tagEndPosition = ms.Position + tagSize;
        
        // Parse frames
        while (ms.Position < tagEndPosition - 10)
        {
            var frameId = new byte[4];
            ReadExact(ms, frameId, 0, 4);
            var frameIdStr = Encoding.ASCII.GetString(frameId);
            
            // Check for null padding
            if (frameIdStr[0] == '\0')
                break;
            
            var frameSizeBytes = new byte[4];
            ReadExact(ms, frameSizeBytes, 0, 4);
            var frameSize = version >= 4 
                ? (frameSizeBytes[0] << 21) | (frameSizeBytes[1] << 14) | (frameSizeBytes[2] << 7) | frameSizeBytes[3]
                : BitConverter.ToInt32(new[] { frameSizeBytes[3], frameSizeBytes[2], frameSizeBytes[1], frameSizeBytes[0] }, 0);
            
            var frameFlagsBytes = new byte[2];
            ReadExact(ms, frameFlagsBytes, 0, 2);
            
            if (frameSize <= 0 || ms.Position + frameSize > tagEndPosition)
                break;
            
            var frameData = new byte[frameSize];
            ReadExact(ms, frameData, 0, frameSize);
            
            // Extract text from frame (skip encoding byte)
            if (frameSize > 1)
            {
                var encoding = frameData[0];
                var textData = new byte[frameSize - 1];
                Array.Copy(frameData, 1, textData, 0, frameSize - 1);
                
                var text = encoding switch
                {
                    0 => Encoding.Latin1.GetString(textData).TrimEnd('\0'),
                    1 => Encoding.Unicode.GetString(textData).TrimEnd('\0'),
                    2 => Encoding.BigEndianUnicode.GetString(textData).TrimEnd('\0'),
                    3 => Encoding.UTF8.GetString(textData).TrimEnd('\0'),
                    _ => Encoding.ASCII.GetString(textData).TrimEnd('\0')
                };
                
                switch (frameIdStr)
                {
                    case "TIT2": metadata.Title = text; break;
                    case "TPE1": metadata.Artist = text; break;
                    case "TALB": metadata.Album = text; break;
                    case "TYER": 
                    case "TDRC": metadata.Year = text; break;
                    case "TCON": metadata.Genre = text; break;
                    case "COMM": metadata.Comment = text; break;
                }
            }
        }
        
        ms.Seek(tagEndPosition, SeekOrigin.Begin);
    }

    private static void ParseMpegFrameHeader(byte[] header, AudioMetadata metadata)
    {
        // MPEG version
        var version = (header[1] >> 3) & 0x03;
        var versionStr = version switch
        {
            0 => "MPEG 2.5",
            2 => "MPEG 2",
            3 => "MPEG 1",
            _ => "Unknown"
        };
        
        // Layer
        var layer = (header[1] >> 1) & 0x03;
        var layerStr = layer switch
        {
            1 => "Layer III",
            2 => "Layer II",
            3 => "Layer I",
            _ => "Unknown"
        };
        
        metadata.Codec = $"{versionStr} {layerStr}";
        
        // Bitrate
        var bitrateIndex = (header[2] >> 4) & 0x0F;
        metadata.Bitrate = GetMpegBitrate(version, layer, bitrateIndex);
        
        // Sample rate
        var sampleRateIndex = (header[2] >> 2) & 0x03;
        metadata.SampleRate = GetMpegSampleRate(version, sampleRateIndex);
        
        // Channel mode
        var channelMode = (header[3] >> 6) & 0x03;
        metadata.Channels = channelMode == 3 ? 1 : 2;
    }

    private static int GetMpegBitrate(int version, int layer, int bitrateIndex)
    {
        // MPEG1 Layer III bitrate table (kbps)
        int[] mpeg1Layer3Bitrates = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
        
        // MPEG2/2.5 Layer III bitrate table (kbps)
        int[] mpeg2Layer3Bitrates = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
        
        if (version == 3 && layer == 1)
            return bitrateIndex < mpeg1Layer3Bitrates.Length ? mpeg1Layer3Bitrates[bitrateIndex] : 0;
        else if ((version == 2 || version == 0) && layer == 1)
            return bitrateIndex < mpeg2Layer3Bitrates.Length ? mpeg2Layer3Bitrates[bitrateIndex] : 0;
        
        return 128; // Default fallback
    }

    private static int GetMpegSampleRate(int version, int sampleRateIndex)
    {
        int[][] sampleRates = {
            new[] { 11025, 12000, 8000, 0 },  // MPEG 2.5
            new[] { 0, 0, 0, 0 },              // Reserved
            new[] { 22050, 24000, 16000, 0 },  // MPEG 2
            new[] { 44100, 48000, 32000, 0 }   // MPEG 1
        };
        
        return sampleRates[version][sampleRateIndex];
    }

    private static void ExtractFlacMetadata(byte[] data, AudioMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        ms.Seek(4, SeekOrigin.Begin); // Skip "fLaC"
        
        while (ms.Position < ms.Length - 4)
        {
            var blockHeader = ms.ReadByte();
            var isLast = (blockHeader & 0x80) != 0;
            var blockType = blockHeader & 0x7F;
            
            var blockSizeBytes = new byte[3];
            ReadExact(ms, blockSizeBytes, 0, 3);
            var blockSize = (blockSizeBytes[0] << 16) | (blockSizeBytes[1] << 8) | blockSizeBytes[2];
            
            var blockData = new byte[blockSize];
            ReadExact(ms, blockData, 0, blockSize);
            
            if (blockType == 0) // STREAMINFO
            {
                // Sample rate (20 bits, starting at bit 80)
                var sampleRate = ((blockData[10] << 12) | (blockData[11] << 4) | (blockData[12] >> 4)) & 0xFFFFF;
                metadata.SampleRate = sampleRate;
                
                // Channels (3 bits, bits 100-102)
                var channels = ((blockData[12] >> 1) & 0x07) + 1;
                metadata.Channels = channels;
                
                // Bits per sample (5 bits, bits 103-107)
                var bitsPerSample = (((blockData[12] & 0x01) << 4) | (blockData[13] >> 4)) + 1;
                metadata.BitDepth = bitsPerSample;
                
                metadata.Codec = "FLAC";
            }
            else if (blockType == 4) // VORBIS_COMMENT
            {
                ParseVorbisComment(blockData, metadata);
            }
            
            if (isLast)
                break;
        }
    }

    private static void ParseVorbisComment(byte[] data, AudioMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Vendor length
        var vendorLengthBytes = new byte[4];
        ReadExact(ms, vendorLengthBytes, 0, 4);
        var vendorLength = BitConverter.ToUInt32(vendorLengthBytes, 0);
        
        ms.Seek(vendorLength, SeekOrigin.Current); // Skip vendor string
        
        // Comment count
        var commentCountBytes = new byte[4];
        ReadExact(ms, commentCountBytes, 0, 4);
        var commentCount = BitConverter.ToUInt32(commentCountBytes, 0);
        
        for (var i = 0; i < commentCount && ms.Position < ms.Length - 4; i++)
        {
            var commentLengthBytes = new byte[4];
            ReadExact(ms, commentLengthBytes, 0, 4);
            var commentLength = BitConverter.ToUInt32(commentLengthBytes, 0);
            
            if (commentLength > ms.Length - ms.Position)
                break;
            
            var commentBytes = new byte[commentLength];
            ReadExact(ms, commentBytes, 0, (int)commentLength);
            var comment = Encoding.UTF8.GetString(commentBytes);
            
            var parts = comment.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].ToUpperInvariant();
                var value = parts[1];
                
                switch (key)
                {
                    case "TITLE": metadata.Title = value; break;
                    case "ARTIST": metadata.Artist = value; break;
                    case "ALBUM": metadata.Album = value; break;
                    case "DATE": metadata.Year = value; break;
                    case "GENRE": metadata.Genre = value; break;
                    case "COMMENT": metadata.Comment = value; break;
                }
            }
        }
    }

    private static void ExtractWavMetadata(byte[] data, AudioMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        ms.Seek(12, SeekOrigin.Begin); // Skip RIFF header + "WAVE"
        
        while (ms.Position < ms.Length - 8)
        {
            var chunkId = new byte[4];
            ReadExact(ms, chunkId, 0, 4);
            var chunkType = Encoding.ASCII.GetString(chunkId);
            
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            var chunkSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            if (chunkType == "fmt ")
            {
                var fmtData = new byte[Math.Min(chunkSize, 16)];
                ReadExact(ms, fmtData, 0, fmtData.Length);
                
                var audioFormat = BitConverter.ToUInt16(fmtData, 0);
                metadata.Codec = audioFormat switch
                {
                    1 => "PCM",
                    3 => "IEEE Float",
                    6 => "A-law",
                    7 => "μ-law",
                    _ => $"Format {audioFormat}"
                };
                
                metadata.Channels = BitConverter.ToUInt16(fmtData, 2);
                metadata.SampleRate = (int)BitConverter.ToUInt32(fmtData, 4);
                metadata.Bitrate = (int)(BitConverter.ToUInt32(fmtData, 8) * 8 / 1000); // Bytes/sec to kbps
                metadata.BitDepth = BitConverter.ToUInt16(fmtData, 14);
                
                if (chunkSize > fmtData.Length)
                    ms.Seek(chunkSize - fmtData.Length, SeekOrigin.Current);
            }
            else if (chunkType == "LIST")
            {
                // Parse INFO list chunk (metadata)
                var listTypeBytes = new byte[4];
                ReadExact(ms, listTypeBytes, 0, 4);
                var listType = Encoding.ASCII.GetString(listTypeBytes);
                
                if (listType == "INFO")
                {
                    ParseWavInfoChunk(ms, chunkSize - 4, metadata);
                }
                else
                {
                    ms.Seek(chunkSize - 4, SeekOrigin.Current);
                }
            }
            else
            {
                ms.Seek(chunkSize, SeekOrigin.Current);
            }
            
            // Align to word boundary
            if (chunkSize % 2 != 0)
                ms.Seek(1, SeekOrigin.Current);
        }
    }

    private static void ParseWavInfoChunk(Stream ms, uint chunkSize, AudioMetadata metadata)
    {
        var endPosition = ms.Position + chunkSize;
        
        while (ms.Position < endPosition - 8)
        {
            var infoId = new byte[4];
            ReadExact(ms, infoId, 0, 4);
            var infoType = Encoding.ASCII.GetString(infoId);
            
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            var infoSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            if (infoSize > 0 && infoSize < 10000)
            {
                var infoData = new byte[infoSize];
                ReadExact(ms, infoData, 0, (int)infoSize);
                var text = Encoding.ASCII.GetString(infoData).TrimEnd('\0');
                
                switch (infoType)
                {
                    case "INAM": metadata.Title = text; break;
                    case "IART": metadata.Artist = text; break;
                    case "IPRD": metadata.Album = text; break;
                    case "ICRD": metadata.Year = text; break;
                    case "IGNR": metadata.Genre = text; break;
                    case "ICMT": metadata.Comment = text; break;
                }
                
                // Align to word boundary
                if (infoSize % 2 != 0)
                    ms.Seek(1, SeekOrigin.Current);
            }
            else
            {
                break;
            }
        }
    }

    private static void ExtractAacMetadata(byte[] data, AudioMetadata metadata)
    {
        // Parse ADTS header (AAC)
        if (data.Length < 7)
            return;
        
        var header = data;
        
        // Sample rate index (4 bits)
        var sampleRateIndex = (header[2] >> 2) & 0x0F;
        int[] sampleRates = { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350, 0, 0, 0 };
        metadata.SampleRate = sampleRates[sampleRateIndex];
        
        // Channel configuration (3 bits)
        var channelConfig = ((header[2] & 0x01) << 2) | ((header[3] >> 6) & 0x03);
        metadata.Channels = channelConfig switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            7 => 8,
            _ => 2
        };
        
        metadata.Codec = "AAC";
    }

    private static void ExtractOggMetadata(byte[] data, AudioMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Skip OGG page header
        ms.Seek(4, SeekOrigin.Begin); // "OggS"
        
        // Find Vorbis identification header
        // This is a simplified parser - full OGG parsing is complex
        metadata.Codec = "Vorbis";
        metadata.Properties["Note"] = "OGG Vorbis full parsing not yet implemented";
    }

    private static void ExtractM4aMetadata(byte[] data, AudioMetadata metadata)
    {
        // M4A uses MP4 container format
        metadata.Codec = "AAC (M4A)";
        metadata.Properties["Note"] = "M4A full parsing not yet implemented (uses MP4 container)";
    }
}

// AudioMetadata class now defined in MediaMetadataModels.cs
