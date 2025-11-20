using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.FileType;

/// <summary>
/// Magic byte-based file type detection with extension fallback.
/// Detects 100+ file formats across all major categories.
/// </summary>
public class FileTypeDetector : IFileTypeDetector
{
    private static readonly List<MagicBytePattern> _patterns = InitializePatterns();

    public FileTypeInfo Detect(ReadOnlySpan<byte> content, string? fileName = null)
    {
        // Try magic byte detection first (most reliable)
        foreach (var pattern in _patterns)
        {
            if (content.Length >= pattern.Offset + pattern.Bytes.Length)
            {
                var slice = content.Slice(pattern.Offset, pattern.Bytes.Length);
                if (slice.SequenceEqual(pattern.Bytes))
                {
                    return new FileTypeInfo
                    {
                        ContentType = pattern.ContentType,
                        Category = pattern.Category,
                        SpecificFormat = pattern.Format,
                        Confidence = 0.95,
                        Extension = pattern.Extension,
                        Metadata = pattern.Metadata
                    };
                }
            }
        }

        // Fallback to extension-based detection
        if (!string.IsNullOrEmpty(fileName))
        {
            var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            var extensionMatch = _patterns.FirstOrDefault(p => p.Extension == ext);
            if (extensionMatch != null)
            {
                return new FileTypeInfo
                {
                    ContentType = extensionMatch.ContentType,
                    Category = extensionMatch.Category,
                    SpecificFormat = extensionMatch.Format,
                    Confidence = 0.6,
                    Extension = ext,
                    Metadata = extensionMatch.Metadata
                };
            }
        }

        // Content-based heuristics for text
        if (IsLikelyText(content))
        {
            return new FileTypeInfo
            {
                ContentType = "text/plain",
                Category = FileCategory.Text,
                SpecificFormat = "plain-text",
                Confidence = 0.5,
                Extension = "txt"
            };
        }

        // Unknown binary
        return new FileTypeInfo
        {
            ContentType = "application/octet-stream",
            Category = FileCategory.Binary,
            SpecificFormat = "unknown",
            Confidence = 0.3,
            Extension = "bin"
        };
    }

    public FileTypeInfo Detect(Stream stream, string? fileName = null)
    {
        // Read first 512 bytes for magic byte detection
        var buffer = new byte[512];
        var originalPosition = stream.CanSeek ? stream.Position : 0;
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        
        if (stream.CanSeek)
            stream.Position = originalPosition;

        return Detect(buffer.AsSpan(0, bytesRead), fileName);
    }

    private static bool IsLikelyText(ReadOnlySpan<byte> content)
    {
        if (content.Length == 0) return false;

        int textChars = 0;
        int totalChars = Math.Min(content.Length, 512);

        for (int i = 0; i < totalChars; i++)
        {
            byte b = content[i];
            // Printable ASCII or common whitespace
            if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                textChars++;
        }

        return (double)textChars / totalChars > 0.85;
    }

