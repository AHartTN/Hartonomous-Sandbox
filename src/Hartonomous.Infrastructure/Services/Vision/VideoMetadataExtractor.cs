using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Hartonomous.Infrastructure.Services.Vision.BinaryReaderHelper;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Extract metadata from video files (MP4, AVI, MKV, WebM, etc.).
/// Parses container format, codec info, resolution, frame rate, bitrate.
/// Uses shared BinaryReaderHelper utilities.
/// </summary>
public static class VideoMetadataExtractor
{
    public static VideoMetadata ExtractMetadata(byte[] videoData)
    {
        var metadata = new VideoMetadata
        {
            FileSizeBytes = videoData.Length
        };
        
        // Detect format
        if (IsMp4(videoData))
        {
            metadata.Container = "MP4";
            ExtractMp4Metadata(videoData, metadata);
        }
        else if (IsAvi(videoData))
        {
            metadata.Container = "AVI";
            ExtractAviMetadata(videoData, metadata);
        }
        else if (IsMkv(videoData))
        {
            metadata.Container = "MKV/WebM";
            ExtractMkvMetadata(videoData, metadata);
        }
        else if (IsWebM(videoData))
        {
            metadata.Container = "WebM";
            ExtractMkvMetadata(videoData, metadata); // WebM uses Matroska format
        }
        else if (IsMov(videoData))
        {
            metadata.Container = "MOV";
            ExtractMp4Metadata(videoData, metadata); // MOV uses similar format to MP4
        }
        
        return metadata;
    }

    private static bool IsMp4(byte[] data) => 
        data.Length >= 12 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70;

    private static bool IsAvi(byte[] data) => 
        data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
        data[8] == 0x41 && data[9] == 0x56 && data[10] == 0x49;

    private static bool IsMkv(byte[] data) => 
        data.Length >= 4 && data[0] == 0x1A && data[1] == 0x45 && data[2] == 0xDF && data[3] == 0xA3;

    private static bool IsWebM(byte[] data) => IsMkv(data); // WebM uses Matroska container

    private static bool IsMov(byte[] data) => 
        data.Length >= 12 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70;

    private static void ExtractMp4Metadata(byte[] data, VideoMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        while (ms.Position < ms.Length - 8)
        {
            // Read atom size
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            Array.Reverse(sizeBytes);
            var atomSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            // Read atom type
            var typeBytes = new byte[4];
            ReadExact(ms, typeBytes, 0, 4);
            var atomType = Encoding.ASCII.GetString(typeBytes);
            
            if (atomSize < 8 || ms.Position + atomSize - 8 > ms.Length)
                break;
            
            var atomDataStart = ms.Position;
            
            switch (atomType)
            {
                case "ftyp": // File type
                    var brandBytes = new byte[4];
                    ReadExact(ms, brandBytes, 0, 4);
                    var brand = Encoding.ASCII.GetString(brandBytes);
                    metadata.Properties["Brand"] = brand;
                    
                    if (brand.StartsWith("iso")) metadata.VideoCodec = "H.264";
                    else if (brand.StartsWith("mp4")) metadata.VideoCodec = "MPEG-4";
                    break;
                
                case "moov": // Movie metadata container
                    ParseMoovAtom(ms, atomSize - 8, metadata);
                    break;
                
                case "mdat": // Media data (skip)
                    break;
            }
            
            // Jump to next atom
            ms.Seek(atomDataStart + atomSize - 8, SeekOrigin.Begin);
        }
    }

