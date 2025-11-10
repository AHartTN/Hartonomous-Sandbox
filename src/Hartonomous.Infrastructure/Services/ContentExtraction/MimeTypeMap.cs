using System;
using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

internal static class MimeTypeMap
{
    private static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = "text/plain",
        [".log"] = "text/plain",
        [".md"] = "text/markdown",
        [".json"] = "application/json",
        [".yaml"] = "application/x-yaml",
        [".yml"] = "application/x-yaml",
        [".cs"] = "text/plain",
        [".js"] = "text/plain",
        [".ts"] = "text/plain",
        [".py"] = "text/plain",
        [".java"] = "text/plain",
        [".go"] = "text/plain",
        [".sql"] = "text/plain",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".pdf"] = "application/pdf",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".xlsm"] = "application/vnd.ms-excel.sheet.macroEnabled.12",
        [".csv"] = "text/csv",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".tiff"] = "image/tiff",
        [".webp"] = "image/webp",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".flac"] = "audio/flac",
        [".ogg"] = "audio/ogg",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".mkv"] = "video/x-matroska"
    };

    public static string? FromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        return Map.TryGetValue(extension, out var value) ? value : null;
    }
}
