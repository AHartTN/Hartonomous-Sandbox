using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Embedding;

/// <summary>
/// Generates embeddings for images using pixel histogram and edge detection.
/// </summary>
public sealed class ImageEmbedder : ModalityEmbedderBase<byte[]>
{
    private readonly ILogger<ImageEmbedder> _logger;

    public override string ModalityType => "image";

    public ImageEmbedder(ILogger<ImageEmbedder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override void ValidateInput(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            throw new ArgumentException("Image data cannot be empty.", nameof(input));
        }
    }

    protected override async Task ExtractFeaturesAsync(
        byte[] imageData,
        Memory<float> embedding,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating image embedding for {Size} bytes.", imageData.Length);

        var embeddingSpan = embedding.Span;

        // Compute features using optimized methods
        var histogram = ComputePixelHistogramOptimized(imageData);
        histogram.AsSpan().CopyTo(embeddingSpan.Slice(0, 256));

        var edgeFeatures = ComputeEdgeFeaturesOptimized(imageData);
        edgeFeatures.AsSpan().CopyTo(embeddingSpan.Slice(256, 128));

        var textureFeatures = ComputeTextureFeaturesOptimized(imageData);
        textureFeatures.AsSpan().CopyTo(embeddingSpan.Slice(384, 128));

        var spatialMoments = ComputeSpatialMomentsOptimized(imageData);
        spatialMoments.AsSpan().CopyTo(embeddingSpan.Slice(512, 256));

        _logger.LogInformation("Image embedding generated with pixel histogram + edge detection.");

        await Task.CompletedTask;
    }

