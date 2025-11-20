using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Hartonomous.Infrastructure.Services.Vision.BinaryReaderHelper;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Extract EXIF and other metadata from image files.
/// Pure C# implementation - no external dependencies.
/// </summary>
public static class ImageMetadataExtractor
{
    public static ImageMetadata ExtractMetadata(byte[] imageData)
    {
        var metadata = new ImageMetadata();
        
        // Detect format
        if (IsJpeg(imageData))
        {
            metadata.Format = "JPEG";
            ExtractJpegMetadata(imageData, metadata);
        }
        else if (IsPng(imageData))
        {
            metadata.Format = "PNG";
            ExtractPngMetadata(imageData, metadata);
        }
        else if (IsBmp(imageData))
        {
            metadata.Format = "BMP";
            ExtractBmpMetadata(imageData, metadata);
        }
        else if (IsGif(imageData))
        {
            metadata.Format = "GIF";
            ExtractGifMetadata(imageData, metadata);
        }
        else if (IsTiff(imageData))
        {
            metadata.Format = "TIFF";
            ExtractTiffMetadata(imageData, metadata);
        }
        else if (IsWebP(imageData))
        {
            metadata.Format = "WebP";
            ExtractWebPMetadata(imageData, metadata);
        }
        
        metadata.FileSizeBytes = imageData.Length;
        
        return metadata;
    }

    private static bool IsJpeg(byte[] data) => 
        data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;

    private static bool IsPng(byte[] data) => 
        data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;

    private static bool IsBmp(byte[] data) => 
        data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D;

    private static bool IsGif(byte[] data) => 
        data.Length >= 6 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46;

    private static bool IsTiff(byte[] data) => 
        data.Length >= 4 && ((data[0] == 0x49 && data[1] == 0x49) || (data[0] == 0x4D && data[1] == 0x4D));

    private static bool IsWebP(byte[] data) => 
        data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46;

    private static void ExtractJpegMetadata(byte[] data, ImageMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Skip SOI marker (0xFF 0xD8)
        ms.Seek(2, SeekOrigin.Begin);
        
        while (ms.Position < ms.Length - 1)
        {
            // Read marker
            var marker1 = ms.ReadByte();
            var marker2 = ms.ReadByte();
            
            if (marker1 != 0xFF)
                break;
            
            // Skip padding bytes
            while (marker2 == 0xFF && ms.Position < ms.Length)
                marker2 = ms.ReadByte();
            
            // Read segment length
            if (ms.Position >= ms.Length - 1)
                break;
                
            var lengthHi = ms.ReadByte();
            var lengthLo = ms.ReadByte();
            var segmentLength = (lengthHi << 8) | lengthLo;
            
            if (segmentLength < 2)
                break;
            
            var dataLength = segmentLength - 2;
            
            // SOF0 (Start of Frame - Baseline DCT)
            if (marker2 == 0xC0)
            {
                ms.ReadByte(); // Precision
                var heightHi = ms.ReadByte();
                var heightLo = ms.ReadByte();
                var widthHi = ms.ReadByte();
                var widthLo = ms.ReadByte();
                
                metadata.Width = (widthHi << 8) | widthLo;
                metadata.Height = (heightHi << 8) | heightLo;
                
                var components = ms.ReadByte();
                metadata.ColorSpace = components == 1 ? "Grayscale" : components == 3 ? "RGB" : "CMYK";
                
                ms.Seek(dataLength - 6, SeekOrigin.Current);
            }
            // APP1 (EXIF data)
            else if (marker2 == 0xE1)
            {
                var exifData = new byte[dataLength];
                ms.Read(exifData, 0, dataLength);
                
                // Check for EXIF identifier
                if (exifData.Length >= 6 && 
                    exifData[0] == 0x45 && exifData[1] == 0x78 && 
                    exifData[2] == 0x69 && exifData[3] == 0x66)
                {
                    ParseExifData(exifData, metadata);
                }
            }
            // APP0 (JFIF)
            else if (marker2 == 0xE0)
            {
                var jfifData = new byte[Math.Min(dataLength, 16)];
                ms.Read(jfifData, 0, jfifData.Length);
                
                if (jfifData.Length >= 5 && 
                    jfifData[0] == 0x4A && jfifData[1] == 0x46 && 
                    jfifData[2] == 0x49 && jfifData[3] == 0x46)
                {
                    metadata.Properties["JFIF"] = "true";
                }
                
                if (dataLength > jfifData.Length)
                    ms.Seek(dataLength - jfifData.Length, SeekOrigin.Current);
            }
            else
            {
                // Skip unknown segments
                ms.Seek(dataLength, SeekOrigin.Current);
            }
        }
    }

