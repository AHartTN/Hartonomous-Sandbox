namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// High-level file categories for atomizer routing.
/// </summary>
public enum FileCategory
{
    Unknown = 0,
    
    // Text-based
    Text = 1,
    Code = 2,
    Markdown = 3,
    Json = 4,
    Xml = 5,
    Yaml = 6,
    
    // Images
    ImageRaster = 10,
    ImageVector = 11,
    
    // Audio
    Audio = 20,
    
    // Video
    Video = 30,
    
    // Documents
    DocumentPdf = 40,
    DocumentWord = 41,
    DocumentExcel = 42,
    DocumentPowerPoint = 43,
    
    // Archives
    Archive = 50,
    
    // AI Models
    ModelGguf = 60,
    ModelSafeTensors = 61,
    ModelOnnx = 62,
    ModelPyTorch = 63,
    ModelTensorFlow = 64,
    
    // Databases
    Database = 70,
    
    // Executables
    Executable = 80,
    
    // Binary
    Binary = 90
}
