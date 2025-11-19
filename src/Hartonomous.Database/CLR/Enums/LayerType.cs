namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Neural network layer types with int backing for SQL Server CLR performance.
    /// Supports transformer models, diffusion models, VAEs, and traditional architectures.
    /// </summary>
    public enum LayerType : int
    {
        Unknown = 0,
        Dense = 1,          // Fully connected layer
        Embedding = 2,      // Token/position embedding
        LayerNorm = 3,      // Layer normalization
        Dropout = 4,        // Dropout regularization
        Attention = 5,      // Self/cross-attention mechanism
        MultiHeadAttention = 6,
        CrossAttention = 7, // Cross-attention (Stable Diffusion)
        FeedForward = 8,    // MLP/FFN block
        Residual = 9,       // Skip connection
        Convolution = 10,   // Convolutional layer
        Pooling = 11,       // Max/Average pooling
        BatchNorm = 12,     // Batch normalization
        UNetDown = 20,      // UNet downsampling block
        UNetMid = 21,       // UNet middle block
        UNetUp = 22,        // UNet upsampling block
        VAE = 23,           // Variational autoencoder
        RNN = 30,           // Recurrent layer
        LSTM = 31,          // Long Short-Term Memory
        GRU = 32            // Gated Recurrent Unit
    }
}