    private float[] ComputePixelHistogramOptimized(byte[] imageData)
    {
        using var image = Image.Load<Rgb24>(imageData);
        var histogram = new int[256];

        // Compute luminance histogram (Y = 0.299*R + 0.587*G + 0.114*B)
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var rowSpan = accessor.GetRowSpan(y);
                for (int x = 0; x < rowSpan.Length; x++)
                {
                    ref readonly var pixel = ref rowSpan[x];
                    int luminance = (int)(0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B);
                    histogram[Math.Clamp(luminance, 0, 255)]++;
                }
            }
        });

        // Normalize to [0, 1]
        int totalPixels = image.Width * image.Height;
        var normalized = new float[256];
        for (int i = 0; i < 256; i++)
        {
            normalized[i] = histogram[i] / (float)totalPixels;
        }

        return normalized;
    }

    private float[] ComputeEdgeFeaturesOptimized(byte[] imageData)
    {
        using var image = Image.Load<Rgb24>(imageData);
        using var grayscale = image.Clone(ctx => ctx.Grayscale());

        var edgeHistogram = new int[128];

        // Sobel edge detection
        grayscale.ProcessPixelRows(accessor =>
        {
            for (int y = 1; y < accessor.Height - 1; y++)
            {
                var prevRow = accessor.GetRowSpan(y - 1);
                var currRow = accessor.GetRowSpan(y);
                var nextRow = accessor.GetRowSpan(y + 1);

                for (int x = 1; x < accessor.Width - 1; x++)
                {
                    // Sobel horizontal gradient
                    int gx = -prevRow[x - 1].R + prevRow[x + 1].R
                             - 2 * currRow[x - 1].R + 2 * currRow[x + 1].R
                             - nextRow[x - 1].R + nextRow[x + 1].R;

                    // Sobel vertical gradient
                    int gy = -prevRow[x - 1].R - 2 * prevRow[x].R - prevRow[x + 1].R
                             + nextRow[x - 1].R + 2 * nextRow[x].R + nextRow[x + 1].R;

                    // Gradient magnitude
                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                    int bin = Math.Clamp(magnitude / 8, 0, 127); // 1024 range → 128 bins
                    edgeHistogram[bin]++;
                }
            }
        });

        // Normalize
        int totalEdges = (grayscale.Width - 2) * (grayscale.Height - 2);
        var normalized = new float[128];
        for (int i = 0; i < 128; i++)
        {
            normalized[i] = edgeHistogram[i] / (float)totalEdges;
        }

        return normalized;
    }

    private float[] ComputeTextureFeaturesOptimized(byte[] imageData)
    {
        using var image = Image.Load<Rgb24>(imageData);
        using var grayscale = image.Clone(ctx => ctx.Grayscale());

        // Simplified GLCM (Gray-Level Co-occurrence Matrix) features
        // We'll compute contrast, correlation, energy, homogeneity across 32 regions
        const int numRegions = 32;
        var features = new float[128]; // 4 features × 32 regions

        int regionWidth = Math.Max(1, grayscale.Width / (int)Math.Sqrt(numRegions));
        int regionHeight = Math.Max(1, grayscale.Height / (int)Math.Sqrt(numRegions));

        int regionIndex = 0;
        grayscale.ProcessPixelRows(accessor =>
        {
            for (int ry = 0; ry < accessor.Height && regionIndex < numRegions; ry += regionHeight)
            {
                for (int rx = 0; rx < accessor.Width && regionIndex < numRegions; rx += regionWidth)
                {
                    float contrast = 0, energy = 0, homogeneity = 0;
                    int pairCount = 0;

                    // Sample pixel pairs in this region
                    for (int y = ry; y < Math.Min(ry + regionHeight, accessor.Height) - 1; y++)
                    {
                        var currRow = accessor.GetRowSpan(y);
                        var nextRow = accessor.GetRowSpan(y + 1);

                        for (int x = rx; x < Math.Min(rx + regionWidth, accessor.Width) - 1; x++)
                        {
                            int i = currRow[x].R / 16; // Quantize to 16 levels
                            int j = currRow[x + 1].R / 16;

                            contrast += (i - j) * (i - j);
                            energy += 1.0f; // Simplified
                            homogeneity += 1.0f / (1.0f + Math.Abs(i - j));
                            pairCount++;
                        }
                    }

                    if (pairCount > 0)
                    {
                        features[regionIndex * 4 + 0] = contrast / pairCount;
                        features[regionIndex * 4 + 1] = 0; // Correlation (simplified, set to 0)
                        features[regionIndex * 4 + 2] = energy / pairCount;
                        features[regionIndex * 4 + 3] = homogeneity / pairCount;
                    }

                    regionIndex++;
                }
            }
        });

        return features;
    }

    private float[] ComputeSpatialMomentsOptimized(byte[] imageData)
    {
        using var image = Image.Load<Rgb24>(imageData);
        using var grayscale = image.Clone(ctx => ctx.Grayscale());

        // Compute Hu moments across 32 spatial regions (7 moments × 32 regions = 224, padded to 256)
        const int numRegions = 32;
        var moments = new float[256];

        int regionWidth = Math.Max(1, grayscale.Width / (int)Math.Sqrt(numRegions));
        int regionHeight = Math.Max(1, grayscale.Height / (int)Math.Sqrt(numRegions));

        int regionIndex = 0;
        grayscale.ProcessPixelRows(accessor =>
        {
            for (int ry = 0; ry < accessor.Height && regionIndex < numRegions; ry += regionHeight)
            {
                for (int rx = 0; rx < accessor.Width && regionIndex < numRegions; rx += regionWidth)
                {
                    // Compute raw moments
                    double m00 = 0, m10 = 0, m01 = 0, m11 = 0, m20 = 0, m02 = 0, m30 = 0, m03 = 0, m21 = 0, m12 = 0;

                    for (int y = ry; y < Math.Min(ry + regionHeight, accessor.Height); y++)
                    {
                        var rowSpan = accessor.GetRowSpan(y);
                        for (int x = rx; x < Math.Min(rx + regionWidth, accessor.Width); x++)
                        {
                            double intensity = rowSpan[x].R / 255.0;
                            int dx = x - rx;
                            int dy = y - ry;

                            m00 += intensity;
                            m10 += dx * intensity;
                            m01 += dy * intensity;
                            m11 += dx * dy * intensity;
                            m20 += dx * dx * intensity;
                            m02 += dy * dy * intensity;
                            m30 += dx * dx * dx * intensity;
                            m03 += dy * dy * dy * intensity;
                            m21 += dx * dx * dy * intensity;
                            m12 += dx * dy * dy * intensity;
                        }
                    }

                    if (m00 > 0)
                    {
                        // Compute centroid
                        double xc = m10 / m00;
                        double yc = m01 / m00;

                        // Compute central moments
                        double mu11 = m11 - xc * m01;
                        double mu20 = m20 - xc * m10;
                        double mu02 = m02 - yc * m01;
                        double mu30 = m30 - 3 * xc * m20 + 2 * xc * xc * m10;
                        double mu03 = m03 - 3 * yc * m02 + 2 * yc * yc * m01;
                        double mu21 = m21 - 2 * xc * m11 - yc * m20 + 2 * xc * xc * m01;
                        double mu12 = m12 - 2 * yc * m11 - xc * m02 + 2 * yc * yc * m10;

                        // Compute normalized central moments
                        double normFactor = Math.Pow(m00, 2.5);
                        double eta20 = mu20 / normFactor;
                        double eta02 = mu02 / normFactor;
                        double eta11 = mu11 / normFactor;
                        double eta30 = mu30 / Math.Pow(m00, 3.0);
                        double eta03 = mu03 / Math.Pow(m00, 3.0);
                        double eta21 = mu21 / Math.Pow(m00, 3.0);
                        double eta12 = mu12 / Math.Pow(m00, 3.0);

                        // Compute 7 Hu moments
                        moments[regionIndex * 7 + 0] = (float)(eta20 + eta02);
                        moments[regionIndex * 7 + 1] = (float)((eta20 - eta02) * (eta20 - eta02) + 4 * eta11 * eta11);
                        moments[regionIndex * 7 + 2] = (float)((eta30 - 3 * eta12) * (eta30 - 3 * eta12) + (3 * eta21 - eta03) * (3 * eta21 - eta03));
                        moments[regionIndex * 7 + 3] = (float)((eta30 + eta12) * (eta30 + eta12) + (eta21 + eta03) * (eta21 + eta03));
                        moments[regionIndex * 7 + 4] = (float)((eta30 - 3 * eta12) * (eta30 + eta12) * ((eta30 + eta12) * (eta30 + eta12) - 3 * (eta21 + eta03) * (eta21 + eta03)) + (3 * eta21 - eta03) * (eta21 + eta03) * (3 * (eta30 + eta12) * (eta30 + eta12) - (eta21 + eta03) * (eta21 + eta03)));
                        moments[regionIndex * 7 + 5] = (float)((eta20 - eta02) * ((eta30 + eta12) * (eta30 + eta12) - (eta21 + eta03) * (eta21 + eta03)) + 4 * eta11 * (eta30 + eta12) * (eta21 + eta03));
                        moments[regionIndex * 7 + 6] = (float)((3 * eta21 - eta03) * (eta30 + eta12) * ((eta30 + eta12) * (eta30 + eta12) - 3 * (eta21 + eta03) * (eta21 + eta03)) - (eta30 - 3 * eta12) * (eta21 + eta03) * (3 * (eta30 + eta12) * (eta30 + eta12) - (eta21 + eta03) * (eta21 + eta03)));
                    }

                    regionIndex++;
                }
            }
        });

        return moments;
    }
}
