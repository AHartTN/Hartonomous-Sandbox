using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Self-contained OCR implementation using pattern matching and character recognition.
/// No external dependencies - pure C# implementation.
/// </summary>
public class HartonomousOcrService
{
    private readonly CharacterTemplateLibrary _templates;
    
    public HartonomousOcrService()
    {
        _templates = new CharacterTemplateLibrary();
    }

    public async Task<List<OcrRegion>> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var regions = new List<OcrRegion>();
            
            using var image = Image.Load<Rgba32>(imageData);
            
            // Convert to grayscale for easier processing
            var grayImage = ConvertToGrayscale(image);
            
            // Apply adaptive thresholding to separate text from background
            var binaryImage = ApplyAdaptiveThreshold(grayImage);
            
            // Find connected components (potential character regions)
            var components = FindConnectedComponents(binaryImage);
            
            // Group components into text lines
            var textLines = GroupIntoLines(components);
            
            // Recognize characters in each line
            foreach (var line in textLines)
            {
                var recognizedText = RecognizeTextLine(line, binaryImage);
                
                if (!string.IsNullOrWhiteSpace(recognizedText))
                {
                    regions.Add(new OcrRegion
                    {
                        Text = recognizedText,
                        BoundingBox = new BoundingBox
                        {
                            X = line.Min(c => c.X),
                            Y = line.Min(c => c.Y),
                            Width = line.Max(c => c.X + c.Width) - line.Min(c => c.X),
                            Height = line.Max(c => c.Y + c.Height) - line.Min(c => c.Y)
                        },
                        Confidence = CalculateLineConfidence(line)
                    });
                }
            }
            
