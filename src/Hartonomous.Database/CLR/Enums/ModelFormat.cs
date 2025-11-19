namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Model file format types with int backing for SQL Server CLR performance.
    /// Use int values in SQL, convert at C# boundary via EnumHelper.
    /// </summary>
    public enum ModelFormat : int
    {
        Unknown = 0,
        GGUF = 1,           // GPT-Generated Unified Format (llama.cpp, ollama)
        SafeTensors = 2,    // Hugging Face format
        ONNX = 3,           // Open Neural Network Exchange
        PyTorch = 4,        // .pth, .pt, .ckpt checkpoints
        TensorFlow = 5,     // SavedModel, .pb
        StableDiffusion = 6 // UNet/VAE/TextEncoder checkpoints
    }
}