    private static void ParseMoovAtom(Stream ms, long atomSize, VideoMetadata metadata)
    {
        var endPosition = ms.Position + atomSize;
        
        while (ms.Position < endPosition - 8)
        {
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            Array.Reverse(sizeBytes);
            var subAtomSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            var typeBytes = new byte[4];
            ReadExact(ms, typeBytes, 0, 4);
            var subAtomType = Encoding.ASCII.GetString(typeBytes);
            
            if (subAtomSize < 8)
                break;
            
            var subAtomDataStart = ms.Position;
            
            switch (subAtomType)
            {
                case "mvhd": // Movie header
                    ParseMvhdAtom(ms, metadata);
                    break;
                
                case "trak": // Track
                    ParseTrakAtom(ms, subAtomSize - 8, metadata);
                    break;
            }
            
            ms.Seek(subAtomDataStart + subAtomSize - 8, SeekOrigin.Begin);
        }
    }

    private static void ParseMvhdAtom(Stream ms, VideoMetadata metadata)
    {
        var version = ms.ReadByte();
        ms.Seek(3, SeekOrigin.Current); // Flags
        
        if (version == 1)
        {
            ms.Seek(16, SeekOrigin.Current); // Creation/modification time (64-bit)
            
            var timescaleBytes = new byte[4];
            ReadExact(ms, timescaleBytes, 0, 4);
            Array.Reverse(timescaleBytes);
            var timescale = BitConverter.ToUInt32(timescaleBytes, 0);
            
            var durationBytes = new byte[8];
            ReadExact(ms, durationBytes, 0, 8);
            Array.Reverse(durationBytes);
            var duration = BitConverter.ToUInt64(durationBytes, 0);
            
            metadata.DurationSeconds = (double)duration / timescale;
        }
        else
        {
            ms.Seek(8, SeekOrigin.Current); // Creation/modification time (32-bit)
            
            var timescaleBytes = new byte[4];
            ReadExact(ms, timescaleBytes, 0, 4);
            Array.Reverse(timescaleBytes);
            var timescale = BitConverter.ToUInt32(timescaleBytes, 0);
            
            var durationBytes = new byte[4];
            ReadExact(ms, durationBytes, 0, 4);
            Array.Reverse(durationBytes);
            var duration = BitConverter.ToUInt32(durationBytes, 0);
            
            metadata.DurationSeconds = (double)duration / timescale;
        }
    }

    private static void ParseTrakAtom(Stream ms, long atomSize, VideoMetadata metadata)
    {
        var endPosition = ms.Position + atomSize;
        
        while (ms.Position < endPosition - 8)
        {
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            Array.Reverse(sizeBytes);
            var subAtomSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            var typeBytes = new byte[4];
            ReadExact(ms, typeBytes, 0, 4);
            var subAtomType = Encoding.ASCII.GetString(typeBytes);
            
            if (subAtomSize < 8)
                break;
            
            var subAtomDataStart = ms.Position;
            
            if (subAtomType == "tkhd") // Track header
            {
                ParseTkhdAtom(ms, metadata);
            }
            else if (subAtomType == "mdia") // Media
            {
                ParseMdiaAtom(ms, subAtomSize - 8, metadata);
            }
            
            ms.Seek(subAtomDataStart + subAtomSize - 8, SeekOrigin.Begin);
        }
    }

    private static void ParseTkhdAtom(Stream ms, VideoMetadata metadata)
    {
        var version = ms.ReadByte();
        ms.Seek(3, SeekOrigin.Current); // Flags
        
        if (version == 1)
        {
            ms.Seek(32, SeekOrigin.Current); // Skip to dimensions
        }
        else
        {
            ms.Seek(20, SeekOrigin.Current); // Skip to dimensions
        }
        
        // Read track dimensions (fixed-point 16.16)
        var widthBytes = new byte[4];
        ReadExact(ms, widthBytes, 0, 4);
        Array.Reverse(widthBytes);
        var widthFixed = BitConverter.ToUInt32(widthBytes, 0);
        
        var heightBytes = new byte[4];
        ReadExact(ms, heightBytes, 0, 4);
        Array.Reverse(heightBytes);
        var heightFixed = BitConverter.ToUInt32(heightBytes, 0);
        
        // Convert from fixed-point to integer
        var width = (int)(widthFixed >> 16);
        var height = (int)(heightFixed >> 16);
        
        if (width > 0 && metadata.Width == 0)
            metadata.Width = width;
        if (height > 0 && metadata.Height == 0)
            metadata.Height = height;
    }