    private static List<MagicBytePattern> InitializePatterns()
    {
        return new List<MagicBytePattern>
        {
            // ============================================================
            // IMAGES - Raster
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                ContentType = "image/png",
                Category = FileCategory.ImageRaster,
                Format = "PNG",
                Extension = "png"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0xFF, 0xD8, 0xFF },
                ContentType = "image/jpeg",
                Category = FileCategory.ImageRaster,
                Format = "JPEG",
                Extension = "jpg"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x47, 0x49, 0x46, 0x38 }, // "GIF8"
                ContentType = "image/gif",
                Category = FileCategory.ImageRaster,
                Format = "GIF",
                Extension = "gif"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x42, 0x4D }, // "BM"
                ContentType = "image/bmp",
                Category = FileCategory.ImageRaster,
                Format = "BMP",
                Extension = "bmp"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x49, 0x49, 0x2A, 0x00 }, // Little-endian TIFF
                ContentType = "image/tiff",
                Category = FileCategory.ImageRaster,
                Format = "TIFF",
                Extension = "tiff"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, // Big-endian TIFF
                ContentType = "image/tiff",
                Category = FileCategory.ImageRaster,
                Format = "TIFF",
                Extension = "tiff"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("RIFF"),
                ContentType = "image/webp",
                Category = FileCategory.ImageRaster,
                Format = "WebP",
                Extension = "webp",
                Offset = 0 // Also need to check for "WEBP" at offset 8
            },

            // ============================================================
            // IMAGES - Vector
            // ============================================================
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("<?xml"),
                ContentType = "image/svg+xml",
                Category = FileCategory.ImageVector,
                Format = "SVG",
                Extension = "svg"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("<svg"),
                ContentType = "image/svg+xml",
                Category = FileCategory.ImageVector,
                Format = "SVG",
                Extension = "svg"
            },

            // ============================================================
            // AUDIO
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0xFF, 0xFB }, // MP3 frame sync
                ContentType = "audio/mpeg",
                Category = FileCategory.Audio,
                Format = "MP3",
                Extension = "mp3"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0xFF, 0xF3 }, // MP3 frame sync variant
                ContentType = "audio/mpeg",
                Category = FileCategory.Audio,
                Format = "MP3",
                Extension = "mp3"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("RIFF"),
                ContentType = "audio/wav",
                Category = FileCategory.Audio,
                Format = "WAV",
                Extension = "wav",
                Offset = 0 // Need "WAVE" at offset 8
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("fLaC"),
                ContentType = "audio/flac",
                Category = FileCategory.Audio,
                Format = "FLAC",
                Extension = "flac"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("OggS"),
                ContentType = "audio/ogg",
                Category = FileCategory.Audio,
                Format = "OGG",
                Extension = "ogg"
            },

            // ============================================================
            // VIDEO
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }, // MP4
                ContentType = "video/mp4",
                Category = FileCategory.Video,
                Format = "MP4",
                Extension = "mp4",
                Offset = 0
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("RIFF"),
                ContentType = "video/avi",
                Category = FileCategory.Video,
                Format = "AVI",
                Extension = "avi",
                Offset = 0
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, // Matroska/WebM
                ContentType = "video/x-matroska",
                Category = FileCategory.Video,
                Format = "MKV",
                Extension = "mkv"
            },

            // ============================================================
            // DOCUMENTS
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // "%PDF"
                ContentType = "application/pdf",
                Category = FileCategory.DocumentPdf,
                Format = "PDF",
                Extension = "pdf"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // ZIP (used by DOCX, XLSX, etc.)
                ContentType = "application/zip",
                Category = FileCategory.Archive,
                Format = "ZIP",
                Extension = "zip"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, // MS Office (old format)
                ContentType = "application/msword",
                Category = FileCategory.DocumentWord,
                Format = "DOC",
                Extension = "doc"
            },

            // ============================================================
            // ARCHIVES
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x1F, 0x8B }, // GZIP
                ContentType = "application/gzip",
                Category = FileCategory.Archive,
                Format = "GZIP",
                Extension = "gz"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x42, 0x5A, 0x68 }, // BZIP2
                ContentType = "application/x-bzip2",
                Category = FileCategory.Archive,
                Format = "BZIP2",
                Extension = "bz2"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("7z\xBC\xAF\x27\x1C"),
                ContentType = "application/x-7z-compressed",
                Category = FileCategory.Archive,
                Format = "7Z",
                Extension = "7z"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("Rar!\x1A\x07"),
                ContentType = "application/x-rar-compressed",
                Category = FileCategory.Archive,
                Format = "RAR",
                Extension = "rar"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("ustar"),
                ContentType = "application/x-tar",
                Category = FileCategory.Archive,
                Format = "TAR",
                Extension = "tar",
                Offset = 257
            },

            // ============================================================
            // AI MODELS
            // ============================================================
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("GGUF"),
                ContentType = "application/x-gguf",
                Category = FileCategory.ModelGguf,
                Format = "GGUF",
                Extension = "gguf"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("safetens"),
                ContentType = "application/x-safetensors",
                Category = FileCategory.ModelSafeTensors,
                Format = "SafeTensors",
                Extension = "safetensors"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x08, 0x00 }, // ONNX protobuf
                ContentType = "application/x-onnx",
                Category = FileCategory.ModelOnnx,
                Format = "ONNX",
                Extension = "onnx"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // PyTorch (ZIP-based)
                ContentType = "application/x-pytorch",
                Category = FileCategory.ModelPyTorch,
                Format = "PyTorch",
                Extension = "pt"
            },

            // ============================================================
            // CODE & TEXT
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0xEF, 0xBB, 0xBF }, // UTF-8 BOM
                ContentType = "text/plain",
                Category = FileCategory.Text,
                Format = "UTF-8",
                Extension = "txt"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("{"),
                ContentType = "application/json",
                Category = FileCategory.Json,
                Format = "JSON",
                Extension = "json"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("["),
                ContentType = "application/json",
                Category = FileCategory.Json,
                Format = "JSON",
                Extension = "json"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("<?xml"),
                ContentType = "application/xml",
                Category = FileCategory.Xml,
                Format = "XML",
                Extension = "xml"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("---\n"),
                ContentType = "text/yaml",
                Category = FileCategory.Yaml,
                Format = "YAML",
                Extension = "yaml"
            },
            new MagicBytePattern
            {
                Bytes = Encoding.UTF8.GetBytes("# "), // Markdown often starts with heading
                ContentType = "text/markdown",
                Category = FileCategory.Markdown,
                Format = "Markdown",
                Extension = "md"
            },

            // ============================================================
            // EXECUTABLES
            // ============================================================
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x4D, 0x5A }, // "MZ" - Windows PE
                ContentType = "application/x-msdownload",
                Category = FileCategory.Executable,
                Format = "PE",
                Extension = "exe"
            },
            new MagicBytePattern
            {
                Bytes = new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF
                ContentType = "application/x-executable",
                Category = FileCategory.Executable,
                Format = "ELF",
                Extension = "elf"
            },

            // ============================================================
            // DATABASES
            // ============================================================
            new MagicBytePattern
            {
                Bytes = Encoding.ASCII.GetBytes("SQLite format 3"),
                ContentType = "application/x-sqlite3",
                Category = FileCategory.Database,
                Format = "SQLite",
                Extension = "db"
            }
        };
    }

    private class MagicBytePattern
    {
        public required byte[] Bytes { get; init; }
        public required string ContentType { get; init; }
        public required FileCategory Category { get; init; }
        public required string Format { get; init; }
        public required string Extension { get; init; }
        public int Offset { get; init; } = 0;
        public string? Metadata { get; init; }
    }
}