    private static void ParseExifData(byte[] exifData, ImageMetadata metadata)
    {
        // Skip "Exif\0\0" header
        if (exifData.Length < 14)
            return;
        
        var offset = 6;
        
        // Check byte order (II = little-endian, MM = big-endian)
        var littleEndian = exifData[offset] == 0x49 && exifData[offset + 1] == 0x49;
        
        // Skip TIFF header
        offset += 8;
        
        // Read IFD0 entries (simplified - full parser would be more complex)
        if (offset + 2 > exifData.Length)
            return;
            
        var entryCount = BinaryReaderHelper.ReadUInt16(exifData, offset, littleEndian);
        offset += 2;
        
        for (int i = 0; i < entryCount && offset + 12 <= exifData.Length; i++)
        {
            var tag = BinaryReaderHelper.ReadUInt16(exifData, offset, littleEndian);
            var type = BinaryReaderHelper.ReadUInt16(exifData, offset + 2, littleEndian);
            var count = BinaryReaderHelper.ReadUInt32(exifData, offset + 4, littleEndian);
            var valueOffset = offset + 8;
            
            // Common EXIF tags
            switch (tag)
            {
                case 0x010F: // Make
                    metadata.CameraMake = BinaryReaderHelper.ReadString(exifData, valueOffset, (int)count);
                    break;
                case 0x0110: // Model
                    metadata.CameraModel = BinaryReaderHelper.ReadString(exifData, valueOffset, (int)count);
                    break;
                case 0x0112: // Orientation
                    metadata.Orientation = BinaryReaderHelper.ReadUInt16(exifData, valueOffset, littleEndian);
                    break;
                case 0x9003: // DateTimeOriginal
                    metadata.DateTaken = DateTime.TryParse(BinaryReaderHelper.ReadString(exifData, valueOffset, (int)count), out var dt) ? dt : (DateTime?)null;
                    break;
                case 0x829A: // ExposureTime
                    metadata.ExposureTime = BinaryReaderHelper.ReadRational(exifData, valueOffset, littleEndian);
                    break;
                case 0x829D: // FNumber
                    metadata.FNumber = BinaryReaderHelper.ReadRational(exifData, valueOffset, littleEndian);
                    break;
                case 0x8827: // ISO
                    metadata.ISO = BinaryReaderHelper.ReadUInt16(exifData, valueOffset, littleEndian);
                    break;
            }
            
            offset += 12;
        }
    }