    private static void ParseMdiaAtom(Stream ms, long atomSize, VideoMetadata metadata)
    {
        var endPosition = ms.Position + atomSize;
        
        while (ms.Position < endPosition - 8)
        {
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            Array.Reverse(sizeBytes);
            var subAtomSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            var typeBytes = new byte[4];
            ReadExact(ms, typeBytes, 0, 4);
            var subAtomType = Encoding.ASCII.GetString(typeBytes);
            
            if (subAtomSize < 8)
                break;
            
            var subAtomDataStart = ms.Position;
            
            if (subAtomType == "hdlr") // Handler reference
            {
                ms.Seek(8, SeekOrigin.Current); // Version/flags
                
                var handlerBytes = new byte[4];
                ReadExact(ms, handlerBytes, 0, 4);
                var handlerType = Encoding.ASCII.GetString(handlerBytes);
                
                if (handlerType == "vide")
                {
                    metadata.HasVideo = true;
                }
                else if (handlerType == "soun")
                {
                    metadata.HasAudio = true;
                }
            }
            else if (subAtomType == "minf") // Media information
            {
                ParseMinfAtom(ms, subAtomSize - 8, metadata);
            }
            
            ms.Seek(subAtomDataStart + subAtomSize - 8, SeekOrigin.Begin);
        }
    }

    private static void ParseMinfAtom(Stream ms, long atomSize, VideoMetadata metadata)
    {
        var endPosition = ms.Position + atomSize;
        
        while (ms.Position < endPosition - 8)
        {
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            Array.Reverse(sizeBytes);
            var subAtomSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            var typeBytes = new byte[4];
            ReadExact(ms, typeBytes, 0, 4);
            var subAtomType = Encoding.ASCII.GetString(typeBytes);
            
            if (subAtomSize < 8)
                break;
            
            var subAtomDataStart = ms.Position;
            
            if (subAtomType == "stbl") // Sample table
            {
                ParseStblAtom(ms, subAtomSize - 8, metadata);
            }
            
            ms.Seek(subAtomDataStart + subAtomSize - 8, SeekOrigin.Begin);
        }
    }

    private static void ParseStblAtom(Stream ms, long atomSize, VideoMetadata metadata)
    {
        var endPosition = ms.Position + atomSize;
        
        while (ms.Position < endPosition - 8)
        {
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            Array.Reverse(sizeBytes);
            var subAtomSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            var typeBytes = new byte[4];
            ReadExact(ms, typeBytes, 0, 4);
            var subAtomType = Encoding.ASCII.GetString(typeBytes);
            
            if (subAtomSize < 8)
                break;
            
            var subAtomDataStart = ms.Position;
            
            if (subAtomType == "stsd") // Sample description
            {
                ms.Seek(4, SeekOrigin.Current); // Version/flags
                
                var entryCountBytes = new byte[4];
                ReadExact(ms, entryCountBytes, 0, 4);
                Array.Reverse(entryCountBytes);
                var entryCount = BitConverter.ToUInt32(entryCountBytes, 0);
                
                if (entryCount > 0)
                {
                    // Read first entry
                    ms.Seek(4, SeekOrigin.Current); // Entry size
                    
                    var codecBytes = new byte[4];
                    ReadExact(ms, codecBytes, 0, 4);
                    var codec = Encoding.ASCII.GetString(codecBytes);
                    
                    if (string.IsNullOrEmpty(metadata.VideoCodec))
                    {
                        metadata.VideoCodec = codec switch
                        {
                            "avc1" => "H.264/AVC",
                            "hvc1" => "H.265/HEVC",
                            "hev1" => "H.265/HEVC",
                            "mp4v" => "MPEG-4 Visual",
                            "vp08" => "VP8",
                            "vp09" => "VP9",
                            "av01" => "AV1",
                            _ => codec
                        };
                    }
                }
            }
            
            ms.Seek(subAtomDataStart + subAtomSize - 8, SeekOrigin.Begin);
        }
    }

