# 08 - Advanced Optimizations: Optional GPU Acceleration (Out-of-Process)

Initial research into using libraries like ILGPU directly within the SQL CLR is **invalidated** due to a critical technical constraint: the SQL CLR host cannot load assemblies that have `.NET Standard` dependencies, which ILGPU requires for its .NET Framework target.

Therefore, to achieve optional GPU acceleration, we must adopt an **out-of-process architecture**. This model isolates the GPU computation into a separate, standalone service, which the SQL CLR can communicate with. This approach is more complex but respects the CLR's limitations and provides greater stability.

The guiding principle remains: "If the GPU service is available, use it. Otherwise, fall back to the highly optimized in-process CPU path without any change to the calling code."

## 1. The Out-of-Process Architecture

This architecture consists of two main components:

1.  **The SQL CLR Assembly:** Remains on .NET Framework 4.8.1. It contains the high-performance SIMD-accelerated CPU path. It will be modified to act as a *client* that attempts to connect to the GPU Worker Service.
2.  **The GPU Worker Service:** A new, standalone .NET application built on a modern framework (e.g., .NET 8). This service's only job is to host the GPU library (ILGPU), load models, and expose endpoints for computation.

## 2. High-Performance IPC: A Hybrid Strategy

To minimize the IPC overhead that is so critical to performance, we will implement a **hybrid IPC strategy** that uses two different mechanisms for two different purposes:

1.  **Named Pipes:** Used for lightweight, message-based command and control (e.g., "a job is ready," "the job is done").
2.  **Memory-Mapped Files (MMFs):** Used to transfer the actual large data (vectors, tensors) by sharing memory directly between the CLR and the GPU worker. This **avoids copying the data**, which is the single biggest source of IPC overhead.

### The Workflow

The interaction for each GPU-accelerated operation will follow these steps:

1.  The SQL CLR function (the client) creates or opens a shared Memory-Mapped File.
2.  It writes the large input vector(s) directly into the MMF.
3.  It sends a small control message to the GPU worker via a Named Pipe. This message is simple and contains information like:
    -   The name/handle of the MMF to use.
    -   The type of operation to perform (e.g., `DotProduct`).
    -   The size and offset of the data within the MMF.
4.  The GPU Worker Service (the server), which is listening on the Named Pipe, receives the control message.
5.  It opens the specified MMF, reads the input data directly from shared memory, and passes it to the GPU for computation.
6.  After the GPU completes the work, the worker writes the result back into the MMF.
7.  The worker sends a small "completion" message back to the CLR via the Named Pipe.
8.  The CLR function receives the completion message and reads the result directly from the MMF.

This hybrid approach provides the best of both worlds: the simplicity of message-passing with Named Pipes for coordination, and the raw performance of MMFs for data transfer, ensuring the IPC overhead is kept to an absolute minimum.

## 3. The Architectural Pattern: Remote Dispatch
We will implement a **Remote Dispatch** pattern. The logic for choosing the computation path remains inside the CLR, but the GPU path now involves the hybrid IPC call to the local worker service.

### Step 1: Define the Computation Interface
This remains the same. An interface abstracts the required operations.

```csharp
// In Hartonomous.Database/CLR/Core/IVectorOperations.cs
public interface IVectorOperations
{
    float DotProduct(float[] a, float[] b);
    // ... other methods
}
```

### Step 2: Implement the CPU Path
The `SimdVectorOps` class remains unchanged, providing the fast, in-process CPU fallback.

```csharp
// In Hartonomous.Database/CLR/Core/SimdVectorOps.cs
public class SimdVectorOps : IVectorOperations
{
    // ... existing SIMD-accelerated implementation ...
}
```

### Step 3: Create the GPU Worker Service
This is a new project, `Hartonomous.Workers.Gpu`, built on modern .NET.

-   It will host an IPC server (e.g., using Named Pipes or gRPC).
-   It will use ILGPU to perform the actual computations on the GPU.
-   It will expose methods corresponding to the `IVectorOperations` interface.