    private static void ExtractPngMetadata(byte[] data, ImageMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Skip PNG signature
        ms.Seek(8, SeekOrigin.Begin);
        
        while (ms.Position < ms.Length - 8)
        {
            // Read chunk length
            var lengthBytes = new byte[4];
            ms.Read(lengthBytes, 0, 4);
            Array.Reverse(lengthBytes); // PNG uses big-endian
            var length = BitConverter.ToInt32(lengthBytes, 0);
            
            // Read chunk type
            var typeBytes = new byte[4];
            ms.Read(typeBytes, 0, 4);
            var chunkType = Encoding.ASCII.GetString(typeBytes);
            
            // IHDR - Image header
            if (chunkType == "IHDR" && length >= 13)
            {
                var ihdrBytes = new byte[13];
                ms.Read(ihdrBytes, 0, 13);
                
                metadata.Width = (int)BinaryReaderHelper.ReadUInt32(ihdrBytes, 0, littleEndian: false);
                metadata.Height = (int)BinaryReaderHelper.ReadUInt32(ihdrBytes, 4, littleEndian: false);
                metadata.BitDepth = ihdrBytes[8];
                
                var colorType = ihdrBytes[9];
                metadata.ColorSpace = colorType switch
                {
                    0 => "Grayscale",
                    2 => "RGB",
                    3 => "Indexed",
                    4 => "Grayscale + Alpha",
                    6 => "RGBA",
                    _ => "Unknown"
                };
                
                metadata.Properties["Compression"] = ihdrBytes[10].ToString();
                metadata.Properties["Filter"] = ihdrBytes[11].ToString();
                metadata.Properties["Interlace"] = ihdrBytes[12] == 1 ? "Adam7" : "None";
                
                ms.Seek(4, SeekOrigin.Current); // Skip CRC
                
                if (length > 13)
                    ms.Seek(length - 13, SeekOrigin.Current);
            }
            // tEXt - Textual data
            else if (chunkType == "tEXt" && length > 0)
            {
                var textData = new byte[length];
                ms.Read(textData, 0, length);
                
                var nullIndex = Array.IndexOf(textData, (byte)0);
                if (nullIndex > 0 && nullIndex < textData.Length - 1)
                {
                    var keyword = Encoding.ASCII.GetString(textData, 0, nullIndex);
                    var value = Encoding.ASCII.GetString(textData, nullIndex + 1, textData.Length - nullIndex - 1);
                    metadata.Properties[keyword] = value;
                }
                
                ms.Seek(4, SeekOrigin.Current); // Skip CRC
            }
            else
            {
                // Skip chunk data and CRC
                ms.Seek(length + 4, SeekOrigin.Current);
            }
            
            // Stop at IEND
            if (chunkType == "IEND")
                break;
        }
    }

    private static void ExtractBmpMetadata(byte[] data, ImageMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        
        br.ReadUInt16(); // "BM"
        metadata.FileSizeBytes = br.ReadInt32();
        br.ReadUInt32(); // Reserved
        br.ReadUInt32(); // Data offset
        
        var headerSize = br.ReadUInt32();
        metadata.Width = br.ReadInt32();
        metadata.Height = Math.Abs(br.ReadInt32()); // Height can be negative
        br.ReadUInt16(); // Planes
        metadata.BitDepth = br.ReadUInt16();
        
        var compression = br.ReadUInt32();
        metadata.Properties["Compression"] = compression switch
        {
            0 => "None",
            1 => "RLE 8-bit",
            2 => "RLE 4-bit",
            3 => "Bitfields",
            _ => compression.ToString()
        };
        
        metadata.ColorSpace = metadata.BitDepth switch
        {
            1 => "Monochrome",
            8 => "Indexed",
            24 => "RGB",
            32 => "RGBA",
            _ => "Unknown"
        };
    }

    private static void ExtractGifMetadata(byte[] data, ImageMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Read header
        var header = new byte[6];
        ms.Read(header, 0, 6);
        var version = Encoding.ASCII.GetString(header, 3, 3); // "87a" or "89a"
        metadata.Properties["GIF Version"] = version;
        
        // Read logical screen descriptor
        var widthLo = ms.ReadByte();
        var widthHi = ms.ReadByte();
        var heightLo = ms.ReadByte();
        var heightHi = ms.ReadByte();
        
        metadata.Width = widthLo | (widthHi << 8);
        metadata.Height = heightLo | (heightHi << 8);
        
        var packed = ms.ReadByte();
        var hasGlobalColorTable = (packed & 0x80) != 0;
        var colorResolution = ((packed & 0x70) >> 4) + 1;
        var globalColorTableSize = 2 << (packed & 0x07);
        
        metadata.BitDepth = colorResolution;
        metadata.ColorSpace = "Indexed";
        metadata.Properties["Global Color Table"] = hasGlobalColorTable.ToString();
    }