    private static void ExtractAviMetadata(byte[] data, VideoMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Skip RIFF header
        ms.Seek(12, SeekOrigin.Begin);
        
        while (ms.Position < ms.Length - 8)
        {
            var chunkId = new byte[4];
            ReadExact(ms, chunkId, 0, 4);
            var chunkType = Encoding.ASCII.GetString(chunkId);
            
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            var chunkSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            if (chunkType == "LIST")
            {
                var listTypeBytes = new byte[4];
                ReadExact(ms, listTypeBytes, 0, 4);
                var listType = Encoding.ASCII.GetString(listTypeBytes);
                
                if (listType == "hdrl")
                {
                    ParseAviHeaderList(ms, chunkSize - 4, metadata);
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

    private static void ParseAviHeaderList(Stream ms, uint listSize, VideoMetadata metadata)
    {
        var endPosition = ms.Position + listSize;
        
        while (ms.Position < endPosition - 8)
        {
            var chunkId = new byte[4];
            ReadExact(ms, chunkId, 0, 4);
            var chunkType = Encoding.ASCII.GetString(chunkId);
            
            var sizeBytes = new byte[4];
            ReadExact(ms, sizeBytes, 0, 4);
            var chunkSize = BitConverter.ToUInt32(sizeBytes, 0);
            
            if (chunkType == "avih")
            {
                var microsecPerFrameBytes = new byte[4];
                ReadExact(ms, microsecPerFrameBytes, 0, 4);
                var microsecPerFrame = BitConverter.ToUInt32(microsecPerFrameBytes, 0);
                
                if (microsecPerFrame > 0)
                {
                    metadata.FrameRate = 1_000_000.0 / microsecPerFrame;
                }
                
                ms.Seek(8, SeekOrigin.Current); // Skip max bytes per sec and padding
                
                var flagsBytes = new byte[4];
                ReadExact(ms, flagsBytes, 0, 4);
                
                var totalFramesBytes = new byte[4];
                ReadExact(ms, totalFramesBytes, 0, 4);
                var totalFrames = BitConverter.ToUInt32(totalFramesBytes, 0);
                
                if (totalFrames > 0 && metadata.FrameRate > 0)
                {
                    metadata.DurationSeconds = totalFrames / metadata.FrameRate;
                }
                
                ms.Seek(4, SeekOrigin.Current); // Initial frames
                
                var streamsBytes = new byte[4];
                ReadExact(ms, streamsBytes, 0, 4);
                
                ms.Seek(4, SeekOrigin.Current); // Suggested buffer size
                
                var widthBytes = new byte[4];
                ReadExact(ms, widthBytes, 0, 4);
                metadata.Width = (int)BitConverter.ToUInt32(widthBytes, 0);
                
                var heightBytes = new byte[4];
                ReadExact(ms, heightBytes, 0, 4);
                metadata.Height = (int)BitConverter.ToUInt32(heightBytes, 0);
            }
            else
            {
                ms.Seek(chunkSize, SeekOrigin.Current);
            }
            
            if (chunkSize % 2 != 0)
                ms.Seek(1, SeekOrigin.Current);
        }
    }

    private static void ExtractMkvMetadata(byte[] data, VideoMetadata metadata)
    {
        // MKV/WebM parsing is complex (EBML format)
        // This is a simplified version that extracts basic info
        
        metadata.Properties["Note"] = "MKV/WebM full parsing not yet implemented";
        
        // Common WebM codecs
        metadata.VideoCodec = "VP8/VP9 (typical)";
        metadata.AudioCodec = "Opus/Vorbis (typical)";
    }
}

// VideoMetadata class now defined in MediaMetadataModels.cs