            return regions;
        }, cancellationToken);
    }

    private byte[,] ConvertToGrayscale(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        var gray = new byte[height, width];
        
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = pixelRow[x];
                    // Standard grayscale conversion
                    gray[y, x] = (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                }
            }
        });
        
        return gray;
    }

    private bool[,] ApplyAdaptiveThreshold(byte[,] grayImage)
    {
        var height = grayImage.GetLength(0);
        var width = grayImage.GetLength(1);
        var binary = new bool[height, width];
        var windowSize = 15; // Adaptive window size
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate local threshold
                var sum = 0;
                var count = 0;
                
                for (int dy = -windowSize / 2; dy <= windowSize / 2; dy++)
                {
                    for (int dx = -windowSize / 2; dx <= windowSize / 2; dx++)
                    {
                        var ny = y + dy;
                        var nx = x + dx;
                        
                        if (ny >= 0 && ny < height && nx >= 0 && nx < width)
                        {
                            sum += grayImage[ny, nx];
                            count++;
                        }
                    }
                }
                
                var threshold = sum / count - 10; // Bias for text detection
                binary[y, x] = grayImage[y, x] < threshold; // Dark text on light background
            }
        }
        
        return binary;
    }

    private List<Component> FindConnectedComponents(bool[,] binaryImage)
    {
        var height = binaryImage.GetLength(0);
        var width = binaryImage.GetLength(1);
        var visited = new bool[height, width];
        var components = new List<Component>();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (binaryImage[y, x] && !visited[y, x])
                {
                    var component = FloodFill(binaryImage, visited, x, y);
                    
                    // Filter out noise (too small or too large)
                    if (component.Pixels.Count >= 10 && component.Pixels.Count <= 10000)
                    {
                        components.Add(component);
                    }
                }
            }
        }
        
        return components;
    }

    private Component FloodFill(bool[,] image, bool[,] visited, int startX, int startY)
    {
        var height = image.GetLength(0);
        var width = image.GetLength(1);
        var component = new Component();
        var stack = new Stack<(int x, int y)>();
        
        stack.Push((startX, startY));
        
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            
            if (x < 0 || x >= width || y < 0 || y >= height || visited[y, x] || !image[y, x])
                continue;
            
            visited[y, x] = true;
            component.Pixels.Add((x, y));
            
            // 8-connected neighbors
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
            stack.Push((x + 1, y + 1));
            stack.Push((x - 1, y - 1));
            stack.Push((x + 1, y - 1));
            stack.Push((x - 1, y + 1));
        }
        
        // Calculate bounding box
        component.X = component.Pixels.Min(p => p.x);
        component.Y = component.Pixels.Min(p => p.y);
        component.Width = component.Pixels.Max(p => p.x) - component.X + 1;
        component.Height = component.Pixels.Max(p => p.y) - component.Y + 1;
        
        return component;
    }

    private List<List<Component>> GroupIntoLines(List<Component> components)
    {
        // Sort components by Y position
        var sorted = components.OrderBy(c => c.Y).ToList();
        var lines = new List<List<Component>>();
        var currentLine = new List<Component>();
        
        foreach (var component in sorted)
        {
            if (currentLine.Count == 0)
            {
                currentLine.Add(component);
            }
            else
            {
                var avgY = currentLine.Average(c => c.Y);
                var avgHeight = currentLine.Average(c => c.Height);
                
                // If component is roughly on the same line
                if (Math.Abs(component.Y - avgY) < avgHeight * 0.5)
                {
                    currentLine.Add(component);
                }
                else
                {
                    // Start new line
                    lines.Add(currentLine.OrderBy(c => c.X).ToList());
                    currentLine = new List<Component> { component };
                }
            }
        }
        
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine.OrderBy(c => c.X).ToList());
        }
        
        return lines;
    }

    private string RecognizeTextLine(List<Component> lineComponents, bool[,] binaryImage)
    {
        var sb = new StringBuilder();
        
        foreach (var component in lineComponents)
        {
            // Extract component image
            var componentImage = ExtractComponentImage(component, binaryImage);
            
            // Match against character templates
            var recognizedChar = _templates.RecognizeCharacter(componentImage);
            
            if (recognizedChar != '\0')
            {
                sb.Append(recognizedChar);
            }
            else
            {
                sb.Append('?'); // Unknown character
            }
        }
        
        return sb.ToString();
    }

    private bool[,] ExtractComponentImage(Component component, bool[,] binaryImage)
    {
        var extracted = new bool[component.Height, component.Width];
        
        foreach (var (x, y) in component.Pixels)
        {
            var localX = x - component.X;
            var localY = y - component.Y;
            extracted[localY, localX] = true;
        }
        
        return extracted;
    }

    private float CalculateLineConfidence(List<Component> lineComponents)
    {
        // Simple heuristic: more consistent component sizes = higher confidence
        if (lineComponents.Count == 0) return 0f;
        
        var avgHeight = lineComponents.Average(c => c.Height);
        var variance = lineComponents.Average(c => Math.Pow(c.Height - avgHeight, 2));
        var stdDev = Math.Sqrt(variance);
        
        // Lower std dev = higher confidence
        var confidence = Math.Max(0, 1.0f - (float)(stdDev / avgHeight));
        return confidence;
    }
}

public class Component
{
    public List<(int x, int y)> Pixels { get; set; } = new();
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Library of character templates for pattern matching.
/// This would be trained/populated with actual character patterns.
/// </summary>
public class CharacterTemplateLibrary
{
    private readonly Dictionary<char, List<bool[,]>> _templates = new();
    
    public CharacterTemplateLibrary()
    {
        // TODO: Load pre-trained character templates
        // For now, this is a placeholder that would be populated with:
        // 1. Standard font renderings
        // 2. Hand-labeled training data
        // 3. Synthetic variations (rotations, scaling, noise)
    }
    
    public char RecognizeCharacter(bool[,] componentImage)
    {
        // TODO: Implement template matching
        // 1. Normalize component image to standard size
        // 2. Compare against all templates using correlation
        // 3. Return best match above confidence threshold
        
        // Placeholder: return space for now
        return ' ';
    }
    
    public void AddTemplate(char character, bool[,] template)
    {
        if (!_templates.ContainsKey(character))
        {
            _templates[character] = new List<bool[,]>();
        }
        _templates[character].Add(template);
    }
}

public class OcrRegion
{
    public required string Text { get; set; }
    public required BoundingBox BoundingBox { get; set; }
    public required float Confidence { get; set; }
}

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
