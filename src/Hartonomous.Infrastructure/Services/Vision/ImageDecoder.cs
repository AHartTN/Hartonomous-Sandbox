using System;
using System.IO;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Pure C# image decoder for common formats (PNG, JPEG, BMP).
/// No external dependencies.
/// </summary>
public static class ImageDecoder
{
    public static RawImage Decode(byte[] imageData)
    {
        // Detect format by magic bytes
        if (imageData.Length >= 8 && imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
        {
            return DecodePng(imageData);
        }
        else if (imageData.Length >= 2 && imageData[0] == 0xFF && imageData[1] == 0xD8)
        {
            return DecodeJpeg(imageData);
        }
        else if (imageData.Length >= 2 && imageData[0] == 0x42 && imageData[1] == 0x4D)
        {
            return DecodeBmp(imageData);
        }
        
        throw new NotSupportedException("Unsupported image format");
    }

    private static RawImage DecodePng(byte[] data)
    {
        // PNG decoding is complex - for now, throw to indicate need for implementation
        // Full implementation would parse IHDR, IDAT chunks, decompress with DEFLATE, defilter scanlines
        throw new NotImplementedException("PNG decoding requires full implementation");
    }

    private static RawImage DecodeJpeg(byte[] data)
    {
        // JPEG decoding is very complex (DCT, Huffman, etc.)
        throw new NotImplementedException("JPEG decoding requires full implementation");
    }

    private static RawImage DecodeBmp(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        
        // BMP Header
        br.ReadUInt16(); // "BM"
        br.ReadUInt32(); // File size
        br.ReadUInt32(); // Reserved
        var dataOffset = br.ReadUInt32();
        
        // DIB Header
        var headerSize = br.ReadUInt32();
        var width = br.ReadInt32();
        var height = br.ReadInt32();
        br.ReadUInt16(); // Planes
        var bitsPerPixel = br.ReadUInt16();
        
        if (bitsPerPixel != 24 && bitsPerPixel != 32)
        {
            throw new NotSupportedException($"BMP {bitsPerPixel}-bit not supported");
        }
        
        // Skip to pixel data
        ms.Seek(dataOffset, SeekOrigin.Begin);
        
        var pixels = new Pixel[height, width];
        var bytesPerPixel = bitsPerPixel / 8;
        var rowSize = ((bitsPerPixel * width + 31) / 32) * 4; // Row padding to 4-byte boundary
        
        // BMP rows are bottom-to-top
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                var b = br.ReadByte();
                var g = br.ReadByte();
                var r = br.ReadByte();
                var a = bytesPerPixel == 4 ? br.ReadByte() : (byte)255;
                
                pixels[y, x] = new Pixel { R = r, G = g, B = b, A = a };
            }
            
            // Skip row padding
            var paddingBytes = rowSize - (width * bytesPerPixel);
            br.ReadBytes(paddingBytes);
        }
        
        return new RawImage
        {
            Width = width,
            Height = height,
            Pixels = pixels
        };
    }
}

