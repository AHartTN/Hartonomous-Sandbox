using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Services.Ingestion.Strategies;

/// <summary>
/// Strategy for detecting text-based content types.
/// </summary>
public class TextContentTypeStrategy : IContentTypeStrategy
{
    public int Priority => 10;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("text/") == true)
            return true;

        if (contentType == "application/json" ||
            contentType == "application/xml" ||
            contentType == "text/markdown")
            return true;

        var textExtensions = new[] { "txt", "md", "json", "xml", "yaml", "yml", "csv", "log", "ini", "cfg", "conf" };
        return fileExtension != null && textExtensions.Contains(fileExtension.ToLowerInvariant());
    }
}

/// <summary>
/// Strategy for detecting image content types.
/// </summary>
public class ImageContentTypeStrategy : IContentTypeStrategy
{
    public int Priority => 20;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("image/") == true)
        {
            // Exclude SVG (vector graphics, handled separately)
            if (contentType == "image/svg+xml")
                return false;
            return true;
        }

        var imageExtensions = new[] { "png", "jpg", "jpeg", "gif", "bmp", "tiff", "tif", "webp" };
        return fileExtension != null && imageExtensions.Contains(fileExtension.ToLowerInvariant());
    }
}

/// <summary>
/// Strategy for detecting video content types.
/// </summary>
public class VideoContentTypeStrategy : IContentTypeStrategy
{
    public int Priority => 30;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("video/") == true)
            return true;

        var videoExtensions = new[] { "mp4", "avi", "mkv", "mov", "wmv", "flv", "webm" };
        return fileExtension != null && videoExtensions.Contains(fileExtension.ToLowerInvariant());
    }
}

/// <summary>
/// Strategy for detecting model/file content types.
/// </summary>
public class ModelContentTypeStrategy : IContentTypeStrategy
{
    public int Priority => 15;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        var modelExtensions = new[] { "onnx", "pb", "h5", "pkl", "joblib", "model", "bin" };
        return fileExtension != null && modelExtensions.Contains(fileExtension.ToLowerInvariant());
    }
}