### Step 4: Create the IPC Client in the CLR
A new class within the CLR project will be responsible for communicating with the worker service.

```csharp
// In Hartonomous.Database/CLR/Core/GpuVectorClient.cs
public class GpuVectorClient : IVectorOperations
{
    private readonly NamedPipeClientStream _pipeClient;

    public GpuVectorClient()
    {
        // In constructor, attempt to connect to the named pipe
        _pipeClient = new NamedPipeClientStream(".", "hartonomous-gpu-pipe", PipeDirection.InOut);
        _pipeClient.Connect(50); // Short timeout (e.g., 50ms)
        if (!_pipeClient.IsConnected)
        {
            throw new TimeoutException("Could not connect to GPU worker service.");
        }
    }

    public float DotProduct(float[] a, float[] b)
    {
        // Write request to the pipe (operation name, vector data)
        // Read result back from the pipe
        // Handle serialization/deserialization
        // ... implementation details ...
    }
}
```

### Step 5: Update the Singleton Factory
The factory's logic now changes from detecting hardware to attempting a connection.

```csharp
// In Hartonomous.Database/CLR/Core/AcceleratorFactory.cs
public static class AcceleratorFactory
{
    private static readonly Lazy<IVectorOperations> _instance = new Lazy<IVectorOperations>(Initialize);
    public static IVectorOperations Instance => _instance.Value;

    private static IVectorOperations Initialize()
    {
        // This code must be run with EXTERNAL_ACCESS or UNSAFE permission set
        // to allow client network connections.
        try
        {
            // Try to connect to the out-of-process GPU worker.
            // The constructor for GpuVectorClient will throw if it can't connect quickly.
            return new GpuVectorClient();
        }
        catch (Exception)
        {
            // If connection fails for any reason (service not running, etc.),
            // fall back to the in-process CPU implementation.
            return new SimdVectorOps();
        }
    }
}
```

## 3. Security and Deployment

-   **CLR Permission Set:** The CLR assembly will need `PERMISSION_SET = EXTERNAL_ACCESS` to allow the IPC client connection (even to localhost).
-   **Deployment:** The new `Hartonomous.Workers.Gpu` service must be deployed as a separate application/service on the same machine as the SQL Server instance. Its lifecycle (startup, shutdown, monitoring) must be managed independently.

## 4. Managing Shared Dependencies

A critical challenge in an out-of-process architecture is preventing versioning conflicts between the client (the SQL CLR project) and the server (the GPU Worker Service). If they use different versions of the data structures being passed between them, it will lead to serialization errors and "dependency hell."

To solve this, we will use the existing **`Hartonomous.Shared.Contracts`** project as the single source of truth for the communication layer.

### The Strategy

1.  **Define Data Transfer Objects (DTOs):** All data structures used for the Inter-Process Communication (IPC) will be defined as simple, serializable classes (DTOs) within the `Hartonomous.Shared.Contracts` project. This includes requests, responses, and any complex data being transferred.

    ```csharp
    // In Hartonomous.Shared.Contracts/Gpu/GpuRequest.cs
    public class GpuRequest
    {
        public GpuOperationType Operation { get; set; }
        public byte[] Payload { get; set; } // Serialized vector data
    }

    // In Hartonomous.Shared.Contracts/Gpu/GpuResponse.cs
    public class GpuResponse
    {
        public bool IsSuccess { get; set; }
        public byte[] Result { get; set; } // Serialized result data
    }
    ```

2.  **Establish Shared References:**
    -   The `Hartonomous.Database` (CLR) project will add a project reference to `Hartonomous.Shared.Contracts`.
    -   The new `Hartonomous.Workers.Gpu` project will also add a project reference to `Hartonomous.Shared.Contracts`.

3.  **Guarantee Version Alignment:** By referencing the same project, both the CLR client and the GPU worker are compiled against the exact same assembly. This ensures that the data contract between them is always synchronized, eliminating the risk of serialization mismatches. Any breaking change to a DTO will result in a compile-time error for both projects, rather than a runtime error in production.