    private static void ExtractTiffMetadata(byte[] data, ImageMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Read byte order
        var b1 = ms.ReadByte();
        var b2 = ms.ReadByte();
        var littleEndian = b1 == 0x49 && b2 == 0x49;
        
        metadata.Properties["Byte Order"] = littleEndian ? "Little Endian" : "Big Endian";
        
        // Read magic number (42)
        var magic = new byte[2];
        ms.Read(magic, 0, 2);
        
        // Read IFD offset
        var offsetBytes = new byte[4];
        ms.Read(offsetBytes, 0, 4);
        if (!littleEndian) Array.Reverse(offsetBytes);
        var ifdOffset = BitConverter.ToUInt32(offsetBytes, 0);
        
        // Jump to IFD
        if (ifdOffset < data.Length - 2)
        {
            ms.Seek(ifdOffset, SeekOrigin.Begin);
            
            var entryCountBytes = new byte[2];
            ms.Read(entryCountBytes, 0, 2);
            if (!littleEndian) Array.Reverse(entryCountBytes);
            var entryCount = BitConverter.ToUInt16(entryCountBytes, 0);
            
            // Read entries (simplified)
            for (int i = 0; i < entryCount && ms.Position + 12 <= ms.Length; i++)
            {
                var entryBytes = new byte[12];
                ms.Read(entryBytes, 0, 12);
                
                var tag = ReadUInt16(entryBytes, 0, littleEndian);
                
                switch (tag)
                {
                    case 0x0100: // ImageWidth
                        metadata.Width = (int)ReadUInt32(entryBytes, 8, littleEndian);
                        break;
                    case 0x0101: // ImageHeight
                        metadata.Height = (int)ReadUInt32(entryBytes, 8, littleEndian);
                        break;
                    case 0x0102: // BitsPerSample
                        metadata.BitDepth = ReadUInt16(entryBytes, 8, littleEndian);
                        break;
                }
            }
        }
    }

    private static void ExtractWebPMetadata(byte[] data, ImageMetadata metadata)
    {
        using var ms = new MemoryStream(data);
        
        // Skip "RIFF"
        ms.Seek(4, SeekOrigin.Begin);
        
        // Read file size
        var sizeBytes = new byte[4];
        ms.Read(sizeBytes, 0, 4);
        
        // Skip "WEBP"
        ms.Seek(4, SeekOrigin.Current);
        
        // Read chunk type
        var chunkType = new byte[4];
        ms.Read(chunkType, 0, 4);
        var type = Encoding.ASCII.GetString(chunkType);
        
        metadata.Properties["WebP Type"] = type;
        
        // Read chunk size
        ms.Read(sizeBytes, 0, 4);
        var chunkSize = BitConverter.ToInt32(sizeBytes, 0);
        
        // VP8 (lossy)
        if (type == "VP8 " && chunkSize >= 10)
        {
            ms.Seek(6, SeekOrigin.Current); // Skip frame tag
            
            var dimensionBytes = new byte[4];
            ms.Read(dimensionBytes, 0, 4);
            
            metadata.Width = (dimensionBytes[0] | (dimensionBytes[1] << 8)) & 0x3FFF;
            metadata.Height = (dimensionBytes[2] | (dimensionBytes[3] << 8)) & 0x3FFF;
            metadata.Properties["Compression"] = "Lossy (VP8)";
        }
        // VP8L (lossless)
        else if (type == "VP8L" && chunkSize >= 5)
        {
            ms.ReadByte(); // Signature
            
            var dimensionBytes = new byte[4];
            ms.Read(dimensionBytes, 0, 4);
            
            var bits = BitConverter.ToUInt32(dimensionBytes, 0);
            metadata.Width = ((int)(bits & 0x3FFF)) + 1;
            metadata.Height = ((int)((bits >> 14) & 0x3FFF)) + 1;
            metadata.Properties["Compression"] = "Lossless (VP8L)";
        }
    }
}

// ImageMetadata class now defined in MediaMetadataModels.cs